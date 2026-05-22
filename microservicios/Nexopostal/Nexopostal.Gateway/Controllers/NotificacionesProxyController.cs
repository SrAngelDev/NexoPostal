using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Proxy directo para enviar notificaciones broadcast desde el Admin.
/// Reenvía al microservicio de Logística (Intranet) que expone SignalR.
/// </summary>
[Route("api/nexopostal/notificaciones")]
[ApiController]
[Authorize(Roles = "Admin")]
public class NotificacionesProxyController : ControllerBase
{
    private static readonly HttpClient _httpClient = new();
    private readonly string _logisticaUrl;

    public NotificacionesProxyController(IConfiguration config)
    {
        _logisticaUrl = config["Microservices:Logistica"] ?? "http://modulo-logistica:80";
    }

    [HttpPost("broadcast")]
    public Task<IActionResult> Broadcast() =>
        Proxy(HttpMethod.Post, $"api/notificaciones/broadcast");

    private async Task<IActionResult> Proxy(HttpMethod method, string path)
    {
        var requestMessage = new HttpRequestMessage(method, $"{_logisticaUrl}/{path}");

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
