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

    public AuthController(IAuthService authService)
    {
        _authService = authService;
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
    public IActionResult Refresh()
    {
        // TODO: Implementar refresh token logic
        return Ok(new { message = "Refresh token endpoint" });
    }
}

