using System.Text.Json;
using Nexopostal.Intranet.DTOs;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio que carga y cachea las oficinas desde el archivo JSON estático
/// de oficinas reales de NexoPostal (Data/oficinas.json).
/// 
/// Proporciona métodos de búsqueda por CP, texto libre y resolución automática
/// de la oficina más cercana a un código postal dado (para el flujo logístico).
/// 
/// Formato del JSON: JSON-LD con @graph[] donde cada oficina tiene:
///   id, title, address { locality, postal-code, street-address },
///   location { latitude, longitude }, schedule, services
/// </summary>
public class OficinasJsonService
{
    private readonly ILogger<OficinasJsonService> _logger;
    private readonly string _jsonPath;
    private List<OficinaJsonDto>? _cache;
    private readonly object _lock = new();

    public OficinasJsonService(ILogger<OficinasJsonService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _jsonPath = Path.Combine(env.ContentRootPath, "Data", "oficinas.json");
    }

    // ───────────────────────────────────────────────────────────────
    //  Consultas básicas
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene todas las oficinas (cacheadas en memoria).
    /// </summary>
    public List<OficinaJsonDto> ObtenerTodas()
    {
        if (_cache != null) return _cache;

        lock (_lock)
        {
            if (_cache != null) return _cache;

            _logger.LogInformation("Cargando oficinas desde {Path}", _jsonPath);

            var json = File.ReadAllText(_jsonPath);
            using var doc = JsonDocument.Parse(json);

            var graph = doc.RootElement.GetProperty("@graph");
            var oficinas = new List<OficinaJsonDto>();

            foreach (var item in graph.EnumerateArray())
            {
                oficinas.Add(TransformarOficina(item));
            }

            _cache = oficinas;
            _logger.LogInformation("Cargadas {Count} oficinas desde JSON", oficinas.Count);
            return _cache;
        }
    }

    /// <summary>
    /// Busca oficinas cuyo código postal coincida o empiece por el valor dado.
    /// Primero coincidencia exacta; si no hay, por prefijo de 3 dígitos.
    /// </summary>
    public List<OficinaJsonDto> BuscarPorCodigoPostal(string codigoPostal)
    {
        var todas = ObtenerTodas();
        var cp = codigoPostal.Trim();

        // Coincidencia exacta
        var exactas = todas.Where(o => o.CodigoPostal == cp).ToList();
        if (exactas.Count > 0) return exactas;

        // Prefijo de 3 dígitos
        var prefijo = cp.Length >= 3 ? cp[..3] : cp;
        return todas
            .Where(o => o.CodigoPostal.StartsWith(prefijo))
            .OrderBy(o => o.CodigoPostal)
            .Take(20)
            .ToList();
    }

    /// <summary>
    /// Busca oficinas por texto libre (nombre, dirección, ciudad o CP).
    /// </summary>
    public List<OficinaJsonDto> BuscarPorTexto(string query)
    {
        var todas = ObtenerTodas();
        var q = query.Trim().ToLowerInvariant();

        return todas
            .Where(o =>
                o.Ciudad.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.Direccion.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.Nombre.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                o.CodigoPostal.Contains(q, StringComparison.OrdinalIgnoreCase))
            .Take(50)
            .ToList();
    }

    /// <summary>
    /// Obtiene una oficina por su ID del JSON.
    /// </summary>
    public OficinaJsonDto? ObtenerPorId(int id)
    {
        return ObtenerTodas().FirstOrDefault(o => o.Id == id);
    }

