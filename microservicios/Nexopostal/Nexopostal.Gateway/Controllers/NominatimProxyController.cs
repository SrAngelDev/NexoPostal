using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para Nominatim (OpenStreetMap geocoding).
/// Evita que el gateway pierda los query params en GET
/// (q, format, limit, countrycodes, etc.) que Nominatim necesita.
/// </summary>
[Route("api/nexopostal/nominatim")]
[ApiController]
public class NominatimProxyController : ControllerBase
{
    private static readonly HttpClient _httpClient = new();
    private const string NominatimBase = "https://nominatim.openstreetmap.org";

    // Nominatim ToS exige un User-Agent descriptivo.
    private const string UserAgent = "NexoPostal/1.0 (nexopostal.local; contact@nexopostal.es)";

    [HttpGet("search")]
    public Task<IActionResult> Search() => ProxyGet("search");

    [HttpGet("reverse")]
    public Task<IActionResult> Reverse() => ProxyGet("reverse");

    private async Task<IActionResult> ProxyGet(string endpoint)
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            $"{NominatimBase}/{endpoint}{queryString}");

        requestMessage.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        requestMessage.Headers.TryAddWithoutValidation("Accept", "application/json");

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
