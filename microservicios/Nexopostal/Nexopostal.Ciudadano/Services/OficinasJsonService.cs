using System.Text.Json;
using Nexopostal.Ciudadano.DTOs;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio que carga y cachea las oficinas desde el archivo JSON estático.
/// Proporciona métodos de búsqueda por código postal, dirección y listado completo.
/// </summary>
public class OficinasJsonService
{
    private readonly ILogger<OficinasJsonService> _logger;
    private readonly string _jsonPath;
    private List<OficinaDto>? _cache;
    private readonly object _lock = new();

    public OficinasJsonService(ILogger<OficinasJsonService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _jsonPath = Path.Combine(env.ContentRootPath, "Data", "oficinas.json");
    }

    /// <summary>
    /// Obtiene todas las oficinas (cacheadas en memoria).
    /// </summary>
    public List<OficinaDto> ObtenerTodas()
    {
        if (_cache != null) return _cache;

        lock (_lock)
        {
            if (_cache != null) return _cache;

            _logger.LogInformation("Cargando oficinas desde {Path}", _jsonPath);

            var json = File.ReadAllText(_jsonPath);
            using var doc = JsonDocument.Parse(json);

            var graph = doc.RootElement.GetProperty("@graph");
            var oficinas = new List<OficinaDto>();

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
    /// </summary>
    public List<OficinaDto> BuscarPorCodigoPostal(string codigoPostal)
    {
        var todas = ObtenerTodas();
        var cp = codigoPostal.Trim();

        // Primero coincidencia exacta
        var exactas = todas.Where(o => o.CodigoPostal == cp).ToList();
        if (exactas.Count > 0) return exactas;

        // Si no hay exactas, buscar por prefijo (primeros 3 dígitos)
        var prefijo = cp.Length >= 3 ? cp[..3] : cp;
        return todas
            .Where(o => o.CodigoPostal.StartsWith(prefijo))
            .OrderBy(o => o.CodigoPostal)
            .Take(20)
            .ToList();
    }

    /// <summary>
    /// Busca oficinas por texto libre (nombre, dirección, ciudad o código postal).
    /// </summary>
    public List<OficinaDto> BuscarPorTexto(string query)
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
    /// Transforma un elemento JSON del @graph al DTO de oficina.
    /// </summary>
    private static OficinaDto TransformarOficina(JsonElement item)
    {
        var address = item.GetProperty("address");
        var location = item.GetProperty("location");

        // schedule y services están al nivel raíz del objeto, NO dentro de organization
        var horario = item.TryGetProperty("schedule", out var sched)
            ? sched.GetString() ?? "Lu-Vi: 09:00-14:00"
            : "Lu-Vi: 09:00-14:00";

        var servicios = item.TryGetProperty("services", out var svc)
            ? svc.GetString() ?? ""
            : "";

        var ciudad = address.GetProperty("locality").GetString() ?? "";
        var cp = address.GetProperty("postal-code").GetString() ?? "";
        var calle = address.GetProperty("street-address").GetString() ?? "";

        return new OficinaDto
        {
            Id = int.TryParse(item.GetProperty("id").GetString(), out var id) ? id : 0,
            Nombre = item.GetProperty("title").GetString() ?? "",
            Direccion = calle,
            CodigoPostal = cp,
            Ciudad = ciudad,
            Provincia = ciudad, // El JSON no tiene provincia separada, usamos la ciudad
            Telefono = "912 197 197",
            Horario = horario,
            Servicios = servicios,
            Activa = true,
            Latitud = location.GetProperty("latitude").GetDouble(),
            Longitud = location.GetProperty("longitude").GetDouble()
        };
    }
}
