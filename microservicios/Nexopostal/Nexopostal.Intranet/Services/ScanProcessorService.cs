using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio que procesa escaneos de códigos de barras y ejecuta
/// automáticamente la operación logística correspondiente.
///
/// Cada modo de escaneo avanza el paquete al siguiente estado del flujo:
///   Oficina origen → CTA origen → (troncal) → CTA destino → Oficina destino → Reparto
/// </summary>
public interface IScanProcessorService
{
    /// <summary>Procesa un escaneo individual.</summary>
    Task<ScanResultDto> ProcesarEscaneo(ScanRequestDto request);

    /// <summary>Procesa un lote de escaneos con el mismo modo.</summary>
    Task<ScanBatchResultDto> ProcesarLote(ScanBatchRequestDto request);
}

public class ScanProcessorService : IScanProcessorService
{
    private readonly IMovimientoPaqueteRepository _movimientoRepo;
    private readonly IHistorialService _historialService;
    private readonly IClasificacionService _clasificacionService;
    private readonly IMovimientoService _movimientoService;
    private readonly INotificacionService _notificacionService;
    private readonly ICiudadanoEstadoNotifierService _ciudadanoNotifier;
    private readonly IAsignacionPaqueteRepository _asignacionRepo;
    private readonly IOperarioCtaRepository _operarioRepo;
    private readonly ILogger<ScanProcessorService> _logger;

    // Mapeo modo → descripción legible
    private static readonly Dictionary<string, string> DescripcionModos = new()
    {
        [ModosEscaneo.RecepcionOficina] = "Recepción en oficina de origen",
        [ModosEscaneo.RecepcionCta] = "Recepción en CTA",
        [ModosEscaneo.Clasificacion] = "Clasificación para expedición",
        [ModosEscaneo.DespachoTroncal] = "Despacho troncal (CTA → CTA)",
        [ModosEscaneo.RecepcionTroncal] = "Recepción troncal en CTA destino",
        [ModosEscaneo.EntregaOficinaDestino] = "Entrega a oficina de destino",
        [ModosEscaneo.SalidaAReparto] = "Salida a reparto a domicilio"
    };

    // Mapeo modo → estado interno del envío
    private static readonly Dictionary<string, string> EstadoInternoModos = new()
    {
        [ModosEscaneo.RecepcionOficina] = "RecogidoEnOrigen",
        [ModosEscaneo.RecepcionCta] = "RecibidoEnCentroOrigen",
        [ModosEscaneo.Clasificacion] = "ClasificadoParaExpedicion",
        [ModosEscaneo.DespachoTroncal] = "EnTransitoHaciaCentroDestino",
        [ModosEscaneo.RecepcionTroncal] = "RecibidoEnCentroDestino",
        [ModosEscaneo.EntregaOficinaDestino] = "DepositadoEnOficina",
        [ModosEscaneo.SalidaAReparto] = "EnReparto"
    };

    public ScanProcessorService(
        IMovimientoPaqueteRepository movimientoRepo,
        IHistorialService historialService,
        IClasificacionService clasificacionService,
        IMovimientoService movimientoService,
        INotificacionService notificacionService,
        ICiudadanoEstadoNotifierService ciudadanoNotifier,
        IAsignacionPaqueteRepository asignacionRepo,
        IOperarioCtaRepository operarioRepo,
        ILogger<ScanProcessorService> logger)
    {
        _movimientoRepo = movimientoRepo;
        _historialService = historialService;
        _clasificacionService = clasificacionService;
        _movimientoService = movimientoService;
        _notificacionService = notificacionService;
        _ciudadanoNotifier = ciudadanoNotifier;
        _asignacionRepo = asignacionRepo;
        _operarioRepo = operarioRepo;
        _logger = logger;
    }

