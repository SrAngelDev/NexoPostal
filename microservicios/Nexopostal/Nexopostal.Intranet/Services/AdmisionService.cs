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
    private readonly IOficinaPostalService _oficinaService;
    private readonly IRepartoOrquestacionService _repartoOrquestacionService;
    private readonly INotificacionService _notificacionService;
    private readonly ILogger<AdmisionService> _logger;

    public AdmisionService(
        IMovimientoPaqueteRepository movimientoRepo,
        IClasificacionService clasificacionService,
        IOficinaPostalService oficinaService,
        IRepartoOrquestacionService repartoOrquestacionService,
        INotificacionService notificacionService,
        ILogger<AdmisionService> logger)
    {
        _movimientoRepo = movimientoRepo;
        _clasificacionService = clasificacionService;
        _oficinaService = oficinaService;
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

        // 4. (REVISADO) NO se autoasigna tarea al OperarioCTA en la admisión.
        //    El flujo debe pasar primero por la Oficina origen: cuando el OperarioOficina
        //    escanee "SalidaOficinaACta" se generará entonces la tarea de Clasificación en CTA.
        var autoAsignacionCta = new AutoAsignacionCtaResultado
        {
            Attempted = false,
            Success = false,
            Message = "La asignación al CTA se difiere al momento en que la oficina origen entregue el paquete."
        };

        // 5. 📡 Notificar a la OFICINA ORIGEN (si se pudo resolver por CP origen).
        //    Si no hay CP origen o no se resuelve, no se notifica a nadie en esta fase.
        int? oficinaOrigenJsonId = null;
        string? oficinaOrigenNombre = null;
        if (!string.IsNullOrWhiteSpace(dto.CodigoPostalOrigen))
        {
            var oficinaOrigen = await _oficinaService.ResolverOficinaPorCp(dto.CodigoPostalOrigen);
            if (oficinaOrigen != null)
            {
                oficinaOrigenJsonId = oficinaOrigen.OficinaId;
                oficinaOrigenNombre = oficinaOrigen.OficinaNombre;

                await _notificacionService.NotificarNuevoPaqueteEnOficina(
                    oficinaOrigen.OficinaId,
                    oficinaOrigen.OficinaNombre,
                    dto.NumeroExpedicion,
                    dto.EsUrgente,
                    dto.CodigoPostalOrigen,
                    dto.CodigoPostalDestino,
                    dto.Observaciones);
            }
            else
            {
                _logger.LogWarning(
                    "Admisión {Expedicion}: no se pudo resolver oficina origen para CP {Cp}",
                    dto.NumeroExpedicion, dto.CodigoPostalOrigen);
            }
        }

        _logger.LogInformation(
            "Paquete {Expedicion} admitido · CP destino: {Cp} → CTA: {Cta} ({Area}) · Urgente: {Urgente} · Troncal: {Troncal} · OficinaOrigen: {Oficina}",
            dto.NumeroExpedicion, dto.CodigoPostalDestino, ctaDestino.CtaCodigo,
            ctaDestino.Area, dto.EsUrgente, requiereTroncal, oficinaOrigenJsonId);

        // 6. Orquestar última milla con Reparto (si vienen datos mínimos de entrega)
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

        // 7. Construir respuesta
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
            AsignacionAutomaticaIntentada = autoAsignacionCta.Attempted,
            AsignacionAutomaticaExitosa = autoAsignacionCta.Success,
            AsignacionAutomaticaIdempotente = autoAsignacionCta.Idempotent,
            AsignacionAutomaticaId = autoAsignacionCta.AsignacionId,
            OperarioAsignadoId = autoAsignacionCta.OperarioAsignadoId,
            OperarioAsignadoNombre = autoAsignacionCta.OperarioAsignadoNombre,
            MensajeAsignacionAutomatica = autoAsignacionCta.Message,
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

    private sealed class AutoAsignacionCtaResultado
    {
        public bool Attempted { get; init; }
        public bool Success { get; init; }
        public bool Idempotent { get; init; }
        public int? AsignacionId { get; init; }
        public int? OperarioAsignadoId { get; init; }
        public string? OperarioAsignadoNombre { get; init; }
        public string? Message { get; init; }
    }
}
