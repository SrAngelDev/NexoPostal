using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para endpoints de asignaciones que necesitan preservar la query string.
///
/// La librería AspNetCore.ApiGateway pierde los query params en GET (mismo motivo
/// que <see cref="TarifasProxyController"/> u <see cref="OficinasProxyController"/>).
/// Cuando el operario escanea un código en la pantalla "Confirmar paso con escáner",
/// la intranet llama a <c>GET /api/asignaciones/buscar?codigo=...</c> y necesitamos
/// que ese <c>?codigo=</c> llegue íntegro al microservicio Logistica.
/// </summary>
[Route("api/asignaciones")]
[ApiController]
[Authorize(Roles = "Admin,Supervisor,OperarioCTA,OperarioOficina")]
public class AsignacionesProxyController : ControllerBase
{
    internal static HttpClient _httpClient = new();
    private readonly string _logisticaUrl;

    public AsignacionesProxyController(IConfiguration config)
    {
        _logisticaUrl = config["Microservices:Logistica"] ?? "http://modulo-logistica:80";
    }

    /// <summary>
    /// Busca una tarea (pendiente o en progreso) del operario autenticado por código
    /// de expedición. 404 si el código no está en sus tareas.
    /// </summary>
    [HttpGet("buscar")]
    public async Task<IActionResult> Buscar()
    {
        var queryString = Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty;
        var requestMessage = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_logisticaUrl}/api/asignaciones/buscar{queryString}");

        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());

        var response = await _httpClient.SendAsync(requestMessage);
        var body = await response.Content.ReadAsStringAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            StatusCode = (int)response.StatusCode,
            ContentType = contentType,
            Content = body
        };
    }
}
