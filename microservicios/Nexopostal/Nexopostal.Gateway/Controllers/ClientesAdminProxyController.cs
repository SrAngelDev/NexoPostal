using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para gestión administrativa de clientes (vista 360).
/// Combina Auth (identidad) y Ciudadano (perfil + agenda + envíos).
/// </summary>
[Route("api/nexopostal/admin-clientes")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ClientesAdminProxyController : ControllerBase
{
    internal static HttpClient _httpClient = new();
    private readonly string _authUrl;
    private readonly string _ciudadanoUrl;

    public ClientesAdminProxyController(IConfiguration config)
    {
        _authUrl = config["Microservices:Auth"] ?? "http://modulo-seguridad:80";
        _ciudadanoUrl = config["Microservices:Ciudadano"] ?? "http://modulo-ciudadano:80";
    }

    /// <summary>Lista todos los usuarios con rol Cliente (fuerza rol=Cliente).</summary>
    [HttpGet]
    public Task<IActionResult> Listar()
    {
        // Forzamos rol=Cliente preservando el resto de query params.
        var query = QueryHelpers("rol", "Cliente");
        return Proxy(HttpMethod.Get, _authUrl, $"api/admin-usuarios{query}", includeBody: false);
    }

    /// <summary>Datos básicos del usuario (identity).</summary>
    [HttpGet("{id}")]
    public Task<IActionResult> Detalle(string id) =>
        Proxy(HttpMethod.Get, _authUrl, $"api/admin-usuarios/{id}", includeBody: false);

    /// <summary>Perfil 360 del cliente: identity + perfil + agenda + envíos.</summary>
    [HttpGet("{id}/perfil-completo")]
    public Task<IActionResult> PerfilCompleto(string id) =>
        Proxy(HttpMethod.Get, _ciudadanoUrl, $"api/admin-clientes/{id}/perfil-completo", includeBody: false);

    /// <summary>Bloquea el acceso de un cliente.</summary>
    [HttpPut("{id}/bloquear")]
    public Task<IActionResult> Bloquear(string id) =>
        Proxy(HttpMethod.Put, _authUrl, $"api/admin-usuarios/{id}/bloquear", includeBody: true);

    /// <summary>Desbloquea el acceso de un cliente.</summary>
    [HttpPut("{id}/desbloquear")]
    public Task<IActionResult> Desbloquear(string id) =>
        Proxy(HttpMethod.Put, _authUrl, $"api/admin-usuarios/{id}/desbloquear", includeBody: true);

    /// <summary>Restablece la contraseña del cliente (admin).</summary>
    [HttpPost("{id}/reset-password")]
    public Task<IActionResult> ResetPassword(string id) =>
        Proxy(HttpMethod.Post, _authUrl, $"api/admin-usuarios/{id}/reset-password", includeBody: true);

    /// <summary>
    /// Añade o sustituye un parámetro de query manteniendo intactos los demás filtros enviados por el frontend.
    /// </summary>
    private string QueryHelpers(string key, string value)
    {
        var existing = Request.QueryString.HasValue ? Request.QueryString.Value!.TrimStart('?') : string.Empty;
        // Quitar duplicado del mismo key si existiera.
        var parts = existing.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => !p.StartsWith(key + "=", StringComparison.OrdinalIgnoreCase));
        var rebuilt = string.Join("&", parts.Append($"{key}={Uri.EscapeDataString(value)}"));
        return "?" + rebuilt;
    }

    /// <summary>Puentea la petición al microservicio objetivo preservando autorización y, cuando toca, el body.</summary>
    private async Task<IActionResult> Proxy(HttpMethod method, string baseUrl, string pathWithQuery, bool includeBody)
    {
        var url = $"{baseUrl}/{pathWithQuery}";
        var requestMessage = new HttpRequestMessage(method, url);

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());

        if (includeBody && (method == HttpMethod.Post || method == HttpMethod.Put))
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
