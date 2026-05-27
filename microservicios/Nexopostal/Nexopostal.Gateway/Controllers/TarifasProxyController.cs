using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para tarifas (evita que el gateway pierda query params en GET).
///
/// Endpoint público (sin autenticación) para consultar precios desde la
/// calculadora pública de nexopostal.es y desde el flujo de envío.
/// </summary>
[Route("api/nexopostal/tarifas")]
[ApiController]
public class TarifasProxyController : ControllerBase
{
    // HttpClient estatico para evitar socket exhaustion.
    internal static HttpClient _httpClient = new();
    private readonly string _ciudadanoUrl;

    public TarifasProxyController(IConfiguration config)
    {
        _ciudadanoUrl = config["Microservices:Ciudadano"] ?? "http://modulo-ciudadano:80";
    }

    /// <summary>Consulta tarifas públicas preservando la query string original.</summary>
    [HttpGet("consultar")]
    public Task<IActionResult> Consultar()
    {
        return ProxyGetRequest("api/tarifas/consultar");
    }

    /// <summary>Calcula una tarifa a partir del payload enviado por el frontend.</summary>
    [HttpPost("calcular")]
    public Task<IActionResult> Calcular()
    {
        return ProxyPostRequest("api/tarifas/calcular");
    }

    /// <summary>Reenvía una petición GET al microservicio Ciudadano sin perder parámetros de consulta.</summary>
    private async Task<IActionResult> ProxyGetRequest(string path)
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_ciudadanoUrl}/{path}{queryString}");

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

    /// <summary>Reenvía una petición POST manteniendo tanto el body como la query string originales.</summary>
    private async Task<IActionResult> ProxyPostRequest(string path)
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_ciudadanoUrl}/{path}{queryString}");

        // Reenviar el body tal cual.
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        if (!string.IsNullOrEmpty(body))
        {
            var contentType = Request.ContentType ?? "application/json";
            requestMessage.Content = new StringContent(body, System.Text.Encoding.UTF8);
            requestMessage.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                contentType.Split(';')[0].Trim());
        }

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
        }

        var response = await _httpClient.SendAsync(requestMessage);
        var responseBody = await response.Content.ReadAsStringAsync();
        var responseContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            ContentType = responseContentType,
            Content = responseBody
        };
    }
}
