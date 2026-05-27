using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para la gestión de usuarios por Admin.
/// Reenvía las peticiones al microservicio Auth, preservando el token JWT y el body.
/// Se usa proxy directo (en lugar del ApiGateway genérico) porque los IDs de usuario
/// son strings (GUIDs), no enteros, y el UrlRewriteMiddleware no los maneja.
/// </summary>
[Route("api/nexopostal/admin-usuarios")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AdminUsersProxyController : ControllerBase
{
    internal static HttpClient _httpClient = new();
    private readonly string _authUrl;
    private readonly string _logisticaUrl;

    public AdminUsersProxyController(IConfiguration config)
    {
        _authUrl = config["Microservices:Auth"] ?? "http://modulo-seguridad:80";
        _logisticaUrl = config["Microservices:Logistica"] ?? "http://modulo-logistica:80";
    }

    /// <summary>Lista usuarios con filtros opcionales (?rol=&bloqueado=&q=).</summary>
    [HttpGet]
    public Task<IActionResult> Listar() =>
        Proxy(HttpMethod.Get, _authUrl, "api/admin-usuarios");

    /// <summary>Obtiene el detalle de un usuario por ID.</summary>
    [HttpGet("{id}")]
    public Task<IActionResult> Detalle(string id) =>
        Proxy(HttpMethod.Get, _authUrl, $"api/admin-usuarios/{id}");

    /// <summary>Crea un nuevo empleado interno.</summary>
    [HttpPost]
    public Task<IActionResult> Crear() =>
        Proxy(HttpMethod.Post, _authUrl, "api/admin-usuarios");

    /// <summary>Edita datos básicos de un empleado (nombre/email/codigo/teléfono/rol).</summary>
    [HttpPut("{id}")]
    public Task<IActionResult> Editar(string id) =>
        Proxy(HttpMethod.Put, _authUrl, $"api/admin-usuarios/{id}");

    /// <summary>Borrado lógico de un empleado.</summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Eliminar(string id) =>
        Proxy(HttpMethod.Delete, _authUrl, $"api/admin-usuarios/{id}");

    /// <summary>Restaura un empleado borrado lógicamente.</summary>
    [HttpPost("{id}/restaurar")]
    public Task<IActionResult> Restaurar(string id) =>
        Proxy(HttpMethod.Post, _authUrl, $"api/admin-usuarios/{id}/restaurar");

    /// <summary>Cambia el rol de un usuario.</summary>
    [HttpPut("{id}/rol")]
    public Task<IActionResult> CambiarRol(string id) =>
        Proxy(HttpMethod.Put, _authUrl, $"api/admin-usuarios/{id}/rol");

    /// <summary>Bloquea el acceso de un usuario.</summary>
    [HttpPut("{id}/bloquear")]
    public Task<IActionResult> Bloquear(string id) =>
        Proxy(HttpMethod.Put, _authUrl, $"api/admin-usuarios/{id}/bloquear");

    /// <summary>Desbloquea el acceso de un usuario.</summary>
    [HttpPut("{id}/desbloquear")]
    public Task<IActionResult> Desbloquear(string id) =>
        Proxy(HttpMethod.Put, _authUrl, $"api/admin-usuarios/{id}/desbloquear");

    /// <summary>Restablece la contraseña de un usuario (flujo admin).</summary>
    [HttpPost("{id}/reset-password")]
    public Task<IActionResult> ResetPassword(string id) =>
        Proxy(HttpMethod.Post, _authUrl, $"api/admin-usuarios/{id}/reset-password");

    /// <summary>Obtiene el detalle operativo (CTA) de un usuario interno.</summary>
    [HttpGet("{id}/detalle-operativo")]
    public Task<IActionResult> DetalleOperativo(string id) =>
        Proxy(HttpMethod.Get, _logisticaUrl, $"api/operarios/admin/identity/{id}");

    /// <summary>Mueve la asignación CTA de un usuario interno.</summary>
    [HttpPut("{id}/cta")]
    public Task<IActionResult> ActualizarCta(string id) =>
        Proxy(HttpMethod.Put, _logisticaUrl, $"api/operarios/admin/identity/{id}/cta");

    /// <summary>Obtiene la oficina asignada a un usuario interno.</summary>
    [HttpGet("{id}/oficina")]
    public Task<IActionResult> ObtenerOficina(string id) =>
        Proxy(HttpMethod.Get, _logisticaUrl, $"api/operarios/admin/identity/{id}/oficina");

    /// <summary>Crea o cambia la oficina asignada a un usuario interno.</summary>
    [HttpPut("{id}/oficina")]
    public Task<IActionResult> ActualizarOficina(string id) =>
        Proxy(HttpMethod.Put, _logisticaUrl, $"api/operarios/admin/identity/{id}/oficina");

    /// <summary>Reenvía la petición al microservicio adecuado manteniendo JWT, query string y body JSON.</summary>
    private async Task<IActionResult> Proxy(HttpMethod method, string baseUrl, string path)
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(method, $"{baseUrl}/{path}{queryString}");

        // Reenviar Authorization header
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());

        // Reenviar body para POST / PUT
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
