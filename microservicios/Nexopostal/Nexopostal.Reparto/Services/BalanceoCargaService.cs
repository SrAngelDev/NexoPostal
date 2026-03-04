using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;

namespace Nexopostal.Reparto.Services;

// ============================================================
//  DTOs de Balanceo de Carga
// ============================================================

public class EstadisticasBalanceo
{
    public DateOnly Fecha { get; set; }
    public int TotalRepartidores { get; set; }
    public int TotalEntregasPendientes { get; set; }
    public double MediaEntregasPorRepartidor { get; set; }
    public int MaxEntregasRepartidor { get; set; }
    public int MinEntregasRepartidor { get; set; }

    /// <summary>Índice de balanceo: 0 = totalmente desbalanceado, 1 = perfectamente balanceado</summary>
    public double IndiceBalanceo { get; set; }
}

// ============================================================
//  Interfaz del servicio de Balanceo de Carga
// ============================================================

public interface IBalanceoCargaService
{
    /// <summary>
    /// Distribuye las entregas pendientes entre los repartidores disponibles.
    /// Devuelve diccionario[repartidorId] = lista de entregaIds asignados.
    /// </summary>
    Task<Dictionary<int, List<int>>> BalancearCargaDiaria(DateOnly fecha, int? oficinaJsonId = null);

    /// <summary>
    /// Calcula cuántas entregas adicionales puede asumir un repartidor en la fecha indicada.
    /// </summary>
    Task<int> CalcularCapacidadDisponible(int repartidorId, DateOnly fecha);

    /// <summary>
    /// Obtiene las estadísticas de balanceo de carga para una fecha.
    /// </summary>
    Task<EstadisticasBalanceo> ObtenerEstadisticasBalanceo(DateOnly fecha, int? oficinaJsonId = null);
}

// ============================================================
//  Implementación del servicio de Balanceo de Carga
// ============================================================

public class BalanceoCargaService : IBalanceoCargaService
{
    private const int MaxEntregasPorRepartidorDia = 30;

    private readonly IRepartidorRepository _repartidorRepo;
    private readonly IRutaRepartoRepository _rutaRepo;
    private readonly IEntregaPaqueteRepository _entregaRepo;
    private readonly ILogger<BalanceoCargaService> _logger;

    public BalanceoCargaService(
        IRepartidorRepository repartidorRepo,
        IRutaRepartoRepository rutaRepo,
        IEntregaPaqueteRepository entregaRepo,
        ILogger<BalanceoCargaService> logger)
    {
        _repartidorRepo = repartidorRepo;
        _rutaRepo = rutaRepo;
        _entregaRepo = entregaRepo;
        _logger = logger;
    }

    // ═══════════════════════════════════════════
    //  BALANCEAR CARGA DIARIA
    // ═══════════════════════════════════════════