    public async Task<ScanResultDto> ProcesarEscaneo(ScanRequestDto request)
    {
        // Validar código
        if (string.IsNullOrWhiteSpace(request.CodigoEscaneado))
            return Error(request, "Código escaneado vacío");

        if (!ModosEscaneo.EsValido(request.ModoOperacion))
            return Error(request, $"Modo de operación desconocido: {request.ModoOperacion}");

        var expedicion = request.CodigoEscaneado.Trim().ToUpperInvariant();

        _logger.LogInformation(
            "Procesando escaneo: {Expedicion} · Modo: {Modo} · CTA: {Cta} · Oficina: {Oficina}",
            expedicion, request.ModoOperacion, request.CtaCodigo, request.OficinaNombre);

        try
        {
            return request.ModoOperacion switch
            {
                ModosEscaneo.RecepcionOficina => await ProcesarRecepcionOficina(request, expedicion),
                ModosEscaneo.RecepcionCta => await ProcesarRecepcionCta(request, expedicion),
                ModosEscaneo.Clasificacion => await ProcesarClasificacion(request, expedicion),
                ModosEscaneo.DespachoTroncal => await ProcesarDespachoTroncal(request, expedicion),
                ModosEscaneo.RecepcionTroncal => await ProcesarRecepcionTroncal(request, expedicion),
                ModosEscaneo.EntregaOficinaDestino => await ProcesarEntregaOficinaDestino(request, expedicion),
                ModosEscaneo.SalidaAReparto => await ProcesarSalidaAReparto(request, expedicion),
                _ => Error(request, "Modo no implementado")
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error procesando escaneo {Expedicion} modo {Modo}",
                expedicion, request.ModoOperacion);
            return Error(request, $"Error interno: {ex.Message}");
        }
    }

    public async Task<ScanBatchResultDto> ProcesarLote(ScanBatchRequestDto request)
    {
        var resultados = new List<ScanResultDto>();

        foreach (var codigo in request.CodigosEscaneados)
        {
            var scanRequest = new ScanRequestDto
            {
                CodigoEscaneado = codigo,
                ModoOperacion = request.ModoOperacion,
                CtaId = request.CtaId,
                CtaCodigo = request.CtaCodigo,
                OficinaJsonId = request.OficinaJsonId,
                OficinaNombre = request.OficinaNombre,
                OperarioNombre = request.OperarioNombre
            };

            var resultado = await ProcesarEscaneo(scanRequest);
            resultados.Add(resultado);
        }

        return new ScanBatchResultDto
        {
            TotalEscaneados = resultados.Count,
            Exitosos = resultados.Count(r => r.Exito),
            Fallidos = resultados.Count(r => !r.Exito),
            Resultados = resultados
        };
    }

    // ═══════════════════════════════════════════
    //  PROCESADORES POR MODO
    // ═══════════════════════════════════════════

    /// <summary>
    /// Paquete recibido en oficina de origen (recogida a domicilio o ventanilla).
    /// → Estado: RecogidoEnOrigen
    /// </summary>
    private async Task<ScanResultDto> ProcesarRecepcionOficina(ScanRequestDto req, string expedicion)
    {
        if (!req.OficinaJsonId.HasValue)
            return Error(req, "Se requiere la oficina para recepción");

        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = "RecogidoEnOrigen",
            TipoUbicacion = TipoUbicacion.Oficina.ToString(),
            UbicacionId = req.OficinaJsonId,
            UbicacionNombre = req.OficinaNombre,
            OperarioNombre = req.OperarioNombre,
            Descripcion = $"Paquete recibido en oficina {req.OficinaNombre}",
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        await _ciudadanoNotifier.NotificarEstadoAsync(
            null, expedicion, "RecogidoEnOrigen",
            $"Paquete recibido en {req.OficinaNombre}");

        return Exito(req, expedicion, "RecogidoEnOrigen",
            $"Paquete recibido en {req.OficinaNombre}",
            req.OficinaNombre);
    }

