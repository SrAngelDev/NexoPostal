using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexoPostal.Auth.Repositories;

namespace NexoPostal.Auth.Controllers;

[ApiController]
[Route("api/internal/auth/session")]
[AllowAnonymous]
public class InternalSessionController : ControllerBase
{
    private const string DefaultServiceKey = "nexopostal-internal-service-key-2025";

    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;

    public InternalSessionController(IUserRepository userRepository, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _configuration = configuration;
    }

    [HttpGet("usuarios/{userId}/estado")]
    public async Task<IActionResult> ObtenerEstadoUsuario(string userId)
    {
        if (!IsInternalServiceAuthorized())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Service key invalida" });

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Ok(new
            {
                activo = false,
                bloqueado = true,
                existe = false
            });
        }

        var bloqueado = await _userRepository.IsLockedOutAsync(user);

        return Ok(new
        {
            activo = !bloqueado,
            bloqueado,
            existe = true
        });
    }

    private bool IsInternalServiceAuthorized()
    {
        var expectedKey = ResolveConfigValue(_configuration["InterServiceSettings:ServiceKey"]);
        if (string.IsNullOrWhiteSpace(expectedKey))
            expectedKey = DefaultServiceKey;

        var providedKey = Request.Headers["X-Service-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
            return false;

        return SecureEquals(expectedKey, providedKey);
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

    private static bool SecureEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected ?? string.Empty);
        var providedBytes = Encoding.UTF8.GetBytes(provided ?? string.Empty);

        if (expectedBytes.Length != providedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}