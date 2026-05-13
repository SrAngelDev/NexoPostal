using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para oficinas (evita que el gateway pierda query params).
/// </summary>
[Route("api/nexopostal/oficinas")]
[ApiController]
public class OficinasProxyController : ControllerBase
{
    // HttpClient estatico para evitar socket exhaustion.
    private static readonly HttpClient _httpClient = new();
    private readonly string _ciudadanoUrl;

    public OficinasProxyController(IConfiguration config)
    {
        _ciudadanoUrl = config["Microservices:Ciudadano"] ?? "http://modulo-ciudadano:80";
    }

    [HttpGet("listar")]
    public Task<IActionResult> Listar()
    {
        return ProxyGetRequest("api/oficinas");
    }

    [HttpGet("buscar")]
    public Task<IActionResult> Buscar()
    {
        return ProxyGetRequest("api/oficinas/buscar");
    }

    private async Task<IActionResult> ProxyGetRequest(string path)
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_ciudadanoUrl}/{path}{queryString}");

        // Reenviar el header Authorization para consistencia con el gateway.
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
