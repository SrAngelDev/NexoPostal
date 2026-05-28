using System.Net.Http.Json;

namespace Nexopostal.Reparto.Services;

/// <summary>
/// Sincroniza cambios de nombre de empleado con el microservicio de autenticación
/// para que el JWT emitido en el siguiente login refleje el nuevo valor.
/// </summary>
public interface IAuthUserSyncService
{
    /// <summary>
    /// Actualiza el NombreCompleto del usuario Identity correspondiente al repartidor editado.
    /// Fire-and-forget tolerante a fallos: nunca propaga excepciones.
    /// </summary>
    Task SincronizarNombreAsync(string identityUserId, string nuevoNombre);
}

public class AuthUserSyncService : IAuthUserSyncService
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceKey;
    private readonly ILogger<AuthUserSyncService> _logger;

    public AuthUserSyncService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<AuthUserSyncService> logger)
    {
        _httpClient = httpClient;
        _serviceKey = configuration["AuthSettings:ServiceKey"]
            ?? "nexopostal-internal-service-key-2025";
        _logger = logger;
    }

    public async Task SincronizarNombreAsync(string identityUserId, string nuevoNombre)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
            return;

        try
        {
            var request = new HttpRequestMessage(
                HttpMethod.Put,
                $"/api/internal/auth/session/usuarios/{identityUserId}/nombre")
            {
                Content = JsonContent.Create(new { nombreCompleto = nuevoNombre })
            };
            request.Headers.Add("X-Service-Key", _serviceKey);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                _logger.LogWarning("AuthUserSync: fallo al actualizar nombre para {UserId} → HTTP {Status}", identityUserId, (int)response.StatusCode);
            else
                _logger.LogInformation("AuthUserSync: NombreCompleto actualizado para {UserId}", identityUserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AuthUserSync: error de comunicación con modulo-seguridad al actualizar nombre para {UserId}", identityUserId);
        }
    }
}
