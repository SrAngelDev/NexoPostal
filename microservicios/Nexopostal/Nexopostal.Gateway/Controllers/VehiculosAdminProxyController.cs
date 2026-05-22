using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para la gestión administrativa de vehículos.
/// Reenvía al microservicio Reparto preservando JWT y body.
/// </summary>
[Route("api/nexopostal/admin-vehiculos")]
[ApiController]
[Authorize(Roles = "Admin")]
public class VehiculosAdminProxyController : ControllerBase
{
    private static readonly HttpClient _httpClient = new();
    private readonly string _repartoUrl;

    public VehiculosAdminProxyController(IConfiguration config)
    {
        _repartoUrl = config["Microservices:Reparto"] ?? "http://modulo-reparto:80";
    }

    [HttpGet]
    public Task<IActionResult> Listar() => Proxy(HttpMethod.Get, "api/admin-vehiculos");

    [HttpGet("{id:int}")]
    public Task<IActionResult> Obtener(int id) => Proxy(HttpMethod.Get, $"api/admin-vehiculos/{id}");

    [HttpPost]
    public Task<IActionResult> Crear() => Proxy(HttpMethod.Post, "api/admin-vehiculos");

    [HttpPut("{id:int}")]
    public Task<IActionResult> Editar(int id) => Proxy(HttpMethod.Put, $"api/admin-vehiculos/{id}");

    [HttpDelete("{id:int}")]
    public Task<IActionResult> Desactivar(int id) => Proxy(HttpMethod.Delete, $"api/admin-vehiculos/{id}");

    [HttpPost("{id:int}/reactivar")]
    public Task<IActionResult> Reactivar(int id) => Proxy(HttpMethod.Post, $"api/admin-vehiculos/{id}/reactivar");

    [HttpPost("{id:int}/asignar")]
    public Task<IActionResult> Asignar(int id) => Proxy(HttpMethod.Post, $"api/admin-vehiculos/{id}/asignar");

    [HttpPost("importar-desde-repartidores")]
    public Task<IActionResult> Importar() => Proxy(HttpMethod.Post, "api/admin-vehiculos/importar-desde-repartidores");

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
            StatusCode = (int)response.StatusCode,
            ContentType = contentType,
            Content = responseBody
        };
    }
}
