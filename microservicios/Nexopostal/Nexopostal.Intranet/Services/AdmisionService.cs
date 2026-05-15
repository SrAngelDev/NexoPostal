using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio para la admisión de paquetes en la red logística.
/// Resuelve automáticamente el CTA de destino según el código postal
/// y envía notificaciones en tiempo real a los operarios del CTA.
/// 
/// Flujo:
///   1. Se recibe un paquete con CP destino
///   2. Se resuelve el CTA correspondiente (ej: CP 28*** → CTA-MAD)
///   3. Si CP origen ≠ CP destino → se crea movimiento troncal
///   4. Se notifica vía SignalR a los OperarioLogisticos del CTA destino
/// </summary>
public interface IAdmisionService
{
    /// <summary>
    /// Admite un paquete en la red logística:
    /// resuelve el CTA por código postal, crea el movimiento si es necesario
    /// y notifica en tiempo real al CTA destino.
    /// </summary>
    Task<AdmisionPaqueteResponseDto> AdmitirPaquete(AdmisionPaqueteDto dto);
}

public class AdmisionService : IAdmisionService
{
    private readonly IMovimientoPaqueteRepository _movimientoRepo;
    private readonly IClasificacionService _clasificacionService;
    private readonly IRepartoOrquestacionService _repartoOrquestacionService;
    private readonly INotificacionService _notificacionService;
    private readonly ILogger<AdmisionService> _logger;

