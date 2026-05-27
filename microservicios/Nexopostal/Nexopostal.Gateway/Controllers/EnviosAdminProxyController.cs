using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo al microservicio Ciudadano para el panel admin de envíos.
/// </summary>
[Route("api/nexopostal/admin-envios")]
[ApiController]
[Authorize(Roles = "Admin")]
public class EnviosAdminProxyController : ControllerBase
{
    internal static HttpClient _httpClient = new();
    private readonly string _ciudadanoUrl;

    public EnviosAdminProxyController(IConfiguration config)
    {
        _ciudadanoUrl = config["Microservices:Ciudadano"] ?? "http://modulo-ciudadano:80";
    }

    [HttpGet]
    public Task<IActionResult> Listar() => Proxy(HttpMethod.Get, "api/admin-envios");

    [HttpGet("{numero}")]
    public Task<IActionResult> Obtener(string numero) => Proxy(HttpMethod.Get, $"api/admin-envios/{numero}");

    [HttpPut("{numero}/estado")]
    public Task<IActionResult> CambiarEstado(string numero) => Proxy(HttpMethod.Put, $"api/admin-envios/{numero}/estado");

    [HttpPost("{numero}/anular")]
    public Task<IActionResult> Anular(string numero) => Proxy(HttpMethod.Post, $"api/admin-envios/{numero}/anular");

    [HttpPost("{numero}/reabrir")]
    public Task<IActionResult> Reabrir(string numero) => Proxy(HttpMethod.Post, $"api/admin-envios/{numero}/reabrir");

    private async Task<IActionResult> Proxy(HttpMethod method, string path)
    {
        var qs = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(method, $"{_ciudadanoUrl}/{path}{qs}");

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());

        if (method == HttpMethod.Post || method == HttpMethod.Put)
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            if (!string.IsNullOrEmpty(body))
                requestMessage.Content = new StringContent(body, Encoding.UTF8, "application/json");
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
