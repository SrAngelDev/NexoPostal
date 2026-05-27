using System.Net.Http.Json;

namespace Nexopostal.Reparto.Services;

/// <summary>
/// Payload interno con el detalle de un evento de entrega que debe reflejarse en el tracking del ciudadano.
/// </summary>
public record TrackingEventoEntregaPayload(
    string NumeroSeguimiento,
    string NumeroExpedicion,
    string EstadoEntrega,
    int NumeroIntento,
    string? Observaciones,
    string? ReceptorNombre,
    string? ReceptorDni,
    double? Latitud,
    double? Longitud,
    string? FirmaDigital,
    string? FotoEntrega);

/// <summary>
/// Contrato para enviar a Ciudadano cambios de ubicación y eventos de entrega generados en Reparto.
/// </summary>
public interface ICiudadanoTrackingNotifierService
{
    /// <summary>Notifica una actualización de ubicación relevante para el tracking público.</summary>
    Task NotificarUbicacionAsync(
        string numeroSeguimiento,
        double latitud,
        double longitud,
        string tipoUbicacion,
        string? descripcion = null,
        CancellationToken cancellationToken = default);

    /// <summary>Notifica un evento de entrega completo para sincronizar estado y timeline.</summary>
    Task NotificarEventoEntregaAsync(
        TrackingEventoEntregaPayload payload,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementación HTTP hacia el microservicio Ciudadano para mantener su tracking en tiempo real actualizado.
/// </summary>
public class CiudadanoTrackingNotifierService : ICiudadanoTrackingNotifierService
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceKey;
    private readonly ILogger<CiudadanoTrackingNotifierService> _logger;

    public CiudadanoTrackingNotifierService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<CiudadanoTrackingNotifierService> logger)
    {
        _httpClient = httpClient;
        _serviceKey = configuration["CiudadanoTrackingSettings:ServiceKey"]
            ?? "nexopostal-internal-service-key-2025";
        _logger = logger;
    }

    /// <summary>
    /// Envía a Ciudadano la última ubicación útil del repartidor asociada a un envío concreto.
    /// </summary>
    public async Task NotificarUbicacionAsync(
        string numeroSeguimiento,
        double latitud,
        double longitud,
        string tipoUbicacion,
        string? descripcion = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var body = new
            {
                numeroSeguimiento,
                latitud,
                longitud,
                tipoUbicacion,
                descripcion
            };

            using var request = CrearRequestInterna("api/envios/interno/tracking/ubicacion", body);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "No se pudo notificar ubicación tracking para {Seguimiento}. Status: {Status}. Body: {Body}",
                    numeroSeguimiento,
                    (int)response.StatusCode,
                    payload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error notificando ubicación tracking para {Seguimiento}",
                numeroSeguimiento);
        }
    }

    /// <summary>
    /// Publica en Ciudadano el resultado de un intento de entrega con todos los metadatos disponibles.
    /// </summary>
    public async Task NotificarEventoEntregaAsync(
        TrackingEventoEntregaPayload payload,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CrearRequestInterna("api/envios/interno/tracking/evento-entrega", payload);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "No se pudo notificar evento de entrega para {Seguimiento}. Status: {Status}. Body: {Body}",
                    payload.NumeroSeguimiento,
                    (int)response.StatusCode,
                    body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Error notificando evento de entrega tracking para {Seguimiento}",
                payload.NumeroSeguimiento);
        }
    }

    /// <summary>
    /// Construye una petición interna autenticada con X-Service-Key para el endpoint de Ciudadano.
    /// </summary>
    private HttpRequestMessage CrearRequestInterna<TPayload>(string path, TPayload payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };

        request.Headers.Add("X-Service-Key", _serviceKey);
        return request;
    }
}