    public async Task<Dictionary<int, List<int>>> BalancearCargaDiaria(DateOnly fecha, int? oficinaJsonId = null)
    {
        _logger.LogInformation(
            "Iniciando balanceo de carga para fecha {Fecha}, oficina {Oficina}",
            fecha, oficinaJsonId?.ToString() ?? "todas");

        // 1. Obtener repartidores activos de la oficina
        var todosRepartidores = await _repartidorRepo.GetAllAsync(oficinaJsonId);
        var repartidoresActivos = todosRepartidores.Where(r => r.Activo).ToList();

        if (repartidoresActivos.Count == 0)
        {
            _logger.LogWarning("No hay repartidores activos para la oficina {Oficina}", oficinaJsonId);
            return new Dictionary<int, List<int>>();
        }

        // 2. Obtener rutas del día para calcular carga actual
        var rutasDelDia = await _rutaRepo.GetByFechaAsync(fecha, oficinaJsonId);

        // 3. Calcular carga actual por repartidor
        var cargaActual = new Dictionary<int, int>();
        foreach (var repartidor in repartidoresActivos)
        {
            cargaActual[repartidor.Id] = 0;
        }

        var rutaIds = rutasDelDia.Select(r => r.Id).ToList();
        var entregasExistentes = rutaIds.Count > 0
            ? await _entregaRepo.GetByRutaIdsAsync(rutaIds)
            : new List<EntregaPaquete>();

        foreach (var ruta in rutasDelDia)
        {
            if (cargaActual.ContainsKey(ruta.RepartidorId))
            {
                var entregasDeRuta = entregasExistentes.Count(e => e.RutaRepartoId == ruta.Id);
                cargaActual[ruta.RepartidorId] += entregasDeRuta;
            }
        }

        // 4. Encontrar entregas pendientes sin asignar
        //    (entregas en rutas planificadas con estado Pendiente que aún no tienen repartidor óptimo)
        var entregasPendientes = entregasExistentes
            .Where(e => e.Estado == EstadoEntrega.Pendiente)
            .ToList();

        // 5. Crear el resultado con entregas ya asignadas
        var asignaciones = new Dictionary<int, List<int>>();
        foreach (var repartidor in repartidoresActivos)
        {
            asignaciones[repartidor.Id] = new List<int>();
        }

        // Agrupar entregas pendientes por ruta para saber a qué repartidor pertenecen actualmente
        var rutaPorId = rutasDelDia.ToDictionary(r => r.Id, r => r.RepartidorId);

        // 6. Redistribuir entregas pendientes de forma balanceada
        var entregasAReasignar = new List<EntregaPaquete>();
        foreach (var entrega in entregasPendientes)
        {
            if (rutaPorId.TryGetValue(entrega.RutaRepartoId, out var repartidorIdActual) &&
                cargaActual.ContainsKey(repartidorIdActual))
            {
                // Si el repartidor actual tiene capacidad, mantener la asignación
                if (cargaActual[repartidorIdActual] <= MaxEntregasPorRepartidorDia)
                {
                    asignaciones[repartidorIdActual].Add(entrega.Id);
                }
                else
                {
                    entregasAReasignar.Add(entrega);
                }
            }
            else
            {
                entregasAReasignar.Add(entrega);
            }
        }

        // 7. Asignar las entregas sobrantes al repartidor con menos carga
        foreach (var entrega in entregasAReasignar)
        {
            var repartidorMenosCarga = repartidoresActivos
                .Where(r => (cargaActual.GetValueOrDefault(r.Id, 0) +
                             asignaciones[r.Id].Count) < MaxEntregasPorRepartidorDia)
                .OrderBy(r => cargaActual.GetValueOrDefault(r.Id, 0) + asignaciones[r.Id].Count)
                .FirstOrDefault();

            if (repartidorMenosCarga != null)
            {
                asignaciones[repartidorMenosCarga.Id].Add(entrega.Id);
            }
            else
            {
                _logger.LogWarning(
                    "No hay repartidores con capacidad disponible para la entrega {EntregaId}",
                    entrega.Id);
            }
        }

        var totalAsignadas = asignaciones.Values.Sum(v => v.Count);
        _logger.LogInformation(
            "Balanceo completado: {TotalAsignadas} entregas distribuidas entre {Repartidores} repartidores",
            totalAsignadas, repartidoresActivos.Count);

        return asignaciones;
    }

    // ═══════════════════════════════════════════
    //  CALCULAR CAPACIDAD DISPONIBLE
    // ═══════════════════════════════════════════

