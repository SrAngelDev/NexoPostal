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
    private const string MensajeSistemaAutoasignacion = "Asignación automática tras admisión de envío pagado";

    private readonly IMovimientoPaqueteRepository _movimientoRepo;
    private readonly IAsignacionPaqueteRepository _asignacionRepo;
    private readonly IOperarioCtaRepository _operarioRepo;
    private readonly IClasificacionService _clasificacionService;
    private readonly IRepartoOrquestacionService _repartoOrquestacionService;
    private readonly INotificacionService _notificacionService;
    private readonly ILogger<AdmisionService> _logger;

    public AdmisionService(
        IMovimientoPaqueteRepository movimientoRepo,
        IAsignacionPaqueteRepository asignacionRepo,
        IOperarioCtaRepository operarioRepo,
        IClasificacionService clasificacionService,
        IRepartoOrquestacionService repartoOrquestacionService,
        INotificacionService notificacionService,
        ILogger<AdmisionService> logger)
    {
        _movimientoRepo = movimientoRepo;
        _asignacionRepo = asignacionRepo;
        _operarioRepo = operarioRepo;
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

        // 4. Crear tarea automática en CTA (mínimo viable): clasificación directa a OperarioOficina
        var autoAsignacionCta = await AutoAsignarClasificacionEnCtaAsync(dto, ctaDestino);

        // 5. 📡 Notificar al rol OperarioCTA del CTA destino
        await _notificacionService.NotificarPaqueteRecibidoEnCta(
            ctaDestino.CtaId,
            ctaDestino.CtaCodigo,
            dto.NumeroExpedicion,
            dto.EsUrgente,
            ctaDestino.Provincia,
            dto.Observaciones);

        _logger.LogInformation(
            "Paquete {Expedicion} admitido · CP destino: {Cp} → CTA: {Cta} ({Area}) · Urgente: {Urgente} · Troncal: {Troncal} · AutoAsignacionCTA: {AutoAsignacion}",
            dto.NumeroExpedicion, dto.CodigoPostalDestino, ctaDestino.CtaCodigo,
            ctaDestino.Area, dto.EsUrgente, requiereTroncal, autoAsignacionCta.Success);

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

    private async Task<AutoAsignacionCtaResultado> AutoAsignarClasificacionEnCtaAsync(
        AdmisionPaqueteDto dto,
        ResolverCtaResponseDto ctaDestino)
    {
        try
        {
            var asignacionExistente = await _asignacionRepo.GetByExpedicionTipoCtaAsync(
                dto.NumeroExpedicion,
                TipoTarea.Clasificacion,
                ctaDestino.CtaId);

            if (asignacionExistente != null)
            {
                var operarioExistente = await _operarioRepo.GetByIdAsync(asignacionExistente.OperarioAsignadoId);

                return new AutoAsignacionCtaResultado
                {
                    Attempted = true,
                    Success = true,
                    Idempotent = true,
                    AsignacionId = asignacionExistente.Id,
                    OperarioAsignadoId = asignacionExistente.OperarioAsignadoId,
                    OperarioAsignadoNombre = operarioExistente?.NombreCompleto,
                    Message = "La tarea de clasificación ya existía para esta expedición en el CTA destino."
                };
            }

            var operariosActivosCta = await _operarioRepo.GetByCtaIdAsync(ctaDestino.CtaId, soloActivos: true);

            var operariosOficina = operariosActivosCta
                .Where(o => o.Rol == RolOperario.OperarioOficina)
                .ToList();

            if (operariosOficina.Count == 0)
            {
                _logger.LogWarning(
                    "Autoasignación CTA omitida para {Expedicion}: no hay OperarioOficina activo en {Cta}",
                    dto.NumeroExpedicion,
                    ctaDestino.CtaCodigo);

                return new AutoAsignacionCtaResultado
                {
                    Attempted = true,
                    Success = false,
                    Message = "No hay operarios de oficina activos para autoasignar la clasificación en este CTA."
                };
            }

            var operarioAsignado = await SeleccionarOperarioConMenorCargaAsync(operariosOficina);

            var asignador = operariosActivosCta.FirstOrDefault(o => o.Rol == RolOperario.OperarioCTA)
                ?? operariosActivosCta.FirstOrDefault(o => o.Rol == RolOperario.Supervisor)
                ?? operarioAsignado;

            var observaciones = string.IsNullOrWhiteSpace(dto.Observaciones)
                ? MensajeSistemaAutoasignacion
                : $"{MensajeSistemaAutoasignacion}. Nota admisión: {dto.Observaciones}";

            var asignacion = await _asignacionRepo.CreateAsync(new AsignacionPaquete
            {
                NumeroExpedicion = dto.NumeroExpedicion,
                OperarioAsignadoId = operarioAsignado.Id,
                AsignadoPorId = asignador.Id,
                CtaId = ctaDestino.CtaId,
                TipoTarea = TipoTarea.Clasificacion,
                EsUrgente = dto.EsUrgente,
                Observaciones = observaciones
            });

            await _notificacionService.NotificarTareaAsignada(
                operarioAsignado.Id,
                ctaDestino.CtaId,
                ctaDestino.CtaCodigo,
                dto.NumeroExpedicion,
                TipoTarea.Clasificacion.ToString(),
                dto.EsUrgente,
                asignador.NombreCompleto);

            _logger.LogInformation(
                "Autoasignación CTA creada: {Expedicion} -> Operario {Operario} ({OperarioId}) en {Cta}",
                dto.NumeroExpedicion,
                operarioAsignado.CodigoEmpleado,
                operarioAsignado.Id,
                ctaDestino.CtaCodigo);

            return new AutoAsignacionCtaResultado
            {
                Attempted = true,
                Success = true,
                Idempotent = false,
                AsignacionId = asignacion.Id,
                OperarioAsignadoId = operarioAsignado.Id,
                OperarioAsignadoNombre = operarioAsignado.NombreCompleto,
                Message = "Tarea de clasificación autoasignada correctamente en el CTA destino."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error en autoasignación CTA para expedición {Expedicion}",
                dto.NumeroExpedicion);

            return new AutoAsignacionCtaResultado
            {
                Attempted = true,
                Success = false,
                Message = "No se pudo crear la asignación automática en CTA."
            };
        }
    }

    private async Task<OperarioCta> SeleccionarOperarioConMenorCargaAsync(List<OperarioCta> operariosOficina)
    {
        var cargas = new List<(OperarioCta Operario, int Carga)>();

        foreach (var operario in operariosOficina)
        {
            var pendientes = await _asignacionRepo.CountByOperarioAndEstadoAsync(operario.Id, EstadoTarea.Pendiente);
            var enProgreso = await _asignacionRepo.CountByOperarioAndEstadoAsync(operario.Id, EstadoTarea.EnProgreso);
            cargas.Add((operario, pendientes + enProgreso));
        }

        return cargas
            .OrderBy(c => c.Carga)
            .ThenBy(c => c.Operario.Id)
            .Select(c => c.Operario)
            .First();
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
