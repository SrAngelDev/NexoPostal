using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio que expone las oficinas postales.
///
/// FUENTE DE VERDAD: tabla <c>OficinasPostales</c> en BD (Intranet).
/// FALLBACK: <c>Data/oficinas.json</c> JSON-LD si la tabla está vacía o falla la BD
/// (transición / arranque inicial antes del seeding).
///
/// El servicio se registra como <b>Singleton</b> y mantiene una caché en memoria que
/// se invalida vía <see cref="Invalidar"/> tras cualquier escritura administrativa.
/// </summary>
public class OficinasJsonService
{
    private readonly ILogger<OficinasJsonService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly string _jsonPath;

    private static readonly object _cacheLock = new();
    private static List<OficinaJsonDto>? _cache;

    public OficinasJsonService(
        ILogger<OficinasJsonService> logger,
        IServiceScopeFactory scopeFactory,
        IWebHostEnvironment env)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _jsonPath = Path.Combine(env.ContentRootPath, "Data", "oficinas.json");
    }

    /// <summary>
    /// Invalida la caché en memoria. Llamar tras cualquier escritura administrativa.
    /// </summary>
    public void Invalidar()
    {
        lock (_cacheLock)
        {
            _cache = null;
        }
        _logger.LogInformation("Caché de oficinas invalidada");
    }

    // ───────────────────────────────────────────────────────────────
    //  Consultas básicas
    // ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Obtiene todas las oficinas activas (cacheadas en memoria).
    /// </summary>
    public List<OficinaJsonDto> ObtenerTodas()
    {
        if (_cache != null) return _cache;

        lock (_cacheLock)
        {
            if (_cache != null) return _cache;
            _cache = CargarDesdeBdConFallback();
            return _cache;
        }
    }

    /// <summary>
    /// Carga las oficinas leyendo directamente del fichero JSON estático.
    /// Usado por el seeder en la primera ejecución y como fallback si la BD no responde.
    /// </summary>
    public List<OficinaJsonDto> CargarDesdeJsonFile()
    {
        _logger.LogInformation("Cargando oficinas desde fichero {Path}", _jsonPath);

        var json = File.ReadAllText(_jsonPath);
        using var doc = JsonDocument.Parse(json);

        var graph = doc.RootElement.GetProperty("@graph");
        var oficinas = new List<OficinaJsonDto>();

        foreach (var item in graph.EnumerateArray())
        {
            oficinas.Add(TransformarOficina(item));
        }

        _logger.LogInformation("Cargadas {Count} oficinas desde JSON", oficinas.Count);
        return oficinas;
    }

    private List<OficinaJsonDto> CargarDesdeBdConFallback()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IntranetDbContext>();
            var lista = db.OficinasPostales
                .AsNoTracking()
                .Where(o => o.Activo)
                .OrderBy(o => o.Id)
                .Select(o => new OficinaJsonDto
                {
                    Id = o.Id,
                    Nombre = o.Nombre,
                    Direccion = o.Direccion,
                    CodigoPostal = o.CodigoPostal,
                    Ciudad = o.Ciudad,
                    Horario = o.Horario,
                    Servicios = o.Servicios,
                    Latitud = o.Latitud,
                    Longitud = o.Longitud
                })
                .ToList();

            if (lista.Count > 0)
            {
                _logger.LogInformation("Cargadas {Count} oficinas desde BD", lista.Count);
                return lista;
            }

            _logger.LogWarning("La tabla OficinasPostales está vacía; usando fallback JSON");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo leer OficinasPostales de BD; usando fallback JSON");
        }

        return CargarDesdeJsonFile();
    }

    /// <summary>
    /// Busca oficinas cuyo código postal coincida o empiece por el valor dado.
    /// Primero coincidencia exacta; si no hay, por prefijo de 3 dígitos.
    /// </summary>
    public List<OficinaJsonDto> BuscarPorCodigoPostal(string codigoPostal)
    {
        var todas = ObtenerTodas();
        var cp = codigoPostal.Trim();

        var exactas = todas.Where(o => o.CodigoPostal == cp).ToList();
        if (exactas.Count > 0) return exactas;

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
    /// Obtiene una oficina por su ID.
    /// </summary>
    public OficinaJsonDto? ObtenerPorId(int id)
    {
        return ObtenerTodas().FirstOrDefault(o => o.Id == id);
    }

    // ───────────────────────────────────────────────────────────────
    //  Resolución automática: CP → Oficina más cercana
    // ───────────────────────────────────────────────────────────────

    public OficinaJsonDto? ResolverOficinaMasCercana(string codigoPostal)
    {
        var todas = ObtenerTodas();
        var cp = codigoPostal.Trim();

        var exactas = todas.Where(o => o.CodigoPostal == cp).ToList();
        if (exactas.Count > 0) return exactas[0];

        if (cp.Length >= 3)
        {
            var prefijo3 = cp[..3];
            var zona3 = todas.Where(o => o.CodigoPostal.StartsWith(prefijo3)).ToList();
            if (zona3.Count > 0) return zona3[0];
        }

        if (cp.Length >= 2)
        {
            var prefijo2 = cp[..2];
            var provincia = todas.Where(o => o.CodigoPostal.StartsWith(prefijo2)).ToList();
            if (provincia.Count > 0) return provincia[0];
        }

        return null;
    }

    public OficinaJsonDto? ResolverOficinaMasCercana(
        string codigoPostal,
        double latitudReferencia,
        double longitudReferencia)
    {
        var todas = ObtenerTodas();
        var cp = codigoPostal.Trim();

        List<OficinaJsonDto> candidatas;

        candidatas = todas.Where(o => o.CodigoPostal == cp).ToList();
        if (candidatas.Count == 1) return candidatas[0];

        if (candidatas.Count == 0 && cp.Length >= 3)
            candidatas = todas.Where(o => o.CodigoPostal.StartsWith(cp[..3])).ToList();

        if (candidatas.Count == 0 && cp.Length >= 2)
            candidatas = todas.Where(o => o.CodigoPostal.StartsWith(cp[..2])).ToList();

        if (candidatas.Count == 0) return null;

        return candidatas
            .Where(o => o.Latitud.HasValue && o.Longitud.HasValue)
            .OrderBy(o => CalcularDistanciaKm(
                latitudReferencia, longitudReferencia,
                o.Latitud!.Value, o.Longitud!.Value))
            .FirstOrDefault()
            ?? candidatas[0];
    }

    // ───────────────────────────────────────────────────────────────
    //  Utilidades
    // ───────────────────────────────────────────────────────────────

    public static double CalcularDistanciaKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;

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