    /// <summary>
    /// Paquete llega al CTA (admisión automática).
    /// → Estado: RecibidoEnCentroOrigen
    /// → Crea movimiento troncal si CP origen ≠ CP destino.
    /// </summary>
    private async Task<ScanResultDto> ProcesarRecepcionCta(ScanRequestDto req, string expedicion)
    {
        if (!req.CtaId.HasValue)
            return Error(req, "Se requiere el CTA para recepción");

        var movimientoCreado = false;
        string? detalles = null;

        // Si tenemos CPs, resolver y crear movimiento troncal automáticamente
        if (!string.IsNullOrEmpty(req.CodigoPostalDestino))
        {
            var ctaDestino = await _clasificacionService.ResolverCtaDestino(req.CodigoPostalDestino);
            if (ctaDestino != null && ctaDestino.CtaId != req.CtaId.Value)
            {
                // Crear movimiento troncal automático
                var tipoTransporte = await _clasificacionService.DeterminarTipoTransporte(
                    req.CtaId.Value, ctaDestino.CtaId, req.EsUrgente);

                var movimiento = new MovimientoPaquete
                {
                    NumeroExpedicion = expedicion,
                    CtaOrigenId = req.CtaId.Value,
                    CtaDestinoId = ctaDestino.CtaId,
                    TipoTransporte = tipoTransporte,
                    EsUrgente = req.EsUrgente,
                    Observaciones = $"Troncal automático por escaneo. {req.CtaCodigo} → {ctaDestino.CtaCodigo}"
                };

                await _movimientoRepo.CreateAsync(movimiento);
                movimientoCreado = true;
                detalles = $"Movimiento troncal creado: {req.CtaCodigo} → {ctaDestino.CtaCodigo} vía {tipoTransporte}";
            }
        }

        // Registrar historial
        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = "RecibidoEnCentroOrigen",
            TipoUbicacion = TipoUbicacion.Cta.ToString(),
            UbicacionId = req.CtaId,
            UbicacionNombre = req.CtaCodigo,
            UbicacionCodigo = req.CtaCodigo,
            OperarioNombre = req.OperarioNombre,
            Descripcion = $"Paquete recibido en {req.CtaCodigo}",
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        // Notificar al CTA (SignalR interno)
        await _notificacionService.NotificarPaqueteRecibidoEnCta(
            req.CtaId.Value, req.CtaCodigo ?? "", expedicion,
            req.EsUrgente, "", req.Observaciones);

        // Notificar a Ciudadano (tracking público)
        await _ciudadanoNotifier.NotificarEstadoAsync(
            null, expedicion, "RecibidoEnCentroOrigen",
            $"Paquete admitido en {req.CtaCodigo}");

        var result = Exito(req, expedicion, "RecibidoEnCentroOrigen",
            $"Paquete admitido en {req.CtaCodigo}",
            req.CtaCodigo);

        result.MovimientoTroncalCreado = movimientoCreado;
        result.NotificacionEnviada = true;
        result.Detalles = detalles;

        return result;
    }

    /// <summary>
    /// Paquete clasificado para expedición en CTA.
    /// → Si es CTA origen: Estado ClasificadoParaExpedicion.
    /// → Si es CTA destino (recibido por troncal): Estado AsignadoARuta + notificación de listo para reparto.
    /// </summary>
    private async Task<ScanResultDto> ProcesarClasificacion(ScanRequestDto req, string expedicion)
    {
        if (!req.CtaId.HasValue)
            return Error(req, "Se requiere el CTA para clasificación");

        string? detalles = null;

        // Resolver CTA destino si se proporciona CP
        if (!string.IsNullOrEmpty(req.CodigoPostalDestino))
        {
            var ctaDestino = await _clasificacionService.ResolverCtaDestino(req.CodigoPostalDestino);
            if (ctaDestino != null)
                detalles = $"Destino resuelto: {ctaDestino.CtaCodigo} ({ctaDestino.Provincia})";
        }

        // Detectar si es clasificación de última milla (CTA destino ha recibido un troncal para esta expedición)
        var movimientoRecibido = await _movimientoRepo.GetRecibidoByExpedicionAndCtaDestinoAsync(expedicion, req.CtaId.Value);
        var esUltimaMilla = movimientoRecibido != null;

        string estadoNuevo;
        string descripcion;

        if (esUltimaMilla)
        {
            estadoNuevo = "AsignadoARuta";
            descripcion = $"Paquete clasificado para última milla en {req.CtaCodigo} — listo para reparto";

            // Notificar al CTA que el paquete está disponible para reparto
            await _notificacionService.NotificarGeneralCta(
                req.CtaId.Value, req.CtaCodigo ?? "",
                "📦 Paquete listo para reparto",
                $"El paquete {expedicion} ha sido clasificado en {req.CtaCodigo} y está disponible para asignar a ruta de reparto.");
        }
        else
        {
            estadoNuevo = "ClasificadoParaExpedicion";
            descripcion = $"Paquete clasificado en {req.CtaCodigo}";
        }

        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = estadoNuevo,
            TipoUbicacion = TipoUbicacion.Cta.ToString(),
            UbicacionId = req.CtaId,
            UbicacionNombre = req.CtaCodigo,
            UbicacionCodigo = req.CtaCodigo,
            OperarioNombre = req.OperarioNombre,
            Descripcion = descripcion,
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        await _ciudadanoNotifier.NotificarEstadoAsync(
            null, expedicion, estadoNuevo,
            esUltimaMilla
                ? $"Paquete listo para reparto en {req.CtaCodigo}"
                : $"Clasificado para expedición en {req.CtaCodigo}");

        var result = Exito(req, expedicion, estadoNuevo, descripcion, req.CtaCodigo);
        result.Detalles = detalles;
        return result;
    }

