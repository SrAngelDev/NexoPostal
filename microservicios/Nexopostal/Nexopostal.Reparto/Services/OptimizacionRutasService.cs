using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;

namespace Nexopostal.Reparto.Services;

// ============================================================
//  DTOs de Optimización de Rutas
// ============================================================

public class EntregaParaOptimizar
{
    public int EntregaId { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string DireccionEntrega { get; set; } = string.Empty;
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public bool EsUrgente { get; set; }
    public int OrdenActual { get; set; }
}

public class RutaOptimizada
{
    public int RepartidorId { get; set; }
    public List<EntregaParaOptimizar> EntregasOrdenadas { get; set; } = new();
    public double DistanciaTotalKm { get; set; }
    public int TiempoEstimadoMinutos { get; set; }
    public string Algoritmo { get; set; } = "NearestNeighbor";
}

// ============================================================
//  Interfaz del servicio de Optimización de Rutas
// ============================================================

public interface IOptimizacionRutasService
{
    /// <summary>
    /// Genera la ruta óptima para un repartidor usando el algoritmo del vecino más cercano.
    /// Las entregas urgentes se priorizan antes de aplicar la optimización.
    /// </summary>
    Task<RutaOptimizada> GenerarRutaOptima(int repartidorId, List<EntregaParaOptimizar> entregas);

    /// <summary>
    /// Reordena las entregas a partir de la ubicación actual del repartidor.
    /// </summary>
    Task<List<EntregaParaOptimizar>> ReordenarEntregas(List<EntregaParaOptimizar> entregas, double latActual, double lngActual);

    /// <summary>
    /// Calcula la distancia total en km de una secuencia de entregas usando la fórmula de Haversine.
    /// </summary>
    Task<double> CalcularDistanciaTotal(List<EntregaParaOptimizar> entregas);
}

// ============================================================
//  Implementación del servicio de Optimización de Rutas
// ============================================================

public class OptimizacionRutasService : IOptimizacionRutasService
{
    private const double RadioTierraKm = 6371.0;
    private const double VelocidadMediaKmH = 30.0;
    private const double TiempoParadaMinutos = 5.0;

    private readonly IRepartidorRepository _repartidorRepo;
    private readonly ILogger<OptimizacionRutasService> _logger;

    public OptimizacionRutasService(
        IRepartidorRepository repartidorRepo,
        ILogger<OptimizacionRutasService> logger)
    {
        _repartidorRepo = repartidorRepo;
        _logger = logger;
    }

    // ═══════════════════════════════════════════
    //  GENERAR RUTA ÓPTIMA
    // ═══════════════════════════════════════════

    public async Task<RutaOptimizada> GenerarRutaOptima(int repartidorId, List<EntregaParaOptimizar> entregas)
    {
        _logger.LogInformation("Generando ruta óptima para repartidor {RepartidorId} con {Count} entregas",
            repartidorId, entregas.Count);

        if (entregas.Count == 0)
        {
            return new RutaOptimizada
            {
                RepartidorId = repartidorId,
                EntregasOrdenadas = new List<EntregaParaOptimizar>(),
                DistanciaTotalKm = 0,
                TiempoEstimadoMinutos = 0
            };
        }

        // Separar urgentes y normales
        var urgentes = entregas.Where(e => e.EsUrgente).ToList();
        var normales = entregas.Where(e => !e.EsUrgente).ToList();

        // Obtener la ubicación de partida (primera urgente o primera normal)
        var primerPunto = urgentes.FirstOrDefault() ?? normales.First();
        double latInicio = primerPunto.Latitud;
        double lngInicio = primerPunto.Longitud;

        // Optimizar urgentes primero con nearest-neighbor
        var urgentesOptimizadas = AplicarNearestNeighbor(urgentes, latInicio, lngInicio);

        // Si hay urgentes, la última urgente es el punto de partida para las normales
        if (urgentesOptimizadas.Count > 0)
        {
            var ultimaUrgente = urgentesOptimizadas.Last();
            latInicio = ultimaUrgente.Latitud;
            lngInicio = ultimaUrgente.Longitud;
        }

        // Optimizar normales con nearest-neighbor desde la última urgente
        var normalesOptimizadas = AplicarNearestNeighbor(normales, latInicio, lngInicio);

        // Combinar: urgentes primero, luego normales
        var entregasOrdenadas = urgentesOptimizadas.Concat(normalesOptimizadas).ToList();

        // Asignar orden secuencial
        for (int i = 0; i < entregasOrdenadas.Count; i++)
        {
            entregasOrdenadas[i].OrdenActual = i + 1;
        }

        var distanciaTotal = await CalcularDistanciaTotal(entregasOrdenadas);
        var tiempoEstimado = (int)Math.Ceiling(
            (distanciaTotal / VelocidadMediaKmH) * 60 + entregasOrdenadas.Count * TiempoParadaMinutos);

        var resultado = new RutaOptimizada
        {
            RepartidorId = repartidorId,
            EntregasOrdenadas = entregasOrdenadas,
            DistanciaTotalKm = Math.Round(distanciaTotal, 2),
            TiempoEstimadoMinutos = tiempoEstimado
        };

        _logger.LogInformation(
            "Ruta optimizada: {Distancia} km, {Tiempo} min estimados, {Count} entregas ({Urgentes} urgentes)",
            resultado.DistanciaTotalKm, resultado.TiempoEstimadoMinutos,
            entregasOrdenadas.Count, urgentes.Count);

        return resultado;
    }

