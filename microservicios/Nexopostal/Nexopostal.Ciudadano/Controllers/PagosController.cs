using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using System.Security.Claims;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// Controlador para la gestión de pagos con Stripe Checkout.
/// Flujo: Crear sesión → Stripe Checkout → Verificar pago → Generar documentos → Email
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class PagosController : ControllerBase
{
    private readonly IEnvioRepository _envioRepo;
    private readonly IStripeService _stripeService;
    private readonly IEtiquetaPdfService _etiquetaPdfService;
    private readonly IFacturaPdfService _facturaPdfService;
    private readonly IEmailService _emailService;
    private readonly ITrackingNumberGenerator _trackingGenerator;
    private readonly ILogisticaNotifierService _logisticaNotifier;
    private readonly ILogger<PagosController> _logger;

    public PagosController(
        IEnvioRepository envioRepo,
        IStripeService stripeService,
        IEtiquetaPdfService etiquetaPdfService,
        IFacturaPdfService facturaPdfService,
        IEmailService emailService,
        ITrackingNumberGenerator trackingGenerator,
        ILogisticaNotifierService logisticaNotifier,
        ILogger<PagosController> logger)
    {
        _envioRepo = envioRepo;
        _stripeService = stripeService;
        _etiquetaPdfService = etiquetaPdfService;
        _facturaPdfService = facturaPdfService;
        _emailService = emailService;
        _trackingGenerator = trackingGenerator;
        _logisticaNotifier = logisticaNotifier;
        _logger = logger;
    }

    /// <summary>
    /// Crea un envío en estado PendientePago y genera una sesión de Stripe Checkout.
    /// El frontend redirige al usuario a la URL devuelta para completar el pago.
    /// </summary>
    [Authorize]
    [HttpPost("crear-sesion")]
    [ProducesResponseType(typeof(SesionPagoCreadaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearSesionPago([FromBody] CrearSesionPagoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = ObtenerUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Token inválido");

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
            TipoTarifa = dto.TipoTarifa,
            TiempoEntregaEstimado = dto.TiempoEntregaEstimado,
            CosteCalculado = Math.Round(dto.Coste, 2),
            EstadoActual = EstadoEnvio.PendientePago,
            EstadoInternoActual = EstadoInterno.PendientePago,
            Pagado = false,
            FechaCreacion = DateTime.UtcNow
        };

        await _envioRepo.CreateAsync(envio);

        _logger.LogInformation(
            "Envío {NumeroSeguimiento} creado en estado PendientePago por usuario {UserId}",
            envio.NumeroSeguimiento, userId);

        // 2. Crear sesión de Stripe Checkout
        var successUrl = $"{dto.UrlBase.TrimEnd('/')}/pago-exitoso?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{dto.UrlBase.TrimEnd('/')}/pago-cancelado?envio={envio.NumeroSeguimiento}";

        var (sessionUrl, sessionId) = await _stripeService.CrearSesionCheckout(
            envio, successUrl, cancelUrl);

        // 3. Guardar el ID de sesión de Stripe en el envío
        envio.StripeSessionId = sessionId;
        await _envioRepo.UpdateAsync(envio);

        return Ok(new SesionPagoCreadaDto
        {
            SessionUrl = sessionUrl,
            SessionId = sessionId,
            NumeroSeguimiento = envio.NumeroSeguimiento
        });
    }

    /// <summary>
    /// Verifica el estado de pago de una sesión de Stripe.
    /// Si el pago fue exitoso, marca el envío como pagado, genera PDFs y envía email.
    /// </summary>
    [Authorize]
    [HttpGet("verificar/{sessionId}")]
    [ProducesResponseType(typeof(VerificarPagoResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerificarPago(string sessionId)
    {
        var userId = ObtenerUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Token inválido");

        // Buscar el envío por sessionId y verificar que pertenece al usuario
        var envio = await _envioRepo.GetByStripeSessionAsync(sessionId);

        if (envio == null || envio.IdentityUserId != userId)
        {
            _logger.LogWarning("No se encontró envío con sessionId {SessionId} para usuario {UserId}",
                sessionId, userId);
            return NotFound(new { mensaje = "Sesión de pago no encontrada" });
        }

        // Si ya está procesado, devolver directamente el resultado
        if (envio.Pagado)
        {
            return Ok(MapToVerificarDto(envio));
        }

        // Verificar con Stripe
        var pagado = await _stripeService.VerificarPagoSesion(sessionId);

        if (pagado)
        {
            await ProcesarPagoExitoso(envio);
        }

        return Ok(MapToVerificarDto(envio));
    }

    /// <summary>
    /// Reintenta el pago de un envío en estado PendientePago.
    /// Crea una nueva sesión de Stripe Checkout para el mismo envío.
    /// </summary>
    [Authorize]
    [HttpPost("reintentar/{numero}")]
    [ProducesResponseType(typeof(SesionPagoCreadaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReintentarPago(string numero, [FromBody] ReintentarPagoDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = ObtenerUserId();
        if (string.IsNullOrEmpty(userId))
            return Unauthorized("Token inválido");

        var envio = await _envioRepo.GetByTrackingAndUserAsync(numero, userId);

        if (envio == null)
            return NotFound(new { mensaje = "Envío no encontrado" });

        if (envio.Pagado || envio.EstadoActual != EstadoEnvio.PendientePago)
            return BadRequest(new { mensaje = "Este envío ya ha sido pagado o no está en estado pendiente" });

        // Crear nueva sesión de Stripe
        var successUrl = $"{dto.UrlBase.TrimEnd('/')}/pago-exitoso?session_id={{CHECKOUT_SESSION_ID}}";
        var cancelUrl = $"{dto.UrlBase.TrimEnd('/')}/pago-cancelado?envio={envio.NumeroSeguimiento}";

        var (sessionUrl, sessionId) = await _stripeService.CrearSesionCheckout(
            envio, successUrl, cancelUrl);

        envio.StripeSessionId = sessionId;
        await _envioRepo.UpdateAsync(envio);

        _logger.LogInformation(
            "Reintento de pago para envío {NumeroSeguimiento}: nueva sesión {SessionId}",
            envio.NumeroSeguimiento, sessionId);

        return Ok(new SesionPagoCreadaDto
        {
            SessionUrl = sessionUrl,
            SessionId = sessionId,
            NumeroSeguimiento = envio.NumeroSeguimiento
        });
    }

    /// <summary>
    /// Webhook de Stripe para recibir notificaciones de pago.
    /// No requiere autenticación JWT (es llamado por Stripe directamente).
    /// </summary>
    [HttpPost("webhook")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> StripeWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

        try
        {
            // En modo test, procesamos el evento directamente sin verificar firma
            var stripeEvent = Stripe.EventUtility.ParseEvent(json);

            if (stripeEvent.Type == Stripe.EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                if (session != null)
                {
                    var envio = await _envioRepo.GetByStripeSessionAsync(session.Id);

                    if (envio != null && !envio.Pagado)
                    {
                        await ProcesarPagoExitoso(envio);
                        _logger.LogInformation(
                            "Webhook: pago procesado para envío {NumeroSeguimiento}",
                            envio.NumeroSeguimiento);
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando webhook de Stripe");
            return Ok(); // Siempre devolvemos 200 a Stripe para evitar reintentos
        }
    }

    // ===== MÉTODOS AUXILIARES =====

    /// <summary>
    /// Procesa un pago exitoso: actualiza estado, genera PDFs y envía email
    /// </summary>
    private async Task ProcesarPagoExitoso(Envio envio)
    {
        envio.Pagado = true;
        envio.FechaPago = DateTime.UtcNow;
        envio.EstadoActual = EstadoEnvio.Admitido;
        envio.EstadoInternoActual = EstadoInterno.PendienteRecogida;
        await _envioRepo.UpdateAsync(envio);

        _logger.LogInformation(
            "Pago confirmado para envío {NumeroSeguimiento}. Generando documentos...",
            envio.NumeroSeguimiento);

        // Generar PDFs
        var etiquetaPdf = _etiquetaPdfService.GenerarEtiqueta(envio);
        var facturaPdf = _facturaPdfService.GenerarFactura(envio);

        // Enviar email con adjuntos
        await _emailService.EnviarConfirmacionEnvio(envio, facturaPdf, etiquetaPdf);

        _logger.LogInformation(
            "Documentos generados y email enviado para envío {NumeroSeguimiento}",
            envio.NumeroSeguimiento);

        // 📡 Notificar al microservicio de logística (Intranet) para que
        // resuelva el CTA por código postal y notifique vía SignalR
        var esUrgente = envio.TipoTarifa?.Contains("Express", StringComparison.OrdinalIgnoreCase) == true;
        var remitente = $"{envio.NombreRemitente} {envio.ApellidosRemitente}".Trim();
        var destinatario = $"{envio.NombreDestinatario} {envio.ApellidosDestinatario}".Trim();

        await _logisticaNotifier.NotificarAdmisionAsync(
            envio.NumeroExpedicion,
            envio.CodigoPostalDestino,
            envio.CodigoPostalOrigen,
            remitente,
            destinatario,
            esUrgente);
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

    private string? ObtenerUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value
               ?? User.FindFirst("uid")?.Value;
    }
}
