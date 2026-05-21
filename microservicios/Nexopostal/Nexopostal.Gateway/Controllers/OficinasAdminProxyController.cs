using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para la gestión administrativa de oficinas postales.
/// Reenvía al microservicio Intranet (logística) preservando JWT y body.
/// </summary>
[Route("api/nexopostal/admin-oficinas")]
[ApiController]
[Authorize(Roles = "Admin")]
public class OficinasAdminProxyController : ControllerBase
{
    private static readonly HttpClient _httpClient = new();
    private readonly string _intranetUrl;

    public OficinasAdminProxyController(IConfiguration config)
    {
        _intranetUrl = config["Microservices:Logistica"]
            ?? config["Microservices:Intranet"]
            ?? "http://modulo-intranet:80";
    }

    [HttpGet]
    public Task<IActionResult> Listar() => Proxy(HttpMethod.Get, "api/admin-oficinas");

    [HttpGet("{id:int}")]
    public Task<IActionResult> Obtener(int id) => Proxy(HttpMethod.Get, $"api/admin-oficinas/{id}");

    [HttpPost]
    public Task<IActionResult> Crear() => Proxy(HttpMethod.Post, "api/admin-oficinas");

    [HttpPut("{id:int}")]
    public Task<IActionResult> Editar(int id) => Proxy(HttpMethod.Put, $"api/admin-oficinas/{id}");

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Desactivar(int id) => Proxy(HttpMethod.Delete, $"api/admin-oficinas/{id}");

    [HttpPost("{id:int}/reactivar")]
    public Task<IActionResult> Reactivar(int id) => Proxy(HttpMethod.Post, $"api/admin-oficinas/{id}/reactivar");

    private async Task<IActionResult> Proxy(HttpMethod method, string path)
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(method, $"{_intranetUrl}/{path}{queryString}");

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