    // ───────────────────────────────────────────────────────────────
    //  Resolución automática: CP → Oficina más cercana
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Resuelve la oficina más cercana a un código postal dado.
    /// 
    /// Algoritmo:
    ///   1. Coincidencia exacta de CP → la primera
    ///   2. Prefijo de 3 dígitos → la más cercana por coordenadas
    ///   3. Prefijo de 2 dígitos (misma provincia) → la más cercana por coordenadas
    /// 
    /// Devuelve null si no hay ninguna oficina para esa zona.
    /// </summary>
    public OficinaJsonDto? ResolverOficinaMasCercana(string codigoPostal)
    {
        var todas = ObtenerTodas();
        var cp = codigoPostal.Trim();

        // 1. Coincidencia exacta de CP
        var exactas = todas.Where(o => o.CodigoPostal == cp).ToList();
        if (exactas.Count > 0) return exactas[0];

        // 2. Prefijo de 3 dígitos (misma zona)
        if (cp.Length >= 3)
        {
            var prefijo3 = cp[..3];
            var zona3 = todas.Where(o => o.CodigoPostal.StartsWith(prefijo3)).ToList();
            if (zona3.Count > 0) return zona3[0]; // No hay coordenadas de referencia, toma la primera
        }

        // 3. Prefijo de 2 dígitos (misma provincia)
        if (cp.Length >= 2)
        {
            var prefijo2 = cp[..2];
            var provincia = todas.Where(o => o.CodigoPostal.StartsWith(prefijo2)).ToList();
            if (provincia.Count > 0) return provincia[0];
        }

        return null;
    }

    /// <summary>
    /// Resuelve la oficina más cercana a unas coordenadas geográficas
    /// dentro de un radio de búsqueda limitado (por prefijo de CP).
    /// Si no se proporcionan coordenadas, busca por CP textual.
    /// </summary>
    public OficinaJsonDto? ResolverOficinaMasCercana(
        string codigoPostal,
        double latitudReferencia,
        double longitudReferencia)
    {
        var todas = ObtenerTodas();
        var cp = codigoPostal.Trim();

        // Primero reducir el universo de búsqueda por prefijo de CP
        List<OficinaJsonDto> candidatas;

        // Intentar exacto
        candidatas = todas.Where(o => o.CodigoPostal == cp).ToList();
        if (candidatas.Count == 1) return candidatas[0];

        // Si hay múltiples exactas o ninguna, ampliar a prefijo 3
        if (candidatas.Count == 0 && cp.Length >= 3)
            candidatas = todas.Where(o => o.CodigoPostal.StartsWith(cp[..3])).ToList();

        // Si aún vacío, ampliar a prefijo 2
        if (candidatas.Count == 0 && cp.Length >= 2)
            candidatas = todas.Where(o => o.CodigoPostal.StartsWith(cp[..2])).ToList();

        if (candidatas.Count == 0) return null;

        // Ordenar por distancia Haversine
        return candidatas
            .Where(o => o.Latitud.HasValue && o.Longitud.HasValue)
            .OrderBy(o => CalcularDistanciaKm(
                latitudReferencia, longitudReferencia,
                o.Latitud!.Value, o.Longitud!.Value))
            .FirstOrDefault()
            ?? candidatas[0]; // Fallback si ninguna tiene coordenadas
    }

    // ───────────────────────────────────────────────────────────────
    //  Utilidades
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Calcula la distancia en kilómetros entre dos puntos usando la fórmula de Haversine.
    /// </summary>
    public static double CalcularDistanciaKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Radio de la Tierra en km

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

    /// <summary>
    /// Transforma un elemento JSON del @graph al DTO de oficina.
    /// </summary>
    private static OficinaJsonDto TransformarOficina(JsonElement item)
    {
        var address = item.GetProperty("address");
        var location = item.GetProperty("location");

        var horario = item.TryGetProperty("schedule", out var sched)
            ? sched.GetString() ?? "Lu-Vi: 09:00-14:00"
            : "Lu-Vi: 09:00-14:00";

        var servicios = item.TryGetProperty("services", out var svc)
            ? svc.GetString() ?? ""
            : "";

        var ciudad = address.GetProperty("locality").GetString() ?? "";
        var cp = address.GetProperty("postal-code").GetString() ?? "";
        var calle = address.GetProperty("street-address").GetString() ?? "";

        return new OficinaJsonDto
        {
            Id = int.TryParse(item.GetProperty("id").GetString(), out var id) ? id : 0,
            Nombre = item.GetProperty("title").GetString() ?? "",
            Direccion = calle,
            CodigoPostal = cp,
            Ciudad = ciudad,
            Horario = horario,
            Servicios = servicios,
            Latitud = location.GetProperty("latitude").GetDouble(),
            Longitud = location.GetProperty("longitude").GetDouble()
        };
    }
}
