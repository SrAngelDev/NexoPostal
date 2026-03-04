using Microsoft.AspNetCore.Mvc;

namespace Nexopostal.Gateway.Controllers;

/// <summary>
/// Controlador que actúa como proxy directo para la descarga de archivos binarios
/// (PDFs de etiquetas y facturas) desde los microservicios.
///
/// La librería AspNetCore.ApiGateway no maneja correctamente respuestas binarias
/// (las convierte a texto/JSON), por lo que este controlador las puentea directamente
/// usando HttpClient.
/// </summary>
[Route("api/nexopostal")]
[ApiController]
public class FileProxyController : ControllerBase
{
    // HttpClient estático para evitar socket exhaustion.
    // NO pasa por IHttpClientFactory para evitar el ErrorPropagationHandler
    // registrado en ConfigureHttpClientDefaults, que lee el body como string
    // y destruiría el contenido binario en caso de error.
    private static readonly HttpClient _httpClient = new();
    private readonly string _ciudadanoUrl;

    public FileProxyController(IConfiguration config)
    {
        _ciudadanoUrl = config["Microservices:Ciudadano"] ?? "http://modulo-ciudadano:80";
    }

    /// <summary>
    /// Proxy para descargar la etiqueta PDF de un envío
    /// </summary>
    [HttpGet("envios/etiqueta/{numero}")]
    public async Task<IActionResult> DescargarEtiqueta(string numero)
    {
        return await ProxyFileRequest($"api/envios/etiqueta/{numero}");
    }

    /// <summary>
    /// Proxy para descargar la factura PDF de un envío
    /// </summary>
    [HttpGet("envios/factura/{numero}")]
    public async Task<IActionResult> DescargarFactura(string numero)
    {
        return await ProxyFileRequest($"api/envios/factura/{numero}");
    }

    /// <summary>
    /// Reenvía la petición al microservicio y devuelve la respuesta binaria tal cual.
    /// </summary>
    private async Task<IActionResult> ProxyFileRequest(string path)
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_ciudadanoUrl}/{path}");

        // Reenviar el header Authorization para que el microservicio valide el token
        if (Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            requestMessage.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
        }

        var response = await _httpClient.SendAsync(requestMessage);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            return StatusCode((int)response.StatusCode, errorBody);
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/pdf";
        var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"') ?? "archivo.pdf";

        return File(bytes, contentType, fileName);
    }
}