    /// <summary>
    /// Paquete despachado en movimiento troncal (sale del CTA origen).
    /// → Estado: EnTransitoHaciaCentroDestino
    /// → Busca y despacha el movimiento troncal pendiente del paquete.
    /// </summary>
    private async Task<ScanResultDto> ProcesarDespachoTroncal(ScanRequestDto req, string expedicion)
    {
        if (!req.CtaId.HasValue)
            return Error(req, "Se requiere el CTA para despacho troncal");

        // Buscar movimiento programado de este paquete desde este CTA
        var movimiento = await _movimientoRepo.GetProgramadoByExpedicionAndCtaOrigenAsync(expedicion, req.CtaId.Value);

        string? detalles = null;

        if (movimiento != null)
        {
            // Despachar el movimiento
            var resultado = await _movimientoService.DespacharMovimiento(movimiento.Id);
            if (resultado != null)
                detalles = $"Movimiento {movimiento.Id} despachado hacia {resultado.CtaDestinoCodigo}";
        }

        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = "EnTransitoHaciaCentroDestino",
            TipoUbicacion = TipoUbicacion.Cta.ToString(),
            UbicacionId = req.CtaId,
            UbicacionNombre = req.CtaCodigo,
            UbicacionCodigo = req.CtaCodigo,
            OperarioNombre = req.OperarioNombre,
            Descripcion = $"Paquete despachado desde {req.CtaCodigo} en transporte troncal",
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        await _ciudadanoNotifier.NotificarEstadoAsync(
            null, expedicion, "EnTransitoHaciaCentroDestino",
            $"Paquete despachado desde {req.CtaCodigo}");

        var result = Exito(req, expedicion, "EnTransitoHaciaCentroDestino",
            $"Despachado desde {req.CtaCodigo}",
            req.CtaCodigo);
        result.Detalles = detalles;
        return result;
    }

    /// <summary>
    /// Paquete recibido en CTA destino tras movimiento troncal.
    /// → Estado: RecibidoEnCentroDestino
    /// → Busca y marca como recibido el movimiento troncal.
    /// </summary>
    private async Task<ScanResultDto> ProcesarRecepcionTroncal(ScanRequestDto req, string expedicion)
    {
        if (!req.CtaId.HasValue)
            return Error(req, "Se requiere el CTA para recepción troncal");

        // Buscar movimiento en tránsito hacia este CTA
        var movimiento = await _movimientoRepo.GetEnTransitoByExpedicionAndCtaDestinoAsync(expedicion, req.CtaId.Value);

        string? detalles = null;

        if (movimiento != null)
        {
            var resultado = await _movimientoService.RecibirMovimiento(movimiento.Id);
            if (resultado != null)
                detalles = $"Movimiento recibido desde {resultado.CtaOrigenCodigo}";
        }

        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = "RecibidoEnCentroDestino",
            TipoUbicacion = TipoUbicacion.Cta.ToString(),
            UbicacionId = req.CtaId,
            UbicacionNombre = req.CtaCodigo,
            UbicacionCodigo = req.CtaCodigo,
            OperarioNombre = req.OperarioNombre,
            Descripcion = $"Paquete recibido en CTA destino {req.CtaCodigo}",
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        // Notificar al CTA destino (SignalR interno)
        await _notificacionService.NotificarPaqueteRecibidoEnCta(
            req.CtaId.Value, req.CtaCodigo ?? "", expedicion,
            req.EsUrgente, "", "Recibido tras movimiento troncal");

        // Crear tarea de Clasificación automática en el CTA destino
        var autoAsignacion = await AutoAsignarTareaClasificacionEnCtaAsync(
            expedicion, req.CtaId.Value, req.CtaCodigo ?? "", req.EsUrgente,
            "Clasificación de última milla tras recepción troncal");

        // Notificar a Ciudadano (tracking público)
        await _ciudadanoNotifier.NotificarEstadoAsync(
            null, expedicion, "RecibidoEnCentroDestino",
            $"Recibido en CTA destino {req.CtaCodigo}");

        var result = Exito(req, expedicion, "RecibidoEnCentroDestino",
            $"Recibido en CTA destino {req.CtaCodigo}",
            req.CtaCodigo);
        result.Detalles = detalles;
        result.NotificacionEnviada = true;
        return result;
    }

