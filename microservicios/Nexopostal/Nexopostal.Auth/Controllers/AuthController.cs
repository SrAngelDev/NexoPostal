using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Controllers;

/// <summary>
/// Controlador de autenticación.
/// Delega toda la lógica de negocio al servicio IAuthService (patrón repositorio).
/// </summary>
[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _config;

    public AuthController(IAuthService authService, IConfiguration config)
    {
        _authService = authService;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var result = await _authService.LoginAsync(model);
        if (result == null)
            return Unauthorized(new { error = "Credenciales incorrectas" });

        return Ok(result);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto model)
    {
        var (token, errors) = await _authService.RegisterAsync(model);

        if (token == null)
            return BadRequest(new { errors });

        return Ok(token);
    }

    /// <summary>
    /// Obtiene la información del usuario autenticado.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized();

        var userInfo = await _authService.GetUserInfoAsync(userId);
        if (userInfo == null) return NotFound();

        return Ok(userInfo);
    }

    /// <summary>
    /// Actualiza nombre, email y teléfono del usuario autenticado.
    /// </summary>
    [Authorize]
    [HttpPost("actualizar-perfil")]
    public async Task<IActionResult> ActualizarPerfil([FromBody] ActualizarUsuarioDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized();

        var (user, error) = await _authService.UpdateProfileAsync(userId, dto);
        if (user == null)
            return BadRequest(new { error });

        return Ok(user);
    }

    /// <summary>
    /// Cambia la contraseña verificando la actual.
    /// </summary>
    [Authorize]
    [HttpPost("cambiar-password")]
    public async Task<IActionResult> CambiarPassword([FromBody] CambiarPasswordDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        if (userId == null) return Unauthorized();

        var (success, error) = await _authService.ChangePasswordAsync(userId, dto);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { mensaje = "Contraseña actualizada correctamente" });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var refreshed = await _authService.RefreshTokenAsync(dto);
        if (refreshed == null)
            return Unauthorized(new { error = "Refresh token inválido o expirado" });

        return Ok(refreshed);
    }

    /// <summary>
    /// Envía un email con enlace de recuperación de contraseña.
    /// Siempre responde 200 OK para no revelar si el email está registrado.
    /// </summary>
    [HttpPost("solicitar-reset")]
    public async Task<IActionResult> SolicitarReset([FromBody] SolicitarResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var frontendUrl = _config["AppSettings:FrontendUrl"] ?? "http://localhost:4200";
        await _authService.SolicitarResetPasswordAsync(dto.Email, frontendUrl);

        return Ok(new { mensaje = "Si el email está registrado, recibirás un enlace de recuperación en breve." });
    }

    /// <summary>
    /// Restablece la contraseña usando el token del email de recuperación.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (success, error) = await _authService.ResetPasswordAsync(dto);
        if (!success)
            return BadRequest(new { error });

        return Ok(new { mensaje = "Contraseña restablecida correctamente. Ya puedes iniciar sesión." });
    }
}

