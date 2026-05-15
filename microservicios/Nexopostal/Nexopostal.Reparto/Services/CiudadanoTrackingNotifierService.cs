using System.Net.Http.Json;

namespace Nexopostal.Reparto.Services;

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

public interface ICiudadanoTrackingNotifierService
{
    Task NotificarUbicacionAsync(
        string numeroSeguimiento,
        double latitud,
        double longitud,
        string tipoUbicacion,
        string? descripcion = null,
        CancellationToken cancellationToken = default);

    Task NotificarEventoEntregaAsync(
        TrackingEventoEntregaPayload payload,
        CancellationToken cancellationToken = default);
}

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
