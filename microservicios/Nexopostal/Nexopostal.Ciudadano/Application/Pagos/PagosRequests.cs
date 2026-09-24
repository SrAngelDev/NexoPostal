using MediatR;
using Nexopostal.Shared.Cqrs;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Application.Pagos;
public sealed record CrearSesionPagoCommand(CrearSesionPagoDto Dto, string UserId, TipoEntrega TipoEntrega) : ICommand<SesionPagoCreadaDto>;
public sealed record VerificarPagoCommand(string SessionId, string UserId) : ICommand<VerificarPagoResultadoDto?>;
public sealed record ReintentarPagoCommand(string Numero, string UserId, ReintentarPagoDto Dto) : ICommand<ReintentoPagoResultado>;
public sealed record ConfirmarPagoWebhookCommand(string SessionId) : ICommand<Unit>;
public enum ErrorReintento
{
    EnvioNoEncontrado,
    EstadoNoPendiente
}

public sealed record ReintentoPagoResultado(SesionPagoCreadaDto? Sesion, ErrorReintento? Error);
public sealed class PagosHandlers : IRequestHandler<CrearSesionPagoCommand, SesionPagoCreadaDto>, IRequestHandler<VerificarPagoCommand, VerificarPagoResultadoDto?>, IRequestHandler<ReintentarPagoCommand, ReintentoPagoResultado>, IRequestHandler<ConfirmarPagoWebhookCommand, Unit>
{
    private readonly IEnvioRepository _envioRepo;
    private readonly IStripeService _stripeService;
    private readonly IEtiquetaPdfService _etiquetaPdfService;
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IEmailService _emailService;
    private readonly ITrackingNumberGenerator _trackingGenerator;
    private readonly ILogisticaNotifierService _logisticaNotifier;
    private readonly ITarifasService _tarifasService;
    private readonly ILogger<PagosHandlers> _logger;
    public PagosHandlers(IEnvioRepository envioRepo, IStripeService stripeService, IEtiquetaPdfService etiquetaPdfService, IFacturaPdfService facturaPdfService, IEmailService emailService, ITrackingNumberGenerator trackingGenerator, ILogisticaNotifierService logisticaNotifier, ITarifasService tarifasService, ILogger<PagosHandlers> logger)
    {
        _envioRepo = envioRepo;
        _stripeService = stripeService;
        _etiquetaPdfService = etiquetaPdfService;
        _facturaPdfService = facturaPdfService;
        _emailService = emailService;
        _trackingGenerator = trackingGenerator;
        _logisticaNotifier = logisticaNotifier;
        _tarifasService = tarifasService;
        _logger = logger;
    }

