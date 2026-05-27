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
    private readonly ICiudadanoEnvioLookupService _ciudadanoLookup;
    private readonly ICiudadanoEstadoNotifierService _ciudadanoNotifier;
    private readonly IRepartoBandejaService _repartoBandeja;
    private readonly IAsignacionPaqueteRepository _asignacionRepo;
    private readonly IOperarioCtaRepository _operarioRepo;
    private readonly IOperarioOficinaRepository _operarioOficinaRepo;
    private readonly IOficinaPostalService _oficinaService;
    private readonly ILogger<ScanProcessorService> _logger;

    // Mapeo modo → descripción legible
    private static readonly Dictionary<string, string> DescripcionModos = new()
    {
        [ModosEscaneo.RecepcionOficina] = "Recepción en oficina de origen",
        [ModosEscaneo.SalidaOficinaACta] = "Salida de oficina hacia CTA origen",
        [ModosEscaneo.RecepcionCta] = "Recepción en CTA",
        [ModosEscaneo.Clasificacion] = "Clasificación para expedición",
        [ModosEscaneo.DespachoTroncal] = "Despacho troncal (CTA → CTA)",
        [ModosEscaneo.RecepcionTroncal] = "Recepción troncal en CTA destino",
        [ModosEscaneo.DisponibleParaReparto] = "Disponible para reparto (última milla)",
        [ModosEscaneo.EntregaOficinaDestino] = "Entrega a oficina de destino",
        [ModosEscaneo.SalidaAReparto] = "Salida a reparto a domicilio"
    };

    // Mapeo modo → estado interno del envío
    private static readonly Dictionary<string, string> EstadoInternoModos = new()
    {
        [ModosEscaneo.RecepcionOficina] = "RecogidoEnOrigen",
        [ModosEscaneo.SalidaOficinaACta] = "EnTransitoACentroOrigen",
        [ModosEscaneo.RecepcionCta] = "RecibidoEnCentroOrigen",
        [ModosEscaneo.Clasificacion] = "ClasificadoParaExpedicion",
        [ModosEscaneo.DespachoTroncal] = "EnTransitoHaciaCentroDestino",
        [ModosEscaneo.RecepcionTroncal] = "RecibidoEnCentroDestino",
        [ModosEscaneo.DisponibleParaReparto] = "DisponibleParaReparto",
        [ModosEscaneo.EntregaOficinaDestino] = "DepositadoEnOficina",
        [ModosEscaneo.SalidaAReparto] = "EnReparto"
    };

    public ScanProcessorService(
        IMovimientoPaqueteRepository movimientoRepo,
        IHistorialService historialService,
        IClasificacionService clasificacionService,
        IMovimientoService movimientoService,
        INotificacionService notificacionService,
        ICiudadanoEnvioLookupService ciudadanoLookup,
        ICiudadanoEstadoNotifierService ciudadanoNotifier,
        IRepartoBandejaService repartoBandeja,
        IAsignacionPaqueteRepository asignacionRepo,
        IOperarioCtaRepository operarioRepo,
        IOperarioOficinaRepository operarioOficinaRepo,
        IOficinaPostalService oficinaService,
        ILogger<ScanProcessorService> logger)
    {
        _movimientoRepo = movimientoRepo;
        _historialService = historialService;
        _clasificacionService = clasificacionService;
        _movimientoService = movimientoService;
        _notificacionService = notificacionService;
        _ciudadanoLookup = ciudadanoLookup;
        _ciudadanoNotifier = ciudadanoNotifier;
        _repartoBandeja = repartoBandeja;
        _asignacionRepo = asignacionRepo;
        _operarioRepo = operarioRepo;
        _operarioOficinaRepo = operarioOficinaRepo;
        _oficinaService = oficinaService;
        _logger = logger;
    }

    /// <summary>
    /// Indica si el envío se entrega en oficina (true) o a domicilio (false/desconocido).
    /// </summary>
    private async Task<(bool esOficina, EnvioInternoServiceLookupDto? envio)> ResolverTipoEntregaAsync(string expedicion)
    {
        var envio = await _ciudadanoLookup.ObtenerAsync(expedicion);
        var esOficina = envio is not null && string.Equals(envio.TipoEntrega, "Oficina", StringComparison.OrdinalIgnoreCase);
        return (esOficina, envio);
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
                ModosEscaneo.SalidaOficinaACta => await ProcesarSalidaOficinaACta(request, expedicion),
                ModosEscaneo.RecepcionCta => await ProcesarRecepcionCta(request, expedicion),
                ModosEscaneo.Clasificacion => await ProcesarClasificacion(request, expedicion),
                ModosEscaneo.DespachoTroncal => await ProcesarDespachoTroncal(request, expedicion),
                ModosEscaneo.RecepcionTroncal => await ProcesarRecepcionTroncal(request, expedicion),
                ModosEscaneo.DisponibleParaReparto => await ProcesarDisponibleParaReparto(request, expedicion),
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
            expedicion, expedicion, "RecogidoEnOrigen",
            $"Paquete recibido en {req.OficinaNombre}");

        // Cerrar tarea RecepcionOficina del OperarioOficina en esta oficina
        await CerrarTareaOficinaSiExisteAsync(expedicion, TipoTarea.RecepcionOficina, req.OficinaJsonId.Value);

        // Encadenar: crear la siguiente tarea de oficina (SalidaOficinaACta)
        // para que el OperarioOficina la vea en su listado de pendientes.
        await AutoAsignarTareaEnOficinaAsync(
            TipoTarea.SalidaOficinaACta,
            expedicion, req.OficinaJsonId.Value, req.OficinaNombre, req.EsUrgente,
            "Tarea generada tras recepción en oficina");

        return Exito(req, expedicion, "RecogidoEnOrigen",
            $"Paquete recibido en {req.OficinaNombre}",
            req.OficinaNombre);
    }

    /// <summary>
    /// Paquete sale de la oficina origen hacia el CTA origen.
    /// → Estado: EnTransitoACentroOrigen
    /// → Notifica al CTA origen (éste sí recibe ahora la señal de paquete entrante).
    /// → Crea automáticamente la tarea de Clasificación en el CTA origen.
    /// </summary>
    private async Task<ScanResultDto> ProcesarSalidaOficinaACta(ScanRequestDto req, string expedicion)
    {
        if (!req.OficinaJsonId.HasValue)
            return Error(req, "Se requiere la oficina para registrar la salida hacia CTA");

        // Resolver CTA origen a partir del CP origen (o del CP de la oficina si no viene en el request).
        ResolverCtaResponseDto? ctaOrigen = null;
        var cpOrigen = req.CodigoPostalOrigen;

        // Si el request no trae CP origen, consultarlo en Ciudadano (fuente de verdad del envío).
        if (string.IsNullOrWhiteSpace(cpOrigen))
        {
            try
            {
                var envio = await _ciudadanoLookup.ObtenerAsync(expedicion);
                if (envio != null && !string.IsNullOrWhiteSpace(envio.CodigoPostalOrigen))
                {
                    cpOrigen = envio.CodigoPostalOrigen;
                    _logger.LogInformation(
                        "SalidaOficinaACta {Expedicion}: CP origen {Cp} obtenido vía lookup en Ciudadano",
                        expedicion, cpOrigen);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "SalidaOficinaACta {Expedicion}: fallo al obtener envío de Ciudadano para resolver CP origen",
                    expedicion);
            }
        }

        if (!string.IsNullOrWhiteSpace(cpOrigen))
        {
            ctaOrigen = await _clasificacionService.ResolverCtaDestino(cpOrigen);
        }

        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = "EnTransitoACentroOrigen",
            TipoUbicacion = TipoUbicacion.Oficina.ToString(),
            UbicacionId = req.OficinaJsonId,
            UbicacionNombre = req.OficinaNombre,
            OperarioNombre = req.OperarioNombre,
            Descripcion = $"Paquete en camino desde oficina {req.OficinaNombre} hacia CTA{(ctaOrigen != null ? " " + ctaOrigen.CtaCodigo : "")}",
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        string? detalles = null;
        if (ctaOrigen != null)
        {
            // ⚠️ ORDEN IMPORTANTE: primero crear la tarea, luego notificar al CTA.
            // Si se notifica antes, el OperarioCTA recibe el evento PaqueteRecibidoEnCta
            // y su UI llama a /asignaciones/cta/{id} ANTES de que la tarea esté en BD,
            // devolviendo lista vacía → el operario ve la notificación sin la asignación.
            var autoAsign = await AutoAsignarTareaEnCtaAsync(
                TipoTarea.Recepcion,
                expedicion, ctaOrigen.CtaId, ctaOrigen.CtaCodigo, req.EsUrgente,
                "Tarea generada al salir el paquete de la oficina origen");
            detalles = autoAsign.Message;

            // Notificar al CTA origen (ahora sí recibe la señal de paquete entrante).
            await _notificacionService.NotificarPaqueteRecibidoEnCta(
                ctaOrigen.CtaId, ctaOrigen.CtaCodigo, expedicion,
                req.EsUrgente, ctaOrigen.Provincia, "Paquete en camino desde oficina origen");
        }
        else
        {
            _logger.LogWarning(
                "SalidaOficinaACta {Expedicion}: no se pudo resolver CTA origen (CP origen: {Cp})",
                expedicion, req.CodigoPostalOrigen);
        }

        await _ciudadanoNotifier.NotificarEstadoAsync(
            expedicion, expedicion, "EnTransitoACentroOrigen",
            $"Tu paquete ha salido de la oficina {req.OficinaNombre} hacia el centro de tratamiento");

        var result = Exito(req, expedicion, "EnTransitoACentroOrigen",
            $"Paquete en tránsito hacia CTA{(ctaOrigen != null ? " " + ctaOrigen.CtaCodigo : "")}",
            req.OficinaNombre);
        result.Detalles = detalles;
        result.NotificacionEnviada = ctaOrigen != null;

        // Cerrar tarea SalidaOficinaACta del OperarioOficina en esta oficina
        await CerrarTareaOficinaSiExisteAsync(expedicion, TipoTarea.SalidaOficinaACta, req.OficinaJsonId.Value);

        return result;
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

        // Notificar a Ciudadano (tracking público)
        await _ciudadanoNotifier.NotificarEstadoAsync(
            expedicion, expedicion, "RecibidoEnCentroOrigen",
            $"Paquete admitido en {req.CtaCodigo}");

        var result = Exito(req, expedicion, "RecibidoEnCentroOrigen",
            $"Paquete admitido en {req.CtaCodigo}",
            req.CtaCodigo);

        result.MovimientoTroncalCreado = movimientoCreado;
        result.NotificacionEnviada = true;

        // Encadenado: cerrar Recepción y crear Clasificacion en el mismo CTA.
        // ⚠️ ORDEN IMPORTANTE: crear la siguiente tarea ANTES de notificar al CTA.
        // Si se notifica antes, el OperarioCTA recibe PaqueteRecibidoEnCta y su UI
        // refresca el listado /asignaciones/cta/{id} sin que la nueva Clasificacion
        // esté aún en BD, mostrando "Sin asignaciones".
        await CerrarTareaSiExisteAsync(expedicion, TipoTarea.Recepcion, req.CtaId.Value);
        var autoClasif = await AutoAsignarTareaEnCtaAsync(
            TipoTarea.Clasificacion,
            expedicion, req.CtaId.Value, req.CtaCodigo ?? "", req.EsUrgente,
            "Tarea generada tras recepción en CTA");
        result.Detalles = string.IsNullOrEmpty(detalles) ? autoClasif.Message : $"{detalles}. {autoClasif.Message}";

        // Notificar al CTA (SignalR interno) — después de crear la tarea.
        await _notificacionService.NotificarPaqueteRecibidoEnCta(
            req.CtaId.Value, req.CtaCodigo ?? "", expedicion,
            req.EsUrgente, "", req.Observaciones);

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

        // Resolver TipoEntrega para bifurcar el flujo de última milla:
        //   - Domicilio → DisponibleParaReparto + bandeja del JefeReparto
        //   - Oficina   → EntregaCtaAOficinaDestino en la oficina destino (NO pasa por reparto)
        var (esEntregaEnOficina, envioLookup) = esUltimaMilla
            ? await ResolverTipoEntregaAsync(expedicion)
            : (false, null);

        string estadoNuevo;
        string descripcion;

        if (esUltimaMilla)
        {
            if (esEntregaEnOficina)
            {
                estadoNuevo = "PreparadoParaOficinaDestino";
                descripcion = $"Paquete clasificado en {req.CtaCodigo} — preparado para envío a la oficina destino";
            }
            else
            {
                estadoNuevo = "AsignadoARuta";
                descripcion = $"Paquete clasificado para última milla en {req.CtaCodigo} — listo para reparto";

                // Notificar al CTA que el paquete está disponible para reparto
                await _notificacionService.NotificarGeneralCta(
                    req.CtaId.Value, req.CtaCodigo ?? "",
                    "📦 Paquete listo para reparto",
                    $"El paquete {expedicion} ha sido clasificado en {req.CtaCodigo} y está disponible para asignar a ruta de reparto.");
            }
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
            expedicion, expedicion, estadoNuevo,
            esUltimaMilla
                ? (esEntregaEnOficina
                    ? $"Paquete preparado en {req.CtaCodigo} para entrega en oficina destino"
                    : $"Paquete listo para reparto en {req.CtaCodigo}")
                : $"Clasificado para expedición en {req.CtaCodigo}");

        var result = Exito(req, expedicion, estadoNuevo, descripcion, req.CtaCodigo);
        result.Detalles = detalles;

        // Encadenado: cerrar Clasificacion actual y crear la siguiente tarea según contexto
        await CerrarTareaSiExisteAsync(expedicion, TipoTarea.Clasificacion, req.CtaId.Value);

        if (esUltimaMilla)
        {
            if (esEntregaEnOficina)
            {
                // CTA destino → envío de oficina: NO pasa por reparto.
                // Crear tarea EntregaCtaAOficinaDestino en la oficina destino para
                // que el OperarioOficina la reciba físicamente.
                var oficinaDestinoId = envioLookup?.OficinaDestinoId;
                if (oficinaDestinoId is int oficId)
                {
                    var oficina = _oficinaService.ObtenerPorId(oficId);
                    var siguiente = await AutoAsignarTareaEnOficinaAsync(
                        TipoTarea.EntregaCtaAOficinaDestino,
                        expedicion, oficId, oficina?.Nombre, req.EsUrgente,
                        $"Paquete clasificado en {req.CtaCodigo}; pendiente de recepción en oficina destino");
                    result.Detalles = string.IsNullOrEmpty(result.Detalles)
                        ? siguiente.Message
                        : $"{result.Detalles}. {siguiente.Message}";
                }
                else
                {
                    _logger.LogWarning(
                        "{Expedicion} es entrega en oficina pero no se pudo resolver OficinaDestinoId desde Ciudadano; no se crea la tarea de oficina destino.",
                        expedicion);
                    result.Detalles = string.IsNullOrEmpty(result.Detalles)
                        ? "Envío de oficina sin oficina destino resuelta."
                        : $"{result.Detalles}. Envío de oficina sin oficina destino resuelta.";
                }
            }
            else
            {
                // CTA destino → generar tarea DisponibleParaReparto para cerrar el ciclo CTA
                var siguiente = await AutoAsignarTareaEnCtaAsync(
                    TipoTarea.DisponibleParaReparto,
                    expedicion, req.CtaId.Value, req.CtaCodigo ?? "", req.EsUrgente,
                    "Listo para marcar como disponible para reparto");
                result.Detalles = string.IsNullOrEmpty(result.Detalles) ? siguiente.Message : $"{result.Detalles}. {siguiente.Message}";
            }
        }
        else
        {
            // CTA origen → si hay troncal programado, generar tarea DespachoTroncal
            var troncalProgramado = await _movimientoRepo.GetProgramadoByExpedicionAndCtaOrigenAsync(expedicion, req.CtaId.Value);
            if (troncalProgramado != null)
            {
                var siguiente = await AutoAsignarTareaEnCtaAsync(
                    TipoTarea.DespachoTroncal,
                    expedicion, req.CtaId.Value, req.CtaCodigo ?? "", req.EsUrgente,
                    "Despacho troncal pendiente tras clasificación");
                result.Detalles = string.IsNullOrEmpty(result.Detalles) ? siguiente.Message : $"{result.Detalles}. {siguiente.Message}";
            }
        }

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
        int? ctaDestinoId = null;
        string? ctaDestinoCodigo = null;

        if (movimiento != null)
        {
            // Despachar el movimiento
            var resultado = await _movimientoService.DespacharMovimiento(movimiento.Id);
            if (resultado != null)
            {
                detalles = $"Movimiento {movimiento.Id} despachado hacia {resultado.CtaDestinoCodigo}";
                ctaDestinoId = resultado.CtaDestinoId;
                ctaDestinoCodigo = resultado.CtaDestinoCodigo;
            }
            else
            {
                ctaDestinoId = movimiento.CtaDestinoId;
            }
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
            expedicion, expedicion, "EnTransitoHaciaCentroDestino",
            $"Paquete despachado desde {req.CtaCodigo}");

        var result = Exito(req, expedicion, "EnTransitoHaciaCentroDestino",
            $"Despachado desde {req.CtaCodigo}",
            req.CtaCodigo);
        result.Detalles = detalles;

        // Cerrar tarea DespachoTroncal en el CTA origen
        await CerrarTareaSiExisteAsync(expedicion, TipoTarea.DespachoTroncal, req.CtaId.Value);

        // Auto-crear tarea RecepcionTroncal en el CTA destino para que el OperarioCTA
        // de allí la vea en su listado de pendientes y notificarle por SignalR.
        // ⚠️ ORDEN IMPORTANTE: primero crear la tarea, luego notificar (mismo motivo que en
        // SalidaOficinaACta: el evento PaqueteRecibidoEnCta dispara un refresco en la UI
        // del CTA destino y si la tarea aún no está en BD, el listado llega vacío).
        if (ctaDestinoId.HasValue)
        {
            await AutoAsignarTareaEnCtaAsync(
                TipoTarea.RecepcionTroncal,
                expedicion, ctaDestinoId.Value, ctaDestinoCodigo ?? "", req.EsUrgente,
                $"Tarea generada al despachar el paquete desde {req.CtaCodigo}");

            await _notificacionService.NotificarPaqueteRecibidoEnCta(
                ctaDestinoId.Value, ctaDestinoCodigo ?? "", expedicion,
                req.EsUrgente, "", $"Paquete en tránsito desde {req.CtaCodigo}");
        }

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

        // Cerrar la tarea RecepcionTroncal del OperarioCTA que acaba de escanear.
        // Sin esto la tarea quedaría Pendiente para siempre y el operario podría
        // re-escanear infinitas veces obteniendo el mismo mensaje sin avanzar.
        await CerrarTareaSiExisteAsync(expedicion, TipoTarea.RecepcionTroncal, req.CtaId.Value);

        // ⚠️ ORDEN IMPORTANTE: crear Clasificación ANTES de notificar al CTA destino.
        // Si se notifica antes, el OperarioCTA recibe PaqueteRecibidoEnCta y su UI
        // refresca /asignaciones/cta/{id} sin que la nueva Clasificacion esté en BD,
        // mostrando "Sin asignaciones".
        var autoAsignacion = await AutoAsignarTareaEnCtaAsync(
            TipoTarea.Clasificacion,
            expedicion, req.CtaId.Value, req.CtaCodigo ?? "", req.EsUrgente,
            "Clasificación de última milla tras recepción troncal");

        // Notificar al CTA destino (SignalR interno) — después de crear la tarea.
        await _notificacionService.NotificarPaqueteRecibidoEnCta(
            req.CtaId.Value, req.CtaCodigo ?? "", expedicion,
            req.EsUrgente, "", "Recibido tras movimiento troncal");

        // Notificar a Ciudadano (tracking público)
        await _ciudadanoNotifier.NotificarEstadoAsync(
            expedicion, expedicion, "RecibidoEnCentroDestino",
            $"Recibido en CTA destino {req.CtaCodigo}");

        var result = Exito(req, expedicion, "RecibidoEnCentroDestino",
            $"Recibido en CTA destino {req.CtaCodigo}",
            req.CtaCodigo);
        result.Detalles = detalles;
        result.NotificacionEnviada = true;
        return result;
    }

    /// <summary>
    /// Paquete clasificado y listo para asignar a una ruta de reparto en el CTA destino.
    /// → Estado: DisponibleParaReparto
    /// → Cierra la tarea de Clasificación del OperarioCTA y notifica a JefeReparto.
    /// </summary>
    private async Task<ScanResultDto> ProcesarDisponibleParaReparto(ScanRequestDto req, string expedicion)
    {
        if (!req.CtaId.HasValue)
            return Error(req, "Se requiere el CTA para marcar el paquete como disponible para reparto");

        // Defensa en profundidad: los envíos con entrega en oficina nunca deben pasar
        // por el flujo de reparto. Si llegan aquí, rechazar el escaneo y dirigir al
        // operario al flujo correcto (EntregaOficinaDestino en la oficina destino).
        var (esEntregaEnOficina, _) = await ResolverTipoEntregaAsync(expedicion);
        if (esEntregaEnOficina)
        {
            return Error(req,
                "Este envío es de entrega en oficina y no debe liberarse a reparto. " +
                "Debe entregarse directamente a la oficina destino para recogida del destinatario.");
        }

        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = "DisponibleParaReparto",
            TipoUbicacion = TipoUbicacion.Cta.ToString(),
            UbicacionId = req.CtaId,
            UbicacionNombre = req.CtaCodigo,
            UbicacionCodigo = req.CtaCodigo,
            OperarioNombre = req.OperarioNombre,
            Descripcion = $"Paquete disponible para reparto en {req.CtaCodigo}",
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        // Cerrar tarea DisponibleParaReparto en CTA destino
        await CerrarTareaSiExisteAsync(expedicion, TipoTarea.DisponibleParaReparto, req.CtaId.Value);

        // ─── Bandeja del JefeReparto del CTA destino ───
        // Registramos el paquete en la bandeja persistente del microservicio Reparto.
        // ⚠️ ORDEN IMPORTANTE: registrar en la bandeja ANTES de notificar.
        // Si se notifica antes, el JefeReparto recibe PaqueteDisponibleParaReparto y
        // su UI refresca la bandeja sin que el paquete esté aún registrado en Reparto,
        // mostrando una lista vacía y bloqueando la creación de la ruta.
        await TryRegistrarEnBandejaRepartoAsync(req, expedicion);

        // Notificar a JefeReparto / equipo de reparto del CTA destino.
        await _notificacionService.NotificarPaqueteDisponibleParaReparto(
            req.CtaId.Value, req.CtaCodigo ?? "", expedicion, req.EsUrgente);

        await _ciudadanoNotifier.NotificarEstadoAsync(
            expedicion, expedicion, "DisponibleParaReparto",
            $"Tu paquete está listo para asignar a un repartidor en {req.CtaCodigo}");

        var result = Exito(req, expedicion, "DisponibleParaReparto",
            $"Paquete disponible para reparto desde {req.CtaCodigo}",
            req.CtaCodigo);
        result.NotificacionEnviada = true;
        return result;
    }

    /// <summary>
    /// Paquete entregado a la oficina de destino para recogida o reparto.
    /// Bifurca por TipoEntrega:
    ///   - "Oficina"   → DepositadoEnOficina (esperando recogida del destinatario, FIN del recorrido logístico)
    ///   - "Domicilio" → DepositadoEnOficina (paso intermedio antes de SalidaAReparto)
    /// </summary>
    private async Task<ScanResultDto> ProcesarEntregaOficinaDestino(ScanRequestDto req, string expedicion)
    {
        if (!req.OficinaJsonId.HasValue)
            return Error(req, "Se requiere la oficina para entrega");

        var (esOficina, envio) = await ResolverTipoEntregaAsync(expedicion);

        // Validación cruzada: si el envío indica oficina destino concreta, debe coincidir
        string? avisoOficina = null;
        if (esOficina && envio?.OficinaDestinoId is int oficDest && oficDest != req.OficinaJsonId.Value)
        {
            avisoOficina = $"Aviso: oficina destino esperada {oficDest} pero escaneo en {req.OficinaJsonId}";
            _logger.LogWarning("{Expedicion}: {Aviso}", expedicion, avisoOficina);
        }

        var descripcion = esOficina
            ? $"Paquete disponible para recogida en oficina {req.OficinaNombre}"
            : $"Paquete depositado en oficina de destino {req.OficinaNombre}";

        await RegistrarHistorial(expedicion, new CrearHistorialEventoDto
        {
            NumeroExpedicion = expedicion,
            Estado = "DepositadoEnOficina",
            TipoUbicacion = TipoUbicacion.Oficina.ToString(),
            UbicacionId = req.OficinaJsonId,
            UbicacionNombre = req.OficinaNombre,
            OperarioNombre = req.OperarioNombre,
            Descripcion = descripcion,
            Observaciones = req.Observaciones,
            VisibleParaCliente = true
        });

        var mensaje = esOficina
            ? $"Disponible para recogida en {req.OficinaNombre}"
            : $"Depositado en oficina {req.OficinaNombre}";

        await _ciudadanoNotifier.NotificarEstadoAsync(
            expedicion, expedicion, "DepositadoEnOficina",
            $"Paquete disponible en {req.OficinaNombre}");

        var result = Exito(req, expedicion, "DepositadoEnOficina", mensaje, req.OficinaNombre);
        result.Detalles = avisoOficina ?? (esOficina
            ? "Modalidad: entrega en oficina. Esperando recogida del destinatario."
            : "Modalidad: entrega a domicilio. Pendiente de salida a reparto.");

        // Cerrar tarea EntregaCtaAOficinaDestino del OperarioOficina en esta oficina
        await CerrarTareaOficinaSiExisteAsync(expedicion, TipoTarea.EntregaCtaAOficinaDestino, req.OficinaJsonId.Value);

        // Si la modalidad es "Oficina", encadenar EntregaAlClienteEnOficina
        // para que el OperarioOficina la vea en sus tareas pendientes.
        if (esOficina)
        {
            await AutoAsignarTareaEnOficinaAsync(
                TipoTarea.EntregaAlClienteEnOficina,
                expedicion, req.OficinaJsonId.Value, req.OficinaNombre, req.EsUrgente,
                "Tarea generada tras recepción del paquete en oficina destino");
        }

        return result;
    }

    /// <summary>
    /// Paquete sale de la oficina para reparto a domicilio.
    /// → Estado: EnReparto
    /// Rechaza el escaneo si el envío es de modalidad "Oficina" (no debe salir a reparto).
    /// </summary>
    private async Task<ScanResultDto> ProcesarSalidaAReparto(ScanRequestDto req, string expedicion)
    {
        if (!req.OficinaJsonId.HasValue)
            return Error(req, "Se requiere la oficina para salida a reparto");

        var (esOficina, _) = await ResolverTipoEntregaAsync(expedicion);
        if (esOficina)
        {
            return Error(req, "Este envío es de entrega en oficina; no debe salir a reparto. El destinatario lo recogerá en la oficina.");
        }

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
            expedicion, expedicion, "EnReparto",
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

    // ─── Helper: auto-asignar tarea en CTA ───

    /// <summary>
    /// Cierra (marca como Completada) la tarea pendiente/en progreso del tipo indicado
    /// para esta expedición y CTA. Idempotente: si no encuentra, no hace nada.
    /// </summary>
    private async Task CerrarTareaSiExisteAsync(string numeroExpedicion, TipoTarea tipo, int ctaId)
    {
        try
        {
            var existente = await _asignacionRepo.GetByExpedicionTipoCtaAsync(numeroExpedicion, tipo, ctaId);
            if (existente == null || existente.EstadoTarea == EstadoTarea.Completada || existente.EstadoTarea == EstadoTarea.Cancelada)
                return;

            existente.EstadoTarea = EstadoTarea.Completada;
            existente.FechaCompletada = DateTime.UtcNow;
            if (existente.FechaInicio == null)
                existente.FechaInicio = DateTime.UtcNow;
            await _asignacionRepo.UpdateAsync(existente);

            _logger.LogInformation(
                "Tarea {Tipo} cerrada automáticamente para {Expedicion} en CTA {Cta}",
                tipo, numeroExpedicion, ctaId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "No se pudo cerrar tarea {Tipo} para {Expedicion} en CTA {Cta}",
                tipo, numeroExpedicion, ctaId);
        }
    }

    /// <summary>
    /// Cierra (marca como Completada) la tarea pendiente/en progreso del tipo indicado
    /// para esta expedición y oficina. Idempotente.
    /// </summary>
    private async Task CerrarTareaOficinaSiExisteAsync(string numeroExpedicion, TipoTarea tipo, int oficinaJsonId)
    {
        try
        {
            var existente = await _asignacionRepo.GetByExpedicionTipoOficinaAsync(numeroExpedicion, tipo, oficinaJsonId);
            if (existente == null || existente.EstadoTarea == EstadoTarea.Completada || existente.EstadoTarea == EstadoTarea.Cancelada)
                return;

            existente.EstadoTarea = EstadoTarea.Completada;
            existente.FechaCompletada = DateTime.UtcNow;
            if (existente.FechaInicio == null)
                existente.FechaInicio = DateTime.UtcNow;
            await _asignacionRepo.UpdateAsync(existente);

            _logger.LogInformation(
                "Tarea {Tipo} cerrada automáticamente para {Expedicion} en oficina {Ofi}",
                tipo, numeroExpedicion, oficinaJsonId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "No se pudo cerrar tarea {Tipo} para {Expedicion} en oficina {Ofi}",
                tipo, numeroExpedicion, oficinaJsonId);
        }
    }

    /// <summary>
    /// Crea una tarea del tipo indicado, asignada al OperarioCTA con menor carga en el CTA.
    /// Aplica idempotencia por NumeroExpedicion + TipoTarea + CtaId.
    /// </summary>
    private async Task<(bool Success, bool Idempotent, string Message)> AutoAsignarTareaEnCtaAsync(
        TipoTarea tipoTarea,
        string numeroExpedicion,
        int ctaId,
        string ctaCodigo,
        bool esUrgente,
        string? observaciones = null,
        int? preferidoOperarioId = null)
    {
        try
        {
            var existente = await _asignacionRepo.GetByExpedicionTipoCtaAsync(
                numeroExpedicion, tipoTarea, ctaId);

            if (existente != null)
            {
                _logger.LogInformation(
                    "Tarea {Tipo} ya existe para {Expedicion} en CTA {Cta} (idempotente)",
                    tipoTarea, numeroExpedicion, ctaCodigo);
                return (true, true, $"La tarea de {tipoTarea} ya existía en este CTA.");
            }

            var operariosActivos = await _operarioRepo.GetByCtaIdAsync(ctaId, soloActivos: true);
            // Solo OperarioCTA puede ejecutar escaneos en el CTA (ScanController excluye Supervisor
            // por política: "Supervisor solo supervisión y dashboards"). Si se incluye Supervisor
            // como candidato, la tarea le caería y al intentar escanearla recibiría 403 Forbidden.
            var candidatos = operariosActivos
                .Where(o => o.Rol == RolOperario.OperarioCTA)
                .ToList();

            if (candidatos.Count == 0)
            {
                _logger.LogWarning(
                    "No hay OperarioCTA activo en CTA {Cta} para auto-asignar {Tipo} de {Expedicion}",
                    ctaCodigo, tipoTarea, numeroExpedicion);
                return (false, false, $"No hay operarios CTA activos en este CTA para asignar {tipoTarea}.");
            }

            OperarioCta operarioAsignado;
            if (preferidoOperarioId.HasValue && candidatos.Any(c => c.Id == preferidoOperarioId.Value))
            {
                operarioAsignado = candidatos.First(c => c.Id == preferidoOperarioId.Value);
            }
            else
            {
                var cargas = new List<(OperarioCta Operario, int Carga)>();
                foreach (var op in candidatos)
                {
                    var pendientes = await _asignacionRepo.CountByOperarioAndEstadoAsync(op.Id, EstadoTarea.Pendiente);
                    var enProgreso = await _asignacionRepo.CountByOperarioAndEstadoAsync(op.Id, EstadoTarea.EnProgreso);
                    cargas.Add((op, pendientes + enProgreso));
                }

                operarioAsignado = cargas.OrderBy(c => c.Carga).ThenBy(c => c.Operario.Id).First().Operario;
            }

            var asignador = candidatos.FirstOrDefault(o => o.Rol == RolOperario.OperarioCTA) ?? operarioAsignado;

            await _asignacionRepo.CreateAsync(new AsignacionPaquete
            {
                NumeroExpedicion = numeroExpedicion,
                OperarioAsignadoId = operarioAsignado.Id,
                AsignadoPorId = asignador.Id,
                CtaId = ctaId,
                TipoTarea = tipoTarea,
                EsUrgente = esUrgente,
                Observaciones = observaciones ?? $"Tarea {tipoTarea} — auto-asignada por flujo de escaneo"
            });

            await _notificacionService.NotificarTareaAsignada(
                operarioAsignado.Id, ctaId, ctaCodigo,
                numeroExpedicion, tipoTarea.ToString(),
                esUrgente, asignador.NombreCompleto);

            _logger.LogInformation(
                "Tarea {Tipo} auto-asignada a {Operario} en CTA {Cta} para {Expedicion}",
                tipoTarea, operarioAsignado.CodigoEmpleado, ctaCodigo, numeroExpedicion);

            return (true, false, $"Tarea de {tipoTarea} asignada a {operarioAsignado.NombreCompleto}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error en auto-asignación de tarea {Tipo} para {Expedicion} en CTA {Cta}",
                tipoTarea, numeroExpedicion, ctaCodigo);
            return (false, false, $"Error al crear la asignación automática de {tipoTarea}.");
        }
    }

    /// <summary>
    /// Crea una tarea del tipo indicado, asignada al OperarioOficina con menor carga en la oficina.
    /// Aplica idempotencia por NumeroExpedicion + TipoTarea + OficinaJsonId.
    /// Si no hay operarios activos en la oficina, registra warning y continúa (no rompe el flujo).
    /// </summary>
    private async Task<(bool Success, bool Idempotent, string Message)> AutoAsignarTareaEnOficinaAsync(
        TipoTarea tipoTarea,
        string numeroExpedicion,
        int oficinaJsonId,
        string? oficinaNombre,
        bool esUrgente,
        string? observaciones = null)
    {
        try
        {
            var existente = await _asignacionRepo.GetByExpedicionTipoOficinaAsync(
                numeroExpedicion, tipoTarea, oficinaJsonId);

            if (existente != null)
            {
                _logger.LogInformation(
                    "Tarea {Tipo} ya existe para {Expedicion} en oficina {Ofi} (idempotente)",
                    tipoTarea, numeroExpedicion, oficinaJsonId);
                return (true, true, $"La tarea de {tipoTarea} ya existía en esta oficina.");
            }

            var operariosActivos = await _operarioOficinaRepo.GetByOficinaAsync(oficinaJsonId, soloActivos: true);
            if (operariosActivos.Count == 0)
            {
                _logger.LogWarning(
                    "No hay OperarioOficina activo en oficina {Ofi} para auto-asignar {Tipo} de {Expedicion}",
                    oficinaJsonId, tipoTarea, numeroExpedicion);
                return (false, false, $"No hay operarios de oficina activos para asignar {tipoTarea}.");
            }

            // Elegir el de menor carga (pendientes + en progreso)
            var cargas = new List<(OperarioOficina Operario, int Carga)>();
            foreach (var op in operariosActivos)
            {
                var asignaciones = await _asignacionRepo.GetByOperarioOficinaAsync(op.Id);
                var carga = asignaciones.Count(a =>
                    a.EstadoTarea == EstadoTarea.Pendiente || a.EstadoTarea == EstadoTarea.EnProgreso);
                cargas.Add((op, carga));
            }

            var operarioAsignado = cargas.OrderBy(c => c.Carga).ThenBy(c => c.Operario.Id).First().Operario;

            await _asignacionRepo.CreateAsync(new AsignacionPaquete
            {
                NumeroExpedicion = numeroExpedicion,
                OperarioOficinaAsignadoId = operarioAsignado.Id,
                OficinaJsonId = oficinaJsonId,
                OficinaNombre = oficinaNombre,
                TipoTarea = tipoTarea,
                EsUrgente = esUrgente,
                Observaciones = observaciones ?? $"Tarea {tipoTarea} — auto-asignada por flujo de escaneo"
            });

            _logger.LogInformation(
                "Tarea {Tipo} auto-asignada a OperarioOficina {Operario} en oficina {Ofi} para {Expedicion}",
                tipoTarea, operarioAsignado.NombreCompleto, oficinaJsonId, numeroExpedicion);

            return (true, false, $"Tarea de {tipoTarea} asignada a {operarioAsignado.NombreCompleto}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error en auto-asignación de tarea {Tipo} para {Expedicion} en oficina {Ofi}",
                tipoTarea, numeroExpedicion, oficinaJsonId);
            return (false, false, $"Error al crear la asignación automática de {tipoTarea}.");
        }
    }

    /// <summary>
    /// Registra el paquete en la bandeja del JefeReparto del CTA destino llamando al
    /// microservicio Reparto. Solo se invoca cuando TipoEntrega = Domicilio.
    /// Es tolerante a fallos: si Reparto no responde o el lookup de Ciudadano falla,
    /// el escaneo ya quedó registrado como DisponibleParaReparto en Intranet y se
    /// intentará registrar el pendiente con los datos mínimos disponibles.
    /// </summary>
    private async Task TryRegistrarEnBandejaRepartoAsync(ScanRequestDto req, string expedicion)
    {
        try
        {
            if (!req.CtaId.HasValue)
            {
                _logger.LogWarning(
                    "No se puede registrar {Expedicion} en bandeja de Reparto: CtaId ausente.",
                    expedicion);
                return;
            }

            var envio = await _ciudadanoLookup.ObtenerAsync(expedicion);

            // Si el lookup falla, NO abortamos: solo el flujo Domicilio llega aquí
            // (los envíos Oficina se desvían antes hacia ListoParaRecogidaEnOficina),
            // así que registramos con los datos mínimos del scan request.
            if (envio is null)
            {
                _logger.LogWarning(
                    "Lookup de Ciudadano devolvió null para {Expedicion}; se registra en bandeja con datos mínimos del scan.",
                    expedicion);
            }
            else if (string.Equals(envio.TipoEntrega, "Oficina", StringComparison.OrdinalIgnoreCase))
            {
                // Defensa en profundidad: si por algún motivo llega un Oficina aquí, no entra en la bandeja del Jefe.
                _logger.LogInformation(
                    "{Expedicion} es entrega en oficina; no se registra en bandeja del JefeReparto.",
                    expedicion);
                return;
            }

            string nombreDestinatario = "Destinatario";
            string? telefono = null;
            string direccion = string.Empty;
            string codigoPostal = req.CodigoPostalDestino ?? string.Empty;
            string? ciudad = null;
            string numeroSeguimiento = expedicion;

            if (envio is not null)
            {
                var nombre = $"{envio.NombreDestinatario} {envio.ApellidosDestinatario}".Trim();
                if (!string.IsNullOrWhiteSpace(nombre)) nombreDestinatario = nombre;
                telefono = envio.TelefonoDestinatario;
                direccion = envio.Destino ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(envio.CodigoPostalDestino))
                    codigoPostal = envio.CodigoPostalDestino;
                ciudad = envio.Destino;
                if (!string.IsNullOrWhiteSpace(envio.NumeroSeguimiento))
                    numeroSeguimiento = envio.NumeroSeguimiento;
            }

            var dto = new RegistrarPaqueteBandejaIntranetDto
            {
                NumeroExpedicion = expedicion,
                NumeroSeguimiento = numeroSeguimiento,
                CtaId = req.CtaId.Value,
                CtaCodigo = req.CtaCodigo,
                NombreDestinatario = nombreDestinatario,
                TelefonoDestinatario = telefono,
                DireccionEntrega = direccion,
                CodigoPostalDestino = codigoPostal,
                CiudadDestino = ciudad,
                EsUrgente = req.EsUrgente,
                Observaciones = req.Observaciones
            };

            var resultado = await _repartoBandeja.RegistrarPaqueteAsync(dto);
            if (resultado.Success)
            {
                _logger.LogInformation(
                    "Paquete {Expedicion} {Estado} bandeja del JefeReparto (CTA {Cta}). Id={Id}",
                    expedicion,
                    resultado.Idempotente ? "ya estaba en" : "registrado en",
                    req.CtaCodigo,
                    resultado.Id);
            }
            else
            {
                _logger.LogWarning(
                    "No se pudo registrar {Expedicion} en bandeja de Reparto: {Msg}",
                    expedicion, resultado.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error inesperado registrando {Expedicion} en bandeja de Reparto.",
                expedicion);
        }
    }

}
