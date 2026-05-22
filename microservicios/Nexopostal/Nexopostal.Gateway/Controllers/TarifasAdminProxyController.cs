using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para la gestión administrativa de tarifas editables.
/// Reenvía al microservicio Ciudadano preservando el JWT y el body.
/// </summary>
[Route("api/nexopostal/admin-tarifas")]
[ApiController]
[Authorize(Roles = "Admin")]
public class TarifasAdminProxyController : ControllerBase
{
    private static readonly HttpClient _httpClient = new();
    private readonly string _ciudadanoUrl;

    public TarifasAdminProxyController(IConfiguration config)
    {
        _ciudadanoUrl = config["Microservices:Ciudadano"] ?? "http://modulo-ciudadano:80";
    }

    [HttpGet]
    public Task<IActionResult> Listar() => Proxy(HttpMethod.Get, "api/admin-tarifas");

    [HttpGet("{id:int}")]
    public Task<IActionResult> Obtener(int id) => Proxy(HttpMethod.Get, $"api/admin-tarifas/{id}");

    [HttpPut("{id:int}")]
    public Task<IActionResult> Editar(int id) => Proxy(HttpMethod.Put, $"api/admin-tarifas/{id}");

    [HttpPut("bulk")]
    public Task<IActionResult> EditarBulk() => Proxy(HttpMethod.Put, "api/admin-tarifas/bulk");

    [HttpPost("reset-defaults")]
    public Task<IActionResult> Reset() => Proxy(HttpMethod.Post, "api/admin-tarifas/reset-defaults");

    private async Task<IActionResult> Proxy(HttpMethod method, string path)
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(method, $"{_ciudadanoUrl}/{path}{queryString}");

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