    /// <summary>
    /// Paquete entregado a la oficina de destino para recogida o reparto.
    /// → Estado: DepositadoEnOficina
    /// </summary>
    private async Task<ScanResultDto> ProcesarEntregaOficinaDestino(ScanRequestDto req, string expedicion)
    {
        if (!req.OficinaJsonId.HasValue)
            return Error(req, "Se requiere la oficina para entrega");

        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = "DepositadoEnOficina",
            TipoUbicacion = TipoUbicacion.Oficina.ToString(),
            UbicacionId = req.OficinaJsonId,
            UbicacionNombre = req.OficinaNombre,
            OperarioNombre = req.OperarioNombre,
            Descripcion = $"Paquete depositado en oficina de destino {req.OficinaNombre}",
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        await _ciudadanoNotifier.NotificarEstadoAsync(
            null, expedicion, "DepositadoEnOficina",
            $"Paquete disponible en {req.OficinaNombre}");

        return Exito(req, expedicion, "DepositadoEnOficina",
            $"Depositado en oficina {req.OficinaNombre}",
            req.OficinaNombre);
    }

    /// <summary>
    /// Paquete sale de la oficina para reparto a domicilio.
    /// → Estado: EnReparto
    /// </summary>
    private async Task<ScanResultDto> ProcesarSalidaAReparto(ScanRequestDto req, string expedicion)
    {
        if (!req.OficinaJsonId.HasValue)
            return Error(req, "Se requiere la oficina para salida a reparto");

        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = "EnReparto",
            TipoUbicacion = TipoUbicacion.Oficina.ToString(),
            UbicacionId = req.OficinaJsonId,
            UbicacionNombre = req.OficinaNombre,
            OperarioNombre = req.OperarioNombre,
            Descripcion = $"Paquete en reparto desde oficina {req.OficinaNombre}",
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        await _ciudadanoNotifier.NotificarEstadoAsync(
            null, expedicion, "EnReparto",
            $"En reparto desde {req.OficinaNombre}");

        return Exito(req, expedicion, "EnReparto",
            $"En reparto desde {req.OficinaNombre}",
            req.OficinaNombre);
    }

    // ═══════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════

    private async Task<HistorialEventoInternoDto> RegistrarHistorial(
        string expedicion, CrearHistorialEventoDto dto)
    {
        // Obtener último evento para saber el estado previo
        var ultimoEvento = await _historialService.ObtenerUltimoEvento(expedicion);
        dto.EstadoPrevio = ultimoEvento?.Estado;

        return await _historialService.RegistrarEvento(dto);
    }

    private ScanResultDto Exito(ScanRequestDto req, string expedicion,
        string estadoNuevo, string mensaje, string? ubicacion)
    {
        return new ScanResultDto
        {
            Exito = true,
            NumeroExpedicion = expedicion,
            ModoOperacion = req.ModoOperacion,
            ModoDescripcion = DescripcionModos.GetValueOrDefault(req.ModoOperacion, req.ModoOperacion),
            EstadoNuevo = estadoNuevo,
            Mensaje = mensaje,
            UbicacionNombre = ubicacion
        };
    }