    // ═══════════════════════════════════════════
    //  REORDENAR DESDE UBICACIÓN ACTUAL
    // ═══════════════════════════════════════════

    public Task<List<EntregaParaOptimizar>> ReordenarEntregas(
        List<EntregaParaOptimizar> entregas, double latActual, double lngActual)
    {
        _logger.LogInformation(
            "Reordenando {Count} entregas desde coordenadas ({Lat}, {Lng})",
            entregas.Count, latActual, lngActual);

        if (entregas.Count <= 1)
            return Task.FromResult(entregas);

        // Separar urgentes y normales
        var urgentes = entregas.Where(e => e.EsUrgente).ToList();
        var normales = entregas.Where(e => !e.EsUrgente).ToList();

        var urgentesOptimizadas = AplicarNearestNeighbor(urgentes, latActual, lngActual);

        double latPartida = latActual;
        double lngPartida = lngActual;
        if (urgentesOptimizadas.Count > 0)
        {
            latPartida = urgentesOptimizadas.Last().Latitud;
            lngPartida = urgentesOptimizadas.Last().Longitud;
        }

        var normalesOptimizadas = AplicarNearestNeighbor(normales, latPartida, lngPartida);

        var resultado = urgentesOptimizadas.Concat(normalesOptimizadas).ToList();
        for (int i = 0; i < resultado.Count; i++)
        {
            resultado[i].OrdenActual = i + 1;
        }

        return Task.FromResult(resultado);
    }

    // ═══════════════════════════════════════════
    //  CALCULAR DISTANCIA TOTAL
    // ═══════════════════════════════════════════

    public Task<double> CalcularDistanciaTotal(List<EntregaParaOptimizar> entregas)
    {
        if (entregas.Count < 2)
            return Task.FromResult(0.0);

        double distanciaTotal = 0;
        for (int i = 0; i < entregas.Count - 1; i++)
        {
            distanciaTotal += CalcularDistanciaHaversine(
                entregas[i].Latitud, entregas[i].Longitud,
                entregas[i + 1].Latitud, entregas[i + 1].Longitud);
        }

        return Task.FromResult(Math.Round(distanciaTotal, 4));
    }

    // ═══════════════════════════════════════════
    //  MÉTODOS PRIVADOS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Algoritmo del vecino más cercano (Nearest Neighbor) para TSP.
    /// Enfoque greedy: en cada paso se elige la entrega más cercana a la posición actual.
    /// </summary>
    private List<EntregaParaOptimizar> AplicarNearestNeighbor(
        List<EntregaParaOptimizar> entregas, double latInicio, double lngInicio)
    {
        if (entregas.Count == 0)
            return new List<EntregaParaOptimizar>();

        var pendientes = new List<EntregaParaOptimizar>(entregas);
        var ordenadas = new List<EntregaParaOptimizar>();

        double latActual = latInicio;
        double lngActual = lngInicio;

        while (pendientes.Count > 0)
        {
            EntregaParaOptimizar? masCercana = null;
            double distanciaMinima = double.MaxValue;

            foreach (var entrega in pendientes)
            {
                var distancia = CalcularDistanciaHaversine(
                    latActual, lngActual, entrega.Latitud, entrega.Longitud);

                if (distancia < distanciaMinima)
                {
                    distanciaMinima = distancia;
                    masCercana = entrega;
                }
            }

            if (masCercana != null)
            {
                ordenadas.Add(masCercana);
                latActual = masCercana.Latitud;
                lngActual = masCercana.Longitud;
                pendientes.Remove(masCercana);
            }
        }

        return ordenadas;
    }

    /// <summary>
    /// Calcula la distancia en km entre dos puntos geográficos
    /// usando la fórmula de Haversine.
    /// </summary>
    private static double CalcularDistanciaHaversine(
        double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = GradosARadianes(lat2 - lat1);
        double dLon = GradosARadianes(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(GradosARadianes(lat1)) * Math.Cos(GradosARadianes(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return RadioTierraKm * c;
    }

    private static double GradosARadianes(double grados) => grados * (Math.PI / 180.0);
}
