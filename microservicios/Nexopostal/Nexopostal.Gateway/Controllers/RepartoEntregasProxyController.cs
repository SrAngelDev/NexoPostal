using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para GET /api/nexopostal/reparto/entregas?rutaId=...
/// AspNetCore.ApiGateway pierde la query string al reenviar GETs, por lo que
/// el controller Reparto recibía rutaId=null y devolvía 400.
/// Solo cubre el endpoint raíz; sub-paths como entregas/pendientes-asignacion
/// o entregas/{id}/reasignar siguen pasando por el orquestrador (matching
/// exacto del path con sufijo "$" en DirectProxyPaths).
/// </summary>
[Route("api/nexopostal/reparto/entregas")]
[ApiController]
public class RepartoEntregasProxyController : ControllerBase
{
    private static readonly HttpClient _httpClient = new();
    private readonly string _repartoUrl;

    public RepartoEntregasProxyController(IConfiguration config)
    {
        _repartoUrl = config["Microservices:Reparto"] ?? "http://modulo-reparto:80";
    }

    [HttpGet("")]
    public async Task<IActionResult> Get()
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
}