    private static ScanResultDto Error(ScanRequestDto req, string mensaje)
    {
        return new ScanResultDto
        {
            Exito = false,
            NumeroExpedicion = req.CodigoEscaneado,
            ModoOperacion = req.ModoOperacion,
            ModoDescripcion = DescripcionModos.GetValueOrDefault(req.ModoOperacion, req.ModoOperacion),
            Mensaje = mensaje
        };
    }

    // ─── Helper: auto-asignar tarea Clasificacion en CTA ───

    /// <summary>
    /// Crea una tarea de Clasificación asignada al OperarioCTA con menor carga en el CTA indicado.
    /// Aplica idempotencia por NumeroExpedicion + TipoTarea + CtaId.
    /// Usado al recibir un paquete vía troncal en el CTA destino.
    /// </summary>
    private async Task<(bool Success, bool Idempotent, string Message)> AutoAsignarTareaClasificacionEnCtaAsync(
        string numeroExpedicion, int ctaId, string ctaCodigo, bool esUrgente, string? observaciones = null)
    {
        try
        {
            var existente = await _asignacionRepo.GetByExpedicionTipoCtaAsync(
                numeroExpedicion, TipoTarea.Clasificacion, ctaId);

            if (existente != null)
            {
                _logger.LogInformation(
                    "Tarea Clasificacion ya existe para {Expedicion} en CTA {Cta} (idempotente)",
                    numeroExpedicion, ctaCodigo);
                return (true, true, "La tarea de clasificación ya existía en este CTA.");
            }

            var operariosActivos = await _operarioRepo.GetByCtaIdAsync(ctaId, soloActivos: true);
            var candidatos = operariosActivos
                .Where(o => o.Rol == RolOperario.OperarioCTA || o.Rol == RolOperario.Supervisor)
                .ToList();

            if (candidatos.Count == 0)
            {
                _logger.LogWarning(
                    "No hay OperarioCTA activo en CTA {Cta} para auto-asignar clasificación de {Expedicion}",
                    ctaCodigo, numeroExpedicion);
                return (false, false, "No hay operarios CTA activos en este CTA para asignar la tarea.");
            }

            // Seleccionar el operario con menor carga
            var cargas = new List<(OperarioCta Operario, int Carga)>();
            foreach (var op in candidatos)
            {
                var pendientes = await _asignacionRepo.CountByOperarioAndEstadoAsync(op.Id, EstadoTarea.Pendiente);
                var enProgreso = await _asignacionRepo.CountByOperarioAndEstadoAsync(op.Id, EstadoTarea.EnProgreso);
                cargas.Add((op, pendientes + enProgreso));
            }

            var operarioAsignado = cargas.OrderBy(c => c.Carga).ThenBy(c => c.Operario.Id).First().Operario;
            var asignador = candidatos.FirstOrDefault(o => o.Rol == RolOperario.OperarioCTA) ?? operarioAsignado;

            await _asignacionRepo.CreateAsync(new AsignacionPaquete
            {
                NumeroExpedicion = numeroExpedicion,
                OperarioAsignadoId = operarioAsignado.Id,
                AsignadoPorId = asignador.Id,
                CtaId = ctaId,
                TipoTarea = TipoTarea.Clasificacion,
                EsUrgente = esUrgente,
                Observaciones = observaciones ?? "Clasificación de última milla — asignación automática tras recepción troncal"
            });

            await _notificacionService.NotificarTareaAsignada(
                operarioAsignado.Id, ctaId, ctaCodigo,
                numeroExpedicion, TipoTarea.Clasificacion.ToString(),
                esUrgente, asignador.NombreCompleto);

            _logger.LogInformation(
                "Tarea Clasificacion auto-asignada a {Operario} en CTA {Cta} para {Expedicion}",
                operarioAsignado.CodigoEmpleado, ctaCodigo, numeroExpedicion);

            return (true, false, $"Tarea de clasificación asignada a {operarioAsignado.NombreCompleto}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error en auto-asignación de tarea Clasificacion para {Expedicion} en CTA {Cta}",
                numeroExpedicion, ctaCodigo);
            return (false, false, "Error al crear la asignación automática.");
        }
    }
}
