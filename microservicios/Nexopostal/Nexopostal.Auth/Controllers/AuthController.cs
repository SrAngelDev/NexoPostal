using System.Security.Claims;
using System.Text.RegularExpressions;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Shared.Errors;
using Nexopostal.Shared.Results;
using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Controllers;

/// <summary>
/// Controlador de autenticación con primary constructor.
/// Delega toda la lógica al servicio <see cref="IAuthService"/> y mapea
/// el <see cref="Result{T,DomainError}"/> a HTTP via <see cref="ResultExtensions"/>.
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController(IAuthService authService, IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model) =>
        (await authService.LoginAsync(model)).ToActionResult();

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model) =>
        (await authService.RegisterAsync(model)).ToActionResult();

    /// <summary>Obtiene la información del usuario autenticado.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return (await authService.GetUserInfoAsync(userId)).ToActionResult();
    }

    /// <summary>Actualiza nombre, email y teléfono del usuario autenticado.</summary>
    [Authorize]
    [HttpPost("actualizar-perfil")]
    public async Task<IActionResult> ActualizarPerfil([FromBody] ActualizarUsuarioDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        return (await authService.UpdateProfileAsync(userId, dto)).ToActionResult();
    }

    /// <summary>Cambia la contraseña verificando la actual.</summary>
    [Authorize]
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var result = await authService.ChangePasswordAsync(userId, dto);
        return result.ToActionResult(() => Ok(new { mensaje = "Contraseña actualizada correctamente" }));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto) =>
        (await authService.RefreshTokenAsync(dto)).ToActionResult();

    /// <summary>
    /// Envía un email con enlace de recuperación. Siempre responde 200 para no revelar si el email existe.
    /// </summary>
    [HttpPost("solicitar-reset")]
    public async Task<IActionResult> SolicitarReset([FromBody] SolicitarResetPasswordDto dto)
    {
        var frontendUrl = dto.FrontendUrl?.Trim().TrimEnd('/');

        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            var rawUrl = config["AppSettings:FrontendUrl"] ?? string.Empty;
            frontendUrl = Regex.Replace(
                rawUrl, @"\$\{([^}]+)\}",
                m => Environment.GetEnvironmentVariable(m.Groups[1].Value) ?? m.Value);
        }

        if (string.IsNullOrWhiteSpace(frontendUrl) || frontendUrl.Contains("${"))
            frontendUrl = "http://localhost:4200";

        await authService.SolicitarResetPasswordAsync(dto.Email, frontendUrl);
        return Ok(new { mensaje = "Si el email está registrado, recibirás un enlace de recuperación en breve." });
    }

    /// <summary>Restablece la contraseña usando el token del email de recuperación.</summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        var result = await authService.ResetPasswordAsync(dto);
        return result.ToActionResult(() =>
            Ok(new { mensaje = "Contraseña restablecida correctamente. Ya puedes iniciar sesión." }));
    }

    private string? GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
}
