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
    internal static HttpClient _httpClient = new();
    private readonly string _repartoUrl;

    public VehiculosAdminProxyController(IConfiguration config)
    {
        _repartoUrl = config["Microservices:Reparto"] ?? "http://modulo-reparto:80";
    }

    /// <summary>Lista la flota administrativa con los filtros recibidos por query string.</summary>
    [HttpGet]
    public Task<IActionResult> Listar() => Proxy(HttpMethod.Get, "api/admin-vehiculos");

    /// <summary>Obtiene un vehículo concreto por su id.</summary>
    [HttpGet("{id:int}")]
    public Task<IActionResult> Obtener(int id) => Proxy(HttpMethod.Get, $"api/admin-vehiculos/{id}");

    /// <summary>Crea un nuevo vehículo en la flota.</summary>
    [HttpPost]
    public Task<IActionResult> Crear() => Proxy(HttpMethod.Post, "api/admin-vehiculos");

    /// <summary>Actualiza la ficha de un vehículo existente.</summary>
    [HttpPut("{id:int}")]
    public Task<IActionResult> Editar(int id) => Proxy(HttpMethod.Put, $"api/admin-vehiculos/{id}");

    /// <summary>Desactiva un vehículo sin borrarlo físicamente.</summary>
    [HttpDelete("{id:int}")]
    public Task<IActionResult> Desactivar(int id) => Proxy(HttpMethod.Delete, $"api/admin-vehiculos/{id}");

    /// <summary>Reactiva un vehículo desactivado previamente.</summary>
    [HttpPost("{id:int}/reactivar")]
    public Task<IActionResult> Reactivar(int id) => Proxy(HttpMethod.Post, $"api/admin-vehiculos/{id}/reactivar");

    /// <summary>Asigna un vehículo de flota a un repartidor.</summary>
    [HttpPost("{id:int}/asignar")]
    public Task<IActionResult> Asignar(int id) => Proxy(HttpMethod.Post, $"api/admin-vehiculos/{id}/asignar");

    /// <summary>Importa vehículos iniciales a partir de los datos históricos de repartidores.</summary>
    [HttpPost("importar-desde-repartidores")]
    public Task<IActionResult> Importar() => Proxy(HttpMethod.Post, "api/admin-vehiculos/importar-desde-repartidores");

    /// <summary>Reenvía la petición al microservicio Reparto preservando JWT, query y body.</summary>
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