    public async Task<int> CalcularCapacidadDisponible(int repartidorId, DateOnly fecha)
    {
        var repartidor = await _repartidorRepo.GetByIdAsync(repartidorId);
        if (repartidor == null || !repartidor.Activo)
        {
            _logger.LogWarning("Repartidor {RepartidorId} no encontrado o inactivo", repartidorId);
            return 0;
        }

        var rutasDelDia = await _rutaRepo.GetAllAsync(fecha, repartidorId);
        if (rutasDelDia.Count == 0)
            return MaxEntregasPorRepartidorDia;

        var rutaIds = rutasDelDia.Select(r => r.Id).ToList();
        var entregas = await _entregaRepo.GetByRutaIdsAsync(rutaIds);
        var entregasActivas = entregas.Count(e =>
            e.Estado == EstadoEntrega.Pendiente || e.Estado == EstadoEntrega.EnCamino);

        var capacidad = MaxEntregasPorRepartidorDia - entregasActivas;

        _logger.LogInformation(
            "Repartidor {RepartidorId}: {Activas} entregas activas, capacidad disponible: {Capacidad}",
            repartidorId, entregasActivas, Math.Max(0, capacidad));

        return Math.Max(0, capacidad);
    }

    // ═══════════════════════════════════════════
    //  ESTADÍSTICAS DE BALANCEO
    // ═══════════════════════════════════════════

    public async Task<EstadisticasBalanceo> ObtenerEstadisticasBalanceo(DateOnly fecha, int? oficinaJsonId = null)
    {
        var todosRepartidores = await _repartidorRepo.GetAllAsync(oficinaJsonId);
        var repartidoresActivos = todosRepartidores.Where(r => r.Activo).ToList();

        var rutasDelDia = await _rutaRepo.GetByFechaAsync(fecha, oficinaJsonId);
        var rutaIds = rutasDelDia.Select(r => r.Id).ToList();
        var entregas = rutaIds.Count > 0
            ? await _entregaRepo.GetByRutaIdsAsync(rutaIds)
            : new List<EntregaPaquete>();

        var entregasPendientes = entregas
            .Where(e => e.Estado == EstadoEntrega.Pendiente || e.Estado == EstadoEntrega.EnCamino)
            .ToList();

        // Calcular entregas por repartidor
        var entregasPorRepartidor = new Dictionary<int, int>();
        foreach (var repartidor in repartidoresActivos)
        {
            entregasPorRepartidor[repartidor.Id] = 0;
        }

        var rutaPorId = rutasDelDia.ToDictionary(r => r.Id, r => r.RepartidorId);
        foreach (var entrega in entregasPendientes)
        {
            if (rutaPorId.TryGetValue(entrega.RutaRepartoId, out var repartidorId) &&
                entregasPorRepartidor.ContainsKey(repartidorId))
            {
                entregasPorRepartidor[repartidorId]++;
            }
        }

        var cargas = entregasPorRepartidor.Values.ToList();

        double media = cargas.Count > 0 ? cargas.Average() : 0;
        int maxCarga = cargas.Count > 0 ? cargas.Max() : 0;
        int minCarga = cargas.Count > 0 ? cargas.Min() : 0;

        // Índice de balanceo: 1 - (desviación estándar / media)
        // Si la media es 0, el balanceo es perfecto (no hay entregas)
        double indiceBalanceo = 1.0;
        if (media > 0 && cargas.Count > 1)
        {
            double varianza = cargas.Sum(c => Math.Pow(c - media, 2)) / cargas.Count;
            double desviacion = Math.Sqrt(varianza);
            indiceBalanceo = Math.Max(0, Math.Round(1.0 - (desviacion / media), 4));
        }

        var estadisticas = new EstadisticasBalanceo
        {
            Fecha = fecha,
            TotalRepartidores = repartidoresActivos.Count,
            TotalEntregasPendientes = entregasPendientes.Count,
            MediaEntregasPorRepartidor = Math.Round(media, 2),
            MaxEntregasRepartidor = maxCarga,
            MinEntregasRepartidor = minCarga,
            IndiceBalanceo = indiceBalanceo
        };

        _logger.LogInformation(
            "Estadísticas de balanceo ({Fecha}): {Total} entregas, {Repartidores} repartidores, " +
            "media {Media}, índice {Indice}",
            fecha, entregasPendientes.Count, repartidoresActivos.Count,
            estadisticas.MediaEntregasPorRepartidor, estadisticas.IndiceBalanceo);

        return estadisticas;
    }
}
