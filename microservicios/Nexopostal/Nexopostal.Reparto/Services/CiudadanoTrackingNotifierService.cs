using System.Net.Http.Json;

namespace Nexopostal.Reparto.Services;

public interface ICiudadanoTrackingNotifierService
{
    Task NotificarUbicacionAsync(
        string numeroSeguimiento,
        double latitud,
        double longitud,
        string tipoUbicacion,
        string? descripcion = null,
        CancellationToken cancellationToken = default);
}

public class CiudadanoTrackingNotifierService : ICiudadanoTrackingNotifierService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CiudadanoTrackingNotifierService> _logger;

    public CiudadanoTrackingNotifierService(
        HttpClient httpClient,
        ILogger<CiudadanoTrackingNotifierService> logger)
    {
        _httpClient = httpClient;
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

            var response = await _httpClient.PostAsJsonAsync(
                "api/envios/interno/tracking/ubicacion",
                body,
                cancellationToken);

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
}