    public AdmisionService(
        IMovimientoPaqueteRepository movimientoRepo,
        IClasificacionService clasificacionService,
        IRepartoOrquestacionService repartoOrquestacionService,
        INotificacionService notificacionService,
        ILogger<AdmisionService> logger)
    {
        _movimientoRepo = movimientoRepo;
        _clasificacionService = clasificacionService;
        _repartoOrquestacionService = repartoOrquestacionService;
        _notificacionService = notificacionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AdmisionPaqueteResponseDto> AdmitirPaquete(AdmisionPaqueteDto dto)
    {
        // 1. Resolver CTA de destino por código postal
        var ctaDestino = await _clasificacionService.ResolverCtaDestino(dto.CodigoPostalDestino)
            ?? throw new ArgumentException(
                $"No se pudo resolver CTA para el código postal de destino: {dto.CodigoPostalDestino}");

        // 2. Resolver CTA de origen (si se proporciona CP origen)
        ResolverCtaResponseDto? ctaOrigen = null;
        if (!string.IsNullOrWhiteSpace(dto.CodigoPostalOrigen))
        {
            ctaOrigen = await _clasificacionService.ResolverCtaDestino(dto.CodigoPostalOrigen);
        }

        // 3. Determinar si necesita movimiento troncal (CTAs diferentes)
        var requiereTroncal = ctaOrigen != null && ctaOrigen.CtaId != ctaDestino.CtaId;
        string? tipoTransporteStr = null;

        if (requiereTroncal)
        {
            // Determinar tipo de transporte y crear movimiento automáticamente
            var tipoTransporte = await _clasificacionService.DeterminarTipoTransporte(
                ctaOrigen!.CtaId, ctaDestino.CtaId, dto.EsUrgente);
            tipoTransporteStr = tipoTransporte.ToString();

            var movimiento = new MovimientoPaquete
            {
                NumeroExpedicion = dto.NumeroExpedicion,
                CtaOrigenId = ctaOrigen.CtaId,
                CtaDestinoId = ctaDestino.CtaId,
                TipoTransporte = tipoTransporte,
                EsUrgente = dto.EsUrgente,
                Observaciones = $"Movimiento troncal automático. Origen: {dto.CodigoPostalOrigen} ({ctaOrigen.CtaCodigo}) → Destino: {dto.CodigoPostalDestino} ({ctaDestino.CtaCodigo})"
            };

            await _movimientoRepo.CreateAsync(movimiento);

            _logger.LogInformation(
                "Movimiento troncal creado automáticamente: {Expedicion} de {Origen} a {Destino} vía {Transporte}",
                dto.NumeroExpedicion, ctaOrigen.CtaCodigo, ctaDestino.CtaCodigo, tipoTransporte);
        }

        // 4. 📡 Notificar a los OperarioLogisticos del CTA destino
        await _notificacionService.NotificarPaqueteRecibidoEnCta(
            ctaDestino.CtaId,
            ctaDestino.CtaCodigo,
            dto.NumeroExpedicion,
            dto.EsUrgente,
            ctaDestino.Provincia,
            dto.Observaciones);

        _logger.LogInformation(
            "Paquete {Expedicion} admitido · CP destino: {Cp} → CTA: {Cta} ({Area}) · Urgente: {Urgente} · Troncal: {Troncal}",
            dto.NumeroExpedicion, dto.CodigoPostalDestino, ctaDestino.CtaCodigo,
            ctaDestino.Area, dto.EsUrgente, requiereTroncal);

        // 5. Orquestar última milla con Reparto (si vienen datos mínimos de entrega)
        var orquestacionIntentada = TieneDatosMinimosReparto(dto);
        OrquestacionRepartoResultadoDto? orquestacionReparto = null;

        if (orquestacionIntentada)
        {
            orquestacionReparto = await _repartoOrquestacionService
                .AutoAsignarEntregaDesdeAdmisionAsync(dto, ctaDestino);

            _logger.LogInformation(
                "Orquestación Reparto para {Expedicion}: Success={Success}, Ruta={Ruta}, Entrega={Entrega}, Idempotente={Idempotente}",
                dto.NumeroExpedicion,
                orquestacionReparto.Success,
                orquestacionReparto.RutaCodigo,
                orquestacionReparto.EntregaId,
                orquestacionReparto.Idempotente);
        }
        else
        {
            _logger.LogInformation(
                "Admisión {Expedicion} sin datos suficientes para auto-asignación en Reparto",
                dto.NumeroExpedicion);
        }

        // 6. Construir respuesta
        return new AdmisionPaqueteResponseDto
        {
            NumeroExpedicion = dto.NumeroExpedicion,
            CtaDestinoId = ctaDestino.CtaId,
            CtaDestinoCodigo = ctaDestino.CtaCodigo,
            CtaDestinoNombre = ctaDestino.CtaNombre,
            AreaZonal = ctaDestino.Area,
            CtaOrigenId = ctaOrigen?.CtaId,
            CtaOrigenCodigo = ctaOrigen?.CtaCodigo,
            EsUrgente = dto.EsUrgente,
            Provincia = ctaDestino.Provincia,
            RequiereMovimientoTroncal = requiereTroncal,
            TipoTransporte = tipoTransporteStr,
            OrquestacionRepartoIntentada = orquestacionIntentada,
            OrquestacionRepartoExitosa = orquestacionReparto?.Success == true,
            RepartoIdempotente = orquestacionReparto?.Idempotente == true,
            RutaRepartoId = orquestacionReparto?.RutaId,
            RutaRepartoCodigo = orquestacionReparto?.RutaCodigo,
            RepartidorAsignadoId = orquestacionReparto?.RepartidorId,
            RepartidorAsignadoNombre = orquestacionReparto?.RepartidorNombre,
            EntregaRepartoId = orquestacionReparto?.EntregaId,
            MensajeOrquestacionReparto = orquestacionReparto?.Message,
            Mensaje = requiereTroncal
                ? $"Paquete admitido. Se enviará de {ctaOrigen!.CtaCodigo} a {ctaDestino.CtaCodigo} vía {tipoTransporteStr}. Operarios del CTA notificados."
                : $"Paquete admitido directamente en {ctaDestino.CtaCodigo} ({ctaDestino.Provincia}). Operarios del CTA notificados."
        };
    }

    private static bool TieneDatosMinimosReparto(AdmisionPaqueteDto dto)
    {
        return !string.IsNullOrWhiteSpace(dto.NumeroSeguimiento)
            && !string.IsNullOrWhiteSpace(dto.DireccionEntrega)
            && !string.IsNullOrWhiteSpace(dto.CodigoPostalDestino);
    }
}
