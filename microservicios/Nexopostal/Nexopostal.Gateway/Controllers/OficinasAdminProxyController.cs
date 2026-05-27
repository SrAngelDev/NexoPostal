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
    internal static HttpClient _httpClient = new();
    private readonly string _intranetUrl;

    public OficinasAdminProxyController(IConfiguration config)
    {
        _intranetUrl = config["Microservices:Logistica"]
            ?? config["Microservices:Intranet"]
            ?? "http://modulo-intranet:80";
    }

    /// <summary>Lista las oficinas postales gestionables desde administración.</summary>
    [HttpGet]
    public Task<IActionResult> Listar() => Proxy(HttpMethod.Get, "api/admin-oficinas");

    /// <summary>Obtiene una oficina postal por id.</summary>
    [HttpGet("{id:int}")]
    public Task<IActionResult> Obtener(int id) => Proxy(HttpMethod.Get, $"api/admin-oficinas/{id}");

    /// <summary>Crea una nueva oficina postal.</summary>
    [HttpPost]
    public Task<IActionResult> Crear() => Proxy(HttpMethod.Post, "api/admin-oficinas");

    /// <summary>Actualiza una oficina postal existente.</summary>
    [HttpPut("{id:int}")]
    public Task<IActionResult> Editar(int id) => Proxy(HttpMethod.Put, $"api/admin-oficinas/{id}");

    /// <summary>Desactiva una oficina sin eliminar su histórico.</summary>
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Desactivar(int id) => Proxy(HttpMethod.Delete, $"api/admin-oficinas/{id}");

    /// <summary>Reactiva una oficina deshabilitada previamente.</summary>
    [HttpPost("{id:int}/reactivar")]
    public Task<IActionResult> Reactivar(int id) => Proxy(HttpMethod.Post, $"api/admin-oficinas/{id}/reactivar");

    /// <summary>Puentea la petición hacia Intranet manteniendo JWT, query y cuerpo JSON.</summary>
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