    public async Task<SesionPagoCreadaDto> Handle(CrearSesionPagoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var dto = request.Dto;
        var userId = request.UserId;
        var tipoEntrega = request.TipoEntrega;
        var dimensiones = _tarifasService.ParseDimensiones(dto.Dimensiones);
        var tarifa = _tarifasService.Calcular(new TarifaCalculoInput(dto.Peso, dimensiones.Largo, dimensiones.Ancho, dimensiones.Alto, dto.CodigoPostalOrigen, dto.CodigoPostalDestino, dto.TipoTarifa));
        // 1. Crear el envío en estado PendientePago
        var envio = new Envio
        {
            NumeroSeguimiento = _trackingGenerator.Generate(),
            NumeroExpedicion = _trackingGenerator.GenerateExpedicion(),
            IdentityUserId = userId,
            PesoKg = dto.Peso,
            Dimensiones = dto.Dimensiones,
            CodigoPostalOrigen = dto.CodigoPostalOrigen,
            CodigoPostalDestino = dto.CodigoPostalDestino,
            Origen = dto.DireccionOrigen,
            Destino = dto.DireccionDestino,
            NombreRemitente = dto.NombreRemitente,
            ApellidosRemitente = dto.ApellidosRemitente,
            TelefonoRemitente = dto.TelefonoRemitente,
            EmailRemitente = dto.EmailRemitente,
            DniRemitente = dto.DniRemitente,
            NombreDestinatario = dto.NombreDestinatario,
            ApellidosDestinatario = dto.ApellidosDestinatario,
            TelefonoDestinatario = dto.TelefonoDestinatario,
            EmailDestinatario = dto.EmailDestinatario,
            DniDestinatario = dto.DniDestinatario,
            TipoTarifa = tarifa.TipoTarifa,
            TiempoEntregaEstimado = tarifa.TiempoEntregaEstimado,
            CosteCalculado = tarifa.PrecioTotal,
            OficinaOrigenId = dto.OficinaOrigenId,
            OficinaDestinoId = dto.OficinaDestinoId,
            TipoEntrega = tipoEntrega,
            EstadoActual = EstadoEnvio.PendientePago,
            EstadoInternoActual = EstadoInterno.PendientePago,
            Pagado = false,
            FechaCreacion = DateTime.UtcNow
        };
        await _envioRepo.CreateAsync(envio);
        _logger.LogInformation("Envío {NumeroSeguimiento} creado en estado PendientePago por usuario {UserId}", envio.NumeroSeguimiento, userId);
        // 2. Crear sesión de Stripe Checkout
        var successUrl = $"{dto.UrlBase.TrimEnd('/')}/pago-exitoso?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{dto.UrlBase.TrimEnd('/')}/pago-cancelado?envio={envio.NumeroSeguimiento}";
        var(sessionUrl, sessionId) = await _stripeService.CrearSesionCheckout(envio, successUrl, cancelUrl);
        // 3. Guardar el ID de sesión de Stripe en el envío
        envio.StripeSessionId = sessionId;
        await _envioRepo.UpdateAsync(envio);
        return new SesionPagoCreadaDto
        {
            SessionUrl = sessionUrl,
            SessionId = sessionId,
            NumeroSeguimiento = envio.NumeroSeguimiento,
            PrecioCalculado = tarifa.PrecioTotal,
            TiempoEntregaEstimado = tarifa.TiempoEntregaEstimado,
            Zona = tarifa.Zona,
            TipoTarifa = tarifa.TipoTarifa
        };
    }

    public async Task<VerificarPagoResultadoDto?> Handle(VerificarPagoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sessionId = request.SessionId;
        var userId = request.UserId;
        // Buscar el envío por sessionId y verificar que pertenece al usuario
        var envio = await _envioRepo.GetByStripeSessionAsync(sessionId);
        if (envio == null || envio.IdentityUserId != userId)
        {
            _logger.LogWarning("No se encontró envío con sessionId {SessionId} para usuario {UserId}", sessionId, userId);
            return null;
        }

        // Si ya está procesado, devolver directamente el resultado
        if (envio.Pagado)
        {
            return MapToVerificarDto(envio);
        }

        // Verificar con Stripe
        var pagado = await _stripeService.VerificarPagoSesion(sessionId);
        if (pagado)
        {
            await ProcesarPagoExitoso(envio);
        }

        return MapToVerificarDto(envio);
    }

    public async Task<ReintentoPagoResultado> Handle(ReintentarPagoCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var numero = request.Numero;
        var userId = request.UserId;
        var dto = request.Dto;
        var envio = await _envioRepo.GetByTrackingAndUserAsync(numero, userId);
        if (envio == null)
            return new ReintentoPagoResultado(null, ErrorReintento.EnvioNoEncontrado);
        if (envio.Pagado || envio.EstadoActual != EstadoEnvio.PendientePago)
            return new ReintentoPagoResultado(null, ErrorReintento.EstadoNoPendiente);
        // Crear nueva sesión de Stripe
        var successUrl = $"{dto.UrlBase.TrimEnd('/')}/pago-exitoso?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{dto.UrlBase.TrimEnd('/')}/pago-cancelado?envio={envio.NumeroSeguimiento}";
        var(sessionUrl, sessionId) = await _stripeService.CrearSesionCheckout(envio, successUrl, cancelUrl);
        envio.StripeSessionId = sessionId;
        await _envioRepo.UpdateAsync(envio);
        _logger.LogInformation("Reintento de pago para envío {NumeroSeguimiento}: nueva sesión {SessionId}", envio.NumeroSeguimiento, sessionId);
        return new ReintentoPagoResultado(new SesionPagoCreadaDto { SessionUrl = sessionUrl, SessionId = sessionId, NumeroSeguimiento = envio.NumeroSeguimiento, PrecioCalculado = envio.CosteCalculado, TiempoEntregaEstimado = envio.TiempoEntregaEstimado, Zona = string.Empty, TipoTarifa = envio.TipoTarifa ?? string.Empty }, null);
    }

