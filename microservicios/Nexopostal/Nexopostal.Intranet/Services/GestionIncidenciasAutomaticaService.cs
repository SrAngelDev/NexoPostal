using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio de gestión automática de incidencias.
/// Detecta paquetes sin movimiento, crea incidencias de forma automática,
/// escala incidencias no resueltas y propone resoluciones.
/// </summary>
public interface IGestionIncidenciasAutomaticaService
{
    /// <summary>
    /// Detecta paquetes que no han tenido movimiento durante el número de horas indicado.
    /// </summary>
    Task<List<string>> DetectarPaquetesSinMovimiento(int horasLimite = 48);

    /// <summary>
    /// Escanea el sistema en busca de problemas y crea incidencias automáticamente.
    /// Retorna el número de incidencias creadas.
    /// </summary>
    Task<int> CrearIncidenciasAutomaticas();

    /// <summary>
    /// Escala una incidencia aumentando su prioridad/severidad.
    /// </summary>
    Task<bool> EscalarIncidencia(int incidenciaId);

    /// <summary>
    /// Propone una resolución automática basándose en el tipo de incidencia.
    /// </summary>
    Task<string> ProponerResolucion(int incidenciaId);
}

public class GestionIncidenciasAutomaticaService : IGestionIncidenciasAutomaticaService
{
    private readonly IIncidenciaRepository _incidenciaRepo;
    private readonly IMovimientoPaqueteRepository _movimientoRepo;
    private readonly IHistorialEstadoRepository _historialRepo;
    private readonly ILogger<GestionIncidenciasAutomaticaService> _logger;

