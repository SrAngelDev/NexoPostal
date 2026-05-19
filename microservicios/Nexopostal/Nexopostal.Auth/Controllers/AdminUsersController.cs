using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Controllers;

[ApiController]
[Route("api/admin-usuarios")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    /// <summary>Lista todos los usuarios con filtros opcionales.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? rol,
        [FromQuery] bool? bloqueado,
        [FromQuery] string? q)
    {
        Rol? rolEnum = null;
        if (!string.IsNullOrWhiteSpace(rol) && Enum.TryParse<Rol>(rol, out var parsed))
            rolEnum = parsed;

        var usuarios = await _adminUserService.ListarUsuariosAsync(rolEnum, bloqueado, q);
        return Ok(usuarios);
    }

    /// <summary>Obtiene el detalle de un usuario por ID.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Detalle(string id)
    {
        var usuario = await _adminUserService.ObtenerDetalleAsync(id);
        if (usuario == null)
            return NotFound(new { message = "Usuario no encontrado" });
        return Ok(usuario);
    }

    /// <summary>Cambia el rol de un usuario. El admin no puede cambiar su propio rol.</summary>
    [HttpPut("{id}/rol")]
    public async Task<IActionResult> CambiarRol(string id, [FromBody] AdminCambiarRolDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var (ok, error) = await _adminUserService.CambiarRolAsync(id, dto.NuevoRol, adminId);

        return ok ? NoContent() : BadRequest(new { message = error });
    }

    /// <summary>Bloquea el acceso de un usuario. El admin no puede bloquearse a sí mismo.</summary>
    [HttpPut("{id}/bloquear")]
    public async Task<IActionResult> Bloquear(string id)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var (ok, error) = await _adminUserService.BloquearAsync(id, adminId);

        return ok ? NoContent() : BadRequest(new { message = error });
    }

    /// <summary>Desbloquea el acceso de un usuario.</summary>
    [HttpPut("{id}/desbloquear")]
    public async Task<IActionResult> Desbloquear(string id)
    {
        var (ok, error) = await _adminUserService.DesbloquearAsync(id);
        return ok ? NoContent() : BadRequest(new { message = error });
    }

    /// <summary>Restablece la contraseña de un usuario directamente (sin email de reset).</summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (ok, error) = await _adminUserService.ResetPasswordAsync(id, dto.NuevaPassword);
        return ok ? NoContent() : BadRequest(new { message = error });
    }

    /// <summary>Crea un nuevo empleado interno. No permite rol Cliente.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] AdminCrearEmpleadoDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (user, error) = await _adminUserService.CrearEmpleadoAsync(dto);
        if (user == null)
            return BadRequest(new { message = error });

        return CreatedAtAction(nameof(Detalle), new { id = user.Id }, user);
    }
}