    public async Task<Unit> Handle(ConfirmarPagoWebhookCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var envio = await _envioRepo.GetByStripeSessionAsync(request.SessionId);
        if (envio != null && !envio.Pagado)
        {
            await ProcesarPagoExitoso(envio);
            _logger.LogInformation("Webhook: pago procesado para envío {NumeroSeguimiento}", envio.NumeroSeguimiento);
        }

        return Unit.Value;
    }

    private async Task ProcesarPagoExitoso(Envio envio)
    {
        envio.Pagado = true;
        envio.FechaPago = DateTime.UtcNow;
        envio.EstadoActual = EstadoEnvio.Admitido;
        envio.EstadoInternoActual = EstadoInterno.PendienteRecogida;
        await _envioRepo.UpdateAsync(envio);
        _logger.LogInformation("Pago confirmado para envío {NumeroSeguimiento}. Generando documentos...", envio.NumeroSeguimiento);
        // Generación de PDFs + envío de email (best-effort).
        // Si falla cualquier paso, el pago ya está confirmado en BD: no debemos devolver HTTP 500
        // ni bloquear el alta del paquete en la red logística.
        try
        {
            var etiquetaPdf = _etiquetaPdfService.GenerarEtiqueta(envio);
            var facturaPdf = _facturaPdfService.GenerarFactura(envio);
            await _emailService.EnviarConfirmacionEnvio(envio, facturaPdf, etiquetaPdf);
            _logger.LogInformation("Documentos generados y email enviado para envío {NumeroSeguimiento}", envio.NumeroSeguimiento);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error generando documentos o enviando email para {NumeroSeguimiento}. " + "El pago ya está confirmado; se podrá reenviar manualmente.", envio.NumeroSeguimiento);
        }

        // 📡 Notificar al microservicio de logística (Intranet) para que
        // resuelva el CTA por código postal y notifique vía SignalR
        var esUrgente = envio.TipoTarifa?.Contains("Premium", StringComparison.OrdinalIgnoreCase) == true;
        var remitente = $"{envio.NombreRemitente} {envio.ApellidosRemitente}".Trim();
        var destinatario = $"{envio.NombreDestinatario} {envio.ApellidosDestinatario}".Trim();
        await _logisticaNotifier.NotificarAdmisionAsync(envio.NumeroExpedicion, envio.CodigoPostalDestino, envio.CodigoPostalOrigen, remitente, destinatario, esUrgente, envio.NumeroSeguimiento, envio.Destino, null, envio.TelefonoDestinatario, envio.OficinaOrigenId, envio.OficinaDestinoId, envio.TipoEntrega.ToString());
    }

    private VerificarPagoResultadoDto MapToVerificarDto(Envio envio)
    {
        return new VerificarPagoResultadoDto
        {
            Pagado = envio.Pagado,
            NumeroSeguimiento = envio.NumeroSeguimiento,
            Estado = envio.EstadoActual.ToString(),
            Precio = envio.CosteCalculado,
            Destino = envio.Destino,
            TipoTarifa = envio.TipoTarifa,
            TiempoEntregaEstimado = envio.TiempoEntregaEstimado,
            EmailRemitente = envio.EmailRemitente,
            FechaPago = envio.FechaPago
        };
    }
}
