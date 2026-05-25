using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Shared.Results;
using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Controllers;

[ApiController]
[Route("api/admin-usuarios")]
[Authorize(Roles = "Admin")]
public class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    /// <summary>Lista todos los usuarios con filtros opcionales.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? rol,
        [FromQuery] bool? bloqueado,
        [FromQuery] string? q,
        [FromQuery] bool incluirEliminados = false)
    {
        Rol? rolEnum = null;
        if (!string.IsNullOrWhiteSpace(rol) && Enum.TryParse<Rol>(rol, out var parsed))
            rolEnum = parsed;

        var usuarios = await adminUserService.ListarUsuariosAsync(rolEnum, bloqueado, q, incluirEliminados);
        return Ok(usuarios);
    }

    /// <summary>Obtiene el detalle de un usuario por ID.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Detalle(string id) =>
        (await adminUserService.ObtenerDetalleAsync(id)).ToActionResult();

    /// <summary>Cambia el rol de un usuario. El admin no puede cambiar su propio rol.</summary>
    [HttpPut("{id}/rol")]
    public async Task<IActionResult> CambiarRol(string id, [FromBody] AdminCambiarRolDto dto) =>
        (await adminUserService.CambiarRolAsync(id, dto.NuevoRol, GetAdminId())).ToActionResult();

    /// <summary>Bloquea el acceso de un usuario. El admin no puede bloquearse a sí mismo.</summary>
    [HttpPut("{id}/bloquear")]
    public async Task<IActionResult> Bloquear(string id) =>
        (await adminUserService.BloquearAsync(id, GetAdminId())).ToActionResult();

    /// <summary>Desbloquea el acceso de un usuario.</summary>
    [HttpPut("{id}/desbloquear")]
    public async Task<IActionResult> Desbloquear(string id) =>
        (await adminUserService.DesbloquearAsync(id)).ToActionResult();

    /// <summary>Restablece la contraseña de un usuario directamente (sin email de reset).</summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordDto dto) =>
        (await adminUserService.ResetPasswordAsync(id, dto.NuevaPassword)).ToActionResult();

    /// <summary>Crea un nuevo empleado interno. No permite rol Cliente.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] AdminCrearEmpleadoDto dto) =>
        (await adminUserService.CrearEmpleadoAsync(dto))
            .ToActionResult(user => CreatedAtAction(nameof(Detalle), new { id = user.Id }, user));

    /// <summary>Edita los datos básicos de un empleado.</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(string id, [FromBody] AdminEditarEmpleadoDto dto) =>
        (await adminUserService.EditarEmpleadoAsync(id, dto, GetAdminId())).ToActionResult();

    /// <summary>Borrado lógico del usuario.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id) =>
        (await adminUserService.EliminarAsync(id, GetAdminId())).ToActionResult();

    /// <summary>Revierte el borrado lógico.</summary>
    [HttpPost("{id}/restaurar")]
    public async Task<IActionResult> Restaurar(string id) =>
        (await adminUserService.RestaurarAsync(id)).ToActionResult();

    private string GetAdminId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
