using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexoPostal.Auth.Repositories;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Controllers;

/// <summary>
/// Endpoint interno que permite a otros microservicios comprobar si una cuenta
/// sigue activa antes de confiar en una sesión ya emitida.
/// </summary>
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

    /// <summary>
    /// Devuelve si el usuario existe y si puede seguir operando con normalidad.
    /// Solo responde cuando la petición viene firmada con la clave interna del sistema.
    /// </summary>
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

    /// <summary>
    /// Actualiza el nombre completo de un usuario. Llamado inter-servicio cuando
    /// un administrador edita el nombre de un empleado desde otro microservicio.
    /// </summary>
    [HttpPut("usuarios/{userId}/nombre")]
    public async Task<IActionResult> ActualizarNombre(string userId, [FromBody] ActualizarNombreDto dto)
    {
        if (!IsInternalServiceAuthorized())
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Service key invalida" });

        if (string.IsNullOrWhiteSpace(dto.NombreCompleto))
            return BadRequest(new { message = "NombreCompleto es obligatorio" });

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "Usuario no encontrado" });

        user.NombreCompleto = dto.NombreCompleto.Trim();
        var result = await _userRepository.UpdateAsync(user);
        if (!result.Succeeded)
            return StatusCode(500, new { message = "Error al actualizar el nombre", errors = result.Errors.Select(e => e.Description) });

        return Ok(new { userId, nombreCompleto = user.NombreCompleto });
    }

    /// <summary>
    /// Comprueba la cabecera X-Service-Key con una comparación segura para evitar
    /// filtraciones por tiempo de respuesta.
    /// </summary>
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

    /// <summary>
    /// Sustituye placeholders del estilo ${VARIABLE} por el valor real definido
    /// en las variables de entorno del despliegue.
    /// </summary>
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

    /// <summary>
    /// Compara dos claves con tiempo constante para que el fallo no revele si una
    /// petición estuvo cerca de acertar la credencial interna.
    /// </summary>
    private static bool SecureEquals(string expected, string provided)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected ?? string.Empty);
        var providedBytes = Encoding.UTF8.GetBytes(provided ?? string.Empty);

        if (expectedBytes.Length != providedBytes.Length)
            return false;

        return CryptographicOperations.FixedTimeEquals(expectedBytes, providedBytes);
    }
}

/// <summary>DTO mínimo para la sincronización de nombre desde otro microservicio.</summary>
public record ActualizarNombreDto(string NombreCompleto);