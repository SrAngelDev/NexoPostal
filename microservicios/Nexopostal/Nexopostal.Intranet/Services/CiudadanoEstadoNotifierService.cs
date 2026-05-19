using System.Text;
using System.Text.Json;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Notifica al microservicio Ciudadano cuando el estado interno de un paquete
/// cambia durante el flujo logístico (escaneo manual o simulación automática).
///
/// Esto mantiene sincronizados:
///   - Intranet: HistorialEstadoPaquete (auditoría operativa)
///   - Ciudadano: Envio.EstadoInternoActual (tracking público + SignalR)
///
/// Utiliza autenticación por service key (X-Service-Key header) y puede
/// localizar el envío por NumeroSeguimiento o NumeroExpedicion.
/// </summary>
public interface ICiudadanoEstadoNotifierService
{
    /// <summary>
    /// Notifica a Ciudadano que el estado interno de un paquete ha cambiado.
    /// Proporciona al menos uno de los dos identificadores.
    /// </summary>
    Task NotificarEstadoAsync(
        string? numeroSeguimiento,
        string? numeroExpedicion,
        string estadoInterno,
        string descripcion);
}

public class CiudadanoEstadoNotifierService : ICiudadanoEstadoNotifierService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CiudadanoEstadoNotifierService> _logger;

    public CiudadanoEstadoNotifierService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CiudadanoEstadoNotifierService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotificarEstadoAsync(
        string? numeroSeguimiento,
        string? numeroExpedicion,
        string estadoInterno,
        string descripcion)
    {
        if (string.IsNullOrWhiteSpace(numeroSeguimiento) && string.IsNullOrWhiteSpace(numeroExpedicion))
        {
            _logger.LogWarning("NotificarEstadoAsync: se requiere NumeroSeguimiento o NumeroExpedicion");
            return;
        }

        try
        {
            var serviceKey = _configuration["CiudadanoSettings:ServiceKey"]
                ?? _configuration["InterServiceSettings:ServiceKey"]
                ?? "nexopostal-internal-service-key-2025";

            var payload = new
            {
                numeroSeguimiento,
                numeroExpedicion,
                estadoInterno,
                descripcion
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/envios/interno/tracking/scan-estado");

            request.Content = content;
            request.Headers.Add("X-Service-Key", serviceKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Ciudadano devolvió {Status} al notificar estado {Estado} para {Id}: {Body}",
                    (int)response.StatusCode, estadoInterno,
                    numeroSeguimiento ?? numeroExpedicion, body);
            }
            else
            {
                _logger.LogDebug(
                    "Estado {Estado} notificado a Ciudadano para {Id}",
                    estadoInterno, numeroSeguimiento ?? numeroExpedicion);
            }
        }
        catch (Exception ex)
        {
            // No propagar — la simulación/escaneo no debe fallar por un error de notificación
            _logger.LogError(ex,
                "Error notificando estado {Estado} a Ciudadano para {Id}",
                estadoInterno, numeroSeguimiento ?? numeroExpedicion);
        }
    }
}
