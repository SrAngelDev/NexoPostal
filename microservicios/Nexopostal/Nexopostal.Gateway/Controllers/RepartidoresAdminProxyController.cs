using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para la gestión administrativa de repartidores.
/// Reenvía al microservicio Reparto preservando el JWT y el body.
/// Se usa proxy directo (en lugar del ApiGateway genérico) porque las rutas mezclan
/// IDs enteros con sub-rutas no soportadas por UrlRewriteMiddleware
/// (por ejemplo `/repartidores/identity/{guid}` o `/repartidores/{id}/reactivar`).
/// </summary>
[Route("api/nexopostal/admin-repartidores")]
[ApiController]
[Authorize(Roles = "Admin,JefeReparto")]
public class RepartidoresAdminProxyController : ControllerBase
{
    internal static HttpClient _httpClient = new();
    private readonly string _repartoUrl;

    public RepartidoresAdminProxyController(IConfiguration config)
    {
        _repartoUrl = config["Microservices:Reparto"] ?? "http://modulo-reparto:80";
    }

    /// <summary>Lista repartidores (con filtros ?oficinaJsonId=&incluirInactivos=).</summary>
    [HttpGet]
    public Task<IActionResult> Listar() =>
        Proxy(HttpMethod.Get, "api/reparto/repartidores");

    /// <summary>Crea un nuevo repartidor.</summary>
    [HttpPost]
    public Task<IActionResult> Crear() =>
        Proxy(HttpMethod.Post, "api/reparto/repartidores");

    /// <summary>Obtiene el repartidor asociado a un IdentityUserId.</summary>
    [HttpGet("identity/{userId}")]
    public Task<IActionResult> PorIdentity(string userId) =>
        Proxy(HttpMethod.Get, $"api/reparto/repartidores/identity/{userId}");

    /// <summary>Edita la ficha de un repartidor (oficina, vehículo, contacto).</summary>
    [HttpPut("{id:int}")]
    public Task<IActionResult> Editar(int id) =>
        Proxy(HttpMethod.Put, $"api/reparto/repartidores/{id}");

    /// <summary>Desactiva un repartidor (soft).</summary>
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Desactivar(int id) =>
        Proxy(HttpMethod.Delete, $"api/reparto/repartidores/{id}");

    /// <summary>Reactiva un repartidor previamente desactivado.</summary>
    [HttpPost("{id:int}/reactivar")]
    public Task<IActionResult> Reactivar(int id) =>
        Proxy(HttpMethod.Post, $"api/reparto/repartidores/{id}/reactivar");

    // Método común para reenviar las operaciones administrativas al microservicio Reparto.
    private async Task<IActionResult> Proxy(HttpMethod method, string path)
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(method, $"{_repartoUrl}/{path}{queryString}");

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