    public GestionIncidenciasAutomaticaService(
        IIncidenciaRepository incidenciaRepo,
        IMovimientoPaqueteRepository movimientoRepo,
        IHistorialEstadoRepository historialRepo,
        ILogger<GestionIncidenciasAutomaticaService> logger)
    {
        _incidenciaRepo = incidenciaRepo;
        _movimientoRepo = movimientoRepo;
        _historialRepo = historialRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<string>> DetectarPaquetesSinMovimiento(int horasLimite = 48)
    {
        var umbral = DateTime.UtcNow.AddHours(-horasLimite);

        // Buscar movimientos en tránsito cuya fecha de creación sea anterior al umbral
        var movimientosEstancados = await _movimientoRepo.GetEnTransitoAnterioresAAsync(umbral);

        var expediciones = movimientosEstancados
            .Select(m => m.NumeroExpedicion)
            .Distinct()
            .ToList();

        _logger.LogInformation(
            "🔍 Detección automática → {Count} paquete(s) sin movimiento en las últimas {Horas}h",
            expediciones.Count, horasLimite);

        return expediciones;
    }

    /// <inheritdoc />
    public async Task<int> CrearIncidenciasAutomaticas()
    {
        var incidenciasCreadas = 0;

        // 1. Detectar paquetes sin movimiento (más de 48 horas)
        var paquetesEstancados = await DetectarPaquetesSinMovimiento(48);

        foreach (var expedicion in paquetesEstancados)
        {
            // Verificar si ya existe una incidencia abierta para esta expedición
            var incidenciasExistentes = await _incidenciaRepo.GetByExpedicionAsync(expedicion);
            var tieneIncidenciaAbierta = incidenciasExistentes
                .Any(i => i.Estado == EstadoIncidencia.Abierta || i.Estado == EstadoIncidencia.EnRevision);

            if (tieneIncidenciaAbierta)
                continue;

            // Obtener el movimiento para saber en qué CTA se encuentra
            var movimientos = await _movimientoRepo.GetByExpedicionAsync(expedicion);
            var movimientoActual = movimientos
                .OrderByDescending(m => m.FechaCreacion)
                .FirstOrDefault();

            if (movimientoActual == null)
                continue;

            var incidencia = new Incidencia
            {
                NumeroExpedicion = expedicion,
                CtaId = movimientoActual.CtaOrigenId,
                ReportadaPorId = 0, // Sistema automático: se usa 0 como marcador
                Tipo = TipoIncidencia.PaqueteExtraviado,
                Estado = EstadoIncidencia.Abierta,
                Descripcion = $"[AUTOMÁTICA] Paquete sin movimiento durante más de 48 horas. " +
                              $"Último movimiento registrado el {movimientoActual.FechaCreacion:dd/MM/yyyy HH:mm} UTC. " +
                              $"Estado actual del movimiento: {movimientoActual.Estado}."
            };

            await _incidenciaRepo.CreateAsync(incidencia);
            incidenciasCreadas++;

            _logger.LogWarning(
                "⚠️ Incidencia automática creada → {Expedicion} sin movimiento en CTA {CtaId}",
                expedicion, movimientoActual.CtaOrigenId);
        }

        // 2. Detectar movimientos en tránsito demasiado largos (más de 72 horas)
        var umbral72h = DateTime.UtcNow.AddHours(-72);
        var transitoLargo = await _movimientoRepo.GetEnTransitoAnterioresAAsync(umbral72h);

        foreach (var movimiento in transitoLargo)
        {
            var incidenciasExistentes = await _incidenciaRepo.GetByExpedicionAsync(movimiento.NumeroExpedicion);
            var tieneIncidenciaAbierta = incidenciasExistentes
                .Any(i => i.Estado == EstadoIncidencia.Abierta || i.Estado == EstadoIncidencia.EnRevision);

            if (tieneIncidenciaAbierta)
                continue;

            var incidencia = new Incidencia
            {
                NumeroExpedicion = movimiento.NumeroExpedicion,
                CtaId = movimiento.CtaDestinoId,
                ReportadaPorId = 0,
                Tipo = TipoIncidencia.PaqueteExtraviado,
                Estado = EstadoIncidencia.Abierta,
                Descripcion = $"[AUTOMÁTICA] Paquete en tránsito durante más de 72 horas. " +
                              $"Salió del CTA origen (Id: {movimiento.CtaOrigenId}) el " +
                              $"{movimiento.FechaSalida?.ToString("dd/MM/yyyy HH:mm") ?? "fecha desconocida"} UTC. " +
                              $"Tipo transporte: {movimiento.TipoTransporte}."
            };

            await _incidenciaRepo.CreateAsync(incidencia);
            incidenciasCreadas++;

            _logger.LogWarning(
                "⚠️ Incidencia automática (tránsito largo) → {Expedicion} en ruta >72h",
                movimiento.NumeroExpedicion);
        }

        _logger.LogInformation(
            "🔧 Creación automática de incidencias completada → {Total} incidencia(s) creada(s)",
            incidenciasCreadas);

        return incidenciasCreadas;
    }

    /// <inheritdoc />
    public async Task<bool> EscalarIncidencia(int incidenciaId)
    {
        var incidencia = await _incidenciaRepo.GetByIdAsync(incidenciaId);
        if (incidencia == null)
        {
            _logger.LogWarning("No se encontró la incidencia {Id} para escalar", incidenciaId);
            return false;
        }

        // Solo se pueden escalar incidencias abiertas → pasan a EnRevision
        if (incidencia.Estado == EstadoIncidencia.Abierta)
        {
            incidencia.Estado = EstadoIncidencia.EnRevision;
            incidencia.Descripcion += $"\n[ESCALADA {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC] " +
                                      "Incidencia escalada automáticamente por falta de resolución.";

            await _incidenciaRepo.UpdateAsync(incidencia);

            _logger.LogWarning(
                "🔺 Incidencia {Id} escalada → Abierta → EnRevision · Expedición: {Expedicion}",
                incidenciaId, incidencia.NumeroExpedicion);

            return true;
        }

        // Si ya está en revisión, añadir nota de escalado adicional
        if (incidencia.Estado == EstadoIncidencia.EnRevision)
        {
            incidencia.Descripcion += $"\n[ESCALADA ADICIONAL {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC] " +
                                      "Requiere atención urgente. Incidencia sin resolver durante tiempo prolongado.";

            await _incidenciaRepo.UpdateAsync(incidencia);

            _logger.LogWarning(
                "🔺 Incidencia {Id} escalada adicionalmente (ya en EnRevision) · Expedición: {Expedicion}",
                incidenciaId, incidencia.NumeroExpedicion);

            return true;
        }

        _logger.LogInformation(
            "Incidencia {Id} no escalable: estado actual {Estado}",
            incidenciaId, incidencia.Estado);

        return false;
    }

    /// <inheritdoc />
    public async Task<string> ProponerResolucion(int incidenciaId)
    {
        var incidencia = await _incidenciaRepo.GetByIdAsync(incidenciaId);
        if (incidencia == null)
            return "No se encontró la incidencia especificada.";

        var propuesta = incidencia.Tipo switch
        {
            TipoIncidencia.PaqueteDanado =>
                "1. Documentar fotográficamente los daños del paquete.\n" +
                "2. Verificar el seguro del envío y notificar al remitente.\n" +
                "3. Si el contenido está intacto, reembalar y continuar el envío.\n" +
                "4. Si el contenido está dañado, iniciar proceso de reclamación e indemnización.",

            TipoIncidencia.PaqueteExtraviado =>
                "1. Verificar el último escaneo registrado en el historial de trazabilidad.\n" +
                "2. Contactar con el CTA donde se registró el último movimiento.\n" +
                "3. Revisar las cintas de clasificación y las zonas de almacenamiento temporal.\n" +
                "4. Si no se localiza en 24h, escalar a jefatura y notificar al cliente.",

            TipoIncidencia.DireccionIncorrecta =>
                "1. Contactar con el remitente para verificar la dirección correcta.\n" +
                "2. Si se obtiene la dirección correcta, reclasificar el paquete y actualizar la etiqueta.\n" +
                "3. Si no se puede contactar, retener el paquete 7 días antes de devolver a origen.",

            TipoIncidencia.PaqueteRetenido =>
                "1. Verificar el motivo de la retención (aduanas, contenido prohibido, orden judicial).\n" +
                "2. Documentar la retención y notificar al remitente y destinatario.\n" +
                "3. Coordinar con las autoridades competentes si es necesario.\n" +
                "4. Una vez liberado, reintroducir el paquete en el flujo logístico.",

            TipoIncidencia.ErrorClasificacion =>
                "1. Identificar el CTA correcto de destino consultando la tabla de rutas por CP.\n" +
                "2. Reclasificar el paquete y crear un nuevo movimiento al CTA correcto.\n" +
                "3. Registrar el error para análisis de calidad y calibración del sistema de lectura.\n" +
                "4. Verificar el estado de los arcos de lectura de código de barras.",

            TipoIncidencia.Otra =>
                "1. Revisar la descripción detallada de la incidencia.\n" +
                "2. Evaluar el impacto en la operativa del CTA.\n" +
                "3. Aplicar medidas correctivas según el caso concreto.\n" +
                "4. Documentar la resolución para futuras referencias.",

            _ =>
                "Tipo de incidencia no reconocido. Revisar manualmente y aplicar el protocolo correspondiente."
        };

        _logger.LogInformation(
            "💡 Resolución propuesta para incidencia {Id} (tipo: {Tipo})",
            incidenciaId, incidencia.Tipo);

        return propuesta;
    }
}
