using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para:
///   GET  /api/nexopostal/reparto/entregas?rutaId=...
///   POST /api/nexopostal/reparto/confirmar?entregaId=...
/// AspNetCore.ApiGateway pierde la query string en GET y en POST,
/// por lo que estos endpoints van por proxy controller propio.
/// </summary>
[Route("api/nexopostal/reparto")]
[ApiController]
public class RepartoEntregasProxyController : ControllerBase
{
    internal static HttpClient _httpClient = new();
    private readonly string _repartoUrl;

    public RepartoEntregasProxyController(IConfiguration config)
    {
        _repartoUrl = config["Microservices:Reparto"] ?? "http://modulo-reparto:80";
    }

    /// <summary>Lista las entregas de una ruta conservando la query string que necesita Reparto.</summary>
    [HttpGet("entregas")]
    public async Task<IActionResult> GetEntregas()
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_repartoUrl}/api/reparto/entregas{queryString}");

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
        }

        var response = await _httpClient.SendAsync(requestMessage);
        var body = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            ContentType = contentType,
            Content = body
        };
    }

    /// <summary>
    /// GET /api/nexopostal/reparto/vehiculos[?oficinaJsonId=N]
    /// Lista vehículos activos de la flota para el selector del JefeReparto.
    /// </summary>
    [HttpGet("vehiculos")]
    public async Task<IActionResult> GetVehiculos()
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_repartoUrl}/api/reparto/vehiculos{queryString}");

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());

        var response = await _httpClient.SendAsync(requestMessage);
        var body = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            ContentType = contentType,
            Content = body
        };
    }

    /// <summary>
    /// POST /api/nexopostal/reparto/confirmar?entregaId=N
    /// Reenvía cuerpo JSON + query string al backend de reparto.
    /// El gateway library no preserva query params en POST.
    /// </summary>
    [HttpPost("confirmar")]
    public async Task<IActionResult> PostConfirmar()
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_repartoUrl}/api/reparto/confirmar{queryString}");

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());

        if (Request.ContentLength > 0 || Request.Headers.ContainsKey("Content-Type"))
        {
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var bodyText = await reader.ReadToEndAsync();
            Request.Body.Position = 0;
            requestMessage.Content = new StringContent(bodyText, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(requestMessage);
        var responseBody = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            ContentType = contentType,
            Content = responseBody
        };
    }
}
