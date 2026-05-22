using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Memory;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// DTO espejo de <c>Nexopostal.Ciudadano.DTOs.EnvioInternoServiceDto</c>.
/// Se replica aquí para evitar acoplamiento de proyectos (Intranet no referencia Ciudadano).
/// </summary>
public class EnvioInternoServiceLookupDto
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string EstadoPublico { get; set; } = string.Empty;
    public string EstadoInterno { get; set; } = string.Empty;

    public string TipoEntrega { get; set; } = "Domicilio";
    public int? OficinaOrigenId { get; set; }
    public int? OficinaDestinoId { get; set; }

    public string CodigoPostalOrigen { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;

    public string NombreDestinatario { get; set; } = string.Empty;
    public string ApellidosDestinatario { get; set; } = string.Empty;
    public string TelefonoDestinatario { get; set; } = string.Empty;
    public string? EmailDestinatario { get; set; }

    public decimal PesoKg { get; set; }
    public string Dimensiones { get; set; } = string.Empty;
    public string TipoTarifa { get; set; } = string.Empty;
    public bool Pagado { get; set; }
    public DateTime FechaCreacion { get; set; }
}

/// <summary>
/// Servicio lookup inter-servicio: consulta a Ciudadano los datos operativos
/// de un envío por NumeroExpedicion. Cachea en memoria por TTL corto para no
/// bombardear Ciudadano durante una cadena de escaneos.
/// </summary>
public interface ICiudadanoEnvioLookupService
{
    /// <summary>
    /// Devuelve los datos operativos del envío. null si no existe o si Ciudadano no responde.
    /// </summary>
    Task<EnvioInternoServiceLookupDto?> ObtenerAsync(string numeroExpedicion, CancellationToken ct = default);

    /// <summary>Invalida la caché de un envío (útil tras cambios de estado).</summary>
    void Invalidar(string numeroExpedicion);
}

public class CiudadanoEnvioLookupService : ICiudadanoEnvioLookupService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);
    private const string CachePrefix = "envio-lookup:";

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CiudadanoEnvioLookupService> _logger;

    public CiudadanoEnvioLookupService(HttpClient http, IMemoryCache cache, ILogger<CiudadanoEnvioLookupService> logger)
    {
        _http = http;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EnvioInternoServiceLookupDto?> ObtenerAsync(string numeroExpedicion, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(numeroExpedicion))
            return null;

        var key = CachePrefix + numeroExpedicion;
        if (_cache.TryGetValue<EnvioInternoServiceLookupDto>(key, out var cached) && cached is not null)
            return cached;

        try
        {
            var resp = await _http.GetAsync($"api/envios/interno/service/{Uri.EscapeDataString(numeroExpedicion)}", ct);
            if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Lookup envio {Expedicion} fallido: {Status} {Reason}",
                    numeroExpedicion, (int)resp.StatusCode, resp.ReasonPhrase);
                return null;
            }

            var dto = await resp.Content.ReadFromJsonAsync<EnvioInternoServiceLookupDto>(cancellationToken: ct);
            if (dto is not null)
            {
                _cache.Set(key, dto, CacheTtl);
            }
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando envío {Expedicion} en Ciudadano", numeroExpedicion);
            return null;
        }
    }

    public void Invalidar(string numeroExpedicion)
    {
        if (!string.IsNullOrWhiteSpace(numeroExpedicion))
            _cache.Remove(CachePrefix + numeroExpedicion);
    }
}
