using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Shared.Results;
using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Services;
using MediatR;

namespace NexoPostal.Auth.Controllers;
[ApiController]
[Route("api/admin-usuarios")]
[Authorize(Roles = "Admin")]
public class AdminUsersController(ISender sender) : ControllerBase
{
    /// <summary>Lista todos los usuarios con filtros opcionales.</summary>
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? rol, [FromQuery] bool? bloqueado, [FromQuery] string? q, [FromQuery] bool incluirEliminados = false)
    {
        Rol? rolEnum = null;
        if (!string.IsNullOrWhiteSpace(rol) && Enum.TryParse<Rol>(rol, out var parsed))
            rolEnum = parsed;
        var usuarios = await sender.Send(new NexoPostal.Auth.Application.AdminUser.ListarUsuariosQuery(rolEnum, bloqueado, q, incluirEliminados), HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(usuarios);
    }

    /// <summary>Obtiene el detalle de un usuario por ID.</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> Detalle(string id) => (await sender.Send(new NexoPostal.Auth.Application.AdminUser.ObtenerDetalleQuery(id), HttpContext?.RequestAborted ?? CancellationToken.None)).ToActionResult();
    /// <summary>Cambia el rol de un usuario. El admin no puede cambiar su propio rol.</summary>
    [HttpPut("{id}/rol")]
    public async Task<IActionResult> CambiarRol(string id, [FromBody] AdminCambiarRolDto dto) => (await sender.Send(new NexoPostal.Auth.Application.AdminUser.CambiarRolCommand(id, dto.NuevoRol, GetAdminId()), HttpContext?.RequestAborted ?? CancellationToken.None)).ToActionResult();
    /// <summary>Bloquea el acceso de un usuario. El admin no puede bloquearse a sí mismo.</summary>
    [HttpPut("{id}/bloquear")]
    public async Task<IActionResult> Bloquear(string id) => (await sender.Send(new NexoPostal.Auth.Application.AdminUser.BloquearCommand(id, GetAdminId()), HttpContext?.RequestAborted ?? CancellationToken.None)).ToActionResult();
    /// <summary>Desbloquea el acceso de un usuario.</summary>
    [HttpPut("{id}/desbloquear")]
    public async Task<IActionResult> Desbloquear(string id) => (await sender.Send(new NexoPostal.Auth.Application.AdminUser.DesbloquearCommand(id), HttpContext?.RequestAborted ?? CancellationToken.None)).ToActionResult();
    /// <summary>Restablece la contraseña de un usuario directamente (sin email de reset).</summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordDto dto) => (await sender.Send(new NexoPostal.Auth.Application.AdminUser.ResetPasswordCommand(id, dto.NuevaPassword), HttpContext?.RequestAborted ?? CancellationToken.None)).ToActionResult();
    /// <summary>Crea un nuevo empleado interno. No permite rol Cliente.</summary>
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] AdminCrearEmpleadoDto dto) => (await sender.Send(new NexoPostal.Auth.Application.AdminUser.CrearEmpleadoCommand(dto), HttpContext?.RequestAborted ?? CancellationToken.None)).ToActionResult(user => CreatedAtAction(nameof(Detalle), new { id = user.Id }, user));
    /// <summary>Edita los datos básicos de un empleado.</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Editar(string id, [FromBody] AdminEditarEmpleadoDto dto) => (await sender.Send(new NexoPostal.Auth.Application.AdminUser.EditarEmpleadoCommand(id, dto, GetAdminId()), HttpContext?.RequestAborted ?? CancellationToken.None)).ToActionResult();
    /// <summary>Borrado lógico del usuario.</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(string id) => (await sender.Send(new NexoPostal.Auth.Application.AdminUser.EliminarCommand(id, GetAdminId()), HttpContext?.RequestAborted ?? CancellationToken.None)).ToActionResult();
    /// <summary>Revierte el borrado lógico.</summary>
    [HttpPost("{id}/restaurar")]
    public async Task<IActionResult> Restaurar(string id) => (await sender.Send(new NexoPostal.Auth.Application.AdminUser.RestaurarCommand(id), HttpContext?.RequestAborted ?? CancellationToken.None)).ToActionResult();
    private string GetAdminId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
}
