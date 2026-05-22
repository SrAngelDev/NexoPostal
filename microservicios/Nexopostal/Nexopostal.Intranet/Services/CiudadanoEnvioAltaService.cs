using System.Net.Http.Json;
using Nexopostal.Intranet.DTOs;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// DTO espejo del <c>EnvioCreadoDto</c> de Ciudadano (subset utilizado por Intranet).
/// </summary>
public class EnvioAltaResultadoDto
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string NumeroExpedicion { get; set; } = string.Empty;
    public decimal CosteCalculado { get; set; }
    public string EstadoActual { get; set; } = string.Empty;
    public string TipoEntrega { get; set; } = "Domicilio";
    public int? OficinaOrigenId { get; set; }
    public int? OficinaDestinoId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public string UrlEtiqueta { get; set; } = string.Empty;
}

/// <summary>
/// Cliente inter-servicio para dar de alta presencialmente un envío en oficina,
/// llamando al endpoint <c>POST /api/envios/interno/service/alta-oficina</c> de Ciudadano.
/// </summary>
public interface ICiudadanoEnvioAltaService
{
    /// <summary>
    /// Crea en Ciudadano el envío presencial. La oficina origen se transmite por header
    /// (no por body) para reforzar que es un dato controlado por Intranet.
    /// </summary>
    Task<EnvioAltaResultadoDto?> CrearAsync(
        AltaEnvioOficinaIntranetDto dto,
        int oficinaOrigenId,
        CancellationToken ct = default);
}

public class CiudadanoEnvioAltaService : ICiudadanoEnvioAltaService
{
    private readonly HttpClient _http;
    private readonly ILogger<CiudadanoEnvioAltaService> _logger;

    public CiudadanoEnvioAltaService(HttpClient http, ILogger<CiudadanoEnvioAltaService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<EnvioAltaResultadoDto?> CrearAsync(
        AltaEnvioOficinaIntranetDto dto,
        int oficinaOrigenId,
        CancellationToken ct = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "api/envios/interno/service/alta-oficina")
            {
                Content = JsonContent.Create(dto)
            };
            req.Headers.Add("X-Oficina-Origen-Id", oficinaOrigenId.ToString());

            using var resp = await _http.SendAsync(req, ct);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning(
                    "Alta envío Ciudadano falló: {Status} {Reason} — {Body}",
                    (int)resp.StatusCode, resp.ReasonPhrase, body);
                return null;
            }

            return await resp.Content.ReadFromJsonAsync<EnvioAltaResultadoDto>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invocando alta-oficina en Ciudadano");
            return null;
        }
    }
}
