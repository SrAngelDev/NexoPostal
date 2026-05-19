using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Nexopostal.Gateway.Services;

public enum SessionValidationStatus
{
    Active,
    Blocked,
    Unknown
}

public interface IUserSessionValidationService
{
    Task<SessionValidationStatus> ValidateAsync(string userId, CancellationToken cancellationToken);
}

public class UserSessionValidationService : IUserSessionValidationService
{
    private const string DefaultServiceKey = "nexopostal-internal-service-key-2025";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<UserSessionValidationService> _logger;
    private readonly string _serviceKey;

    public UserSessionValidationService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<UserSessionValidationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var configuredServiceKey = ResolveConfigValue(configuration["InterServiceSettings:ServiceKey"]);
        _serviceKey = string.IsNullOrWhiteSpace(configuredServiceKey)
            ? DefaultServiceKey
            : configuredServiceKey;
    }

    public async Task<SessionValidationStatus> ValidateAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return SessionValidationStatus.Blocked;

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/internal/auth/session/usuarios/{Uri.EscapeDataString(userId)}/estado");
        request.Headers.TryAddWithoutValidation("X-Service-Key", _serviceKey);

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo validar estado de sesion para usuario {UserId}.", userId);
            return SessionValidationStatus.Unknown;
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogError(
                "Gateway no autorizado para validar sesion en Auth (403). Revisa InterServiceSettings:ServiceKey.");
            return SessionValidationStatus.Unknown;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Auth devolvio {StatusCode} al validar sesion de usuario {UserId}.",
                (int)response.StatusCode,
                userId);
            return SessionValidationStatus.Unknown;
        }

        SessionStatusResponse? payload;
        try
        {
            payload = await response.Content.ReadFromJsonAsync<SessionStatusResponse>(JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Respuesta invalida de Auth al validar sesion de usuario {UserId}.", userId);
            return SessionValidationStatus.Unknown;
        }

        if (payload == null)
            return SessionValidationStatus.Unknown;

        return payload.Activo ? SessionValidationStatus.Active : SessionValidationStatus.Blocked;
    }

    private static string ResolveConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return Regex.Replace(value, @"\$\{([^}]+)\}", match =>
        {
            var envVar = match.Groups[1].Value;
            return Environment.GetEnvironmentVariable(envVar) ?? match.Value;
        });
    }

    private sealed class SessionStatusResponse
    {
        public bool Activo { get; set; }
    }
}