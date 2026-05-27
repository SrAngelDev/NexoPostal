using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para la gestión administrativa de CTAs.
/// Reenvía al microservicio Intranet preservando el JWT y el body.
/// </summary>
[Route("api/nexopostal/admin-ctas")]
[ApiController]
[Authorize(Roles = "Admin")]
public class CtasAdminProxyController : ControllerBase
{
    internal static HttpClient _httpClient = new();
    private readonly string _intranetUrl;

    public CtasAdminProxyController(IConfiguration config)
    {
        _intranetUrl = config["Microservices:Logistica"] ?? "http://modulo-logistica:80";
    }

    /// <summary>Lista todos los CTAs (resumen).</summary>
    [HttpGet]
    public Task<IActionResult> Listar() =>
        Proxy(HttpMethod.Get, "api/ctas");

    /// <summary>Detalle completo de un CTA.</summary>
    [HttpGet("{id:int}")]
    public Task<IActionResult> Detalle(int id) =>
        Proxy(HttpMethod.Get, $"api/ctas/{id}");

    /// <summary>Crea un nuevo CTA.</summary>
    [HttpPost]
    public Task<IActionResult> Crear() =>
        Proxy(HttpMethod.Post, "api/ctas");

    /// <summary>Edita un CTA existente.</summary>
    [HttpPut("{id:int}")]
    public Task<IActionResult> Editar(int id) =>
        Proxy(HttpMethod.Put, $"api/ctas/{id}");

    /// <summary>Desactiva un CTA (soft).</summary>
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Desactivar(int id) =>
        Proxy(HttpMethod.Delete, $"api/ctas/{id}");

    /// <summary>Reactiva un CTA.</summary>
    [HttpPost("{id:int}/reactivar")]
    public Task<IActionResult> Reactivar(int id) =>
        Proxy(HttpMethod.Post, $"api/ctas/{id}/reactivar");

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
            StatusCode  = (int)response.StatusCode,
            ContentType = contentType,
            Content     = responseBody
        };
    }
}
