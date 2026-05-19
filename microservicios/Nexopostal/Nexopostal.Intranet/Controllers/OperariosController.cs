using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;
using System.Security.Claims;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Controlador para la gestión de operarios de CTA.
/// Permite consultar información propia, listar operarios del CTA y crear nuevos.
/// 
/// Accesible por:
///   - GET endpoints: Admin, OperarioJefe, OperarioLogistico, Operario
///   - POST/DELETE endpoints: Admin, OperarioJefe
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor,OperarioCTA,OperarioOficina")]
public class OperariosController : ControllerBase
{
    private readonly IOperarioService _operarioService;
    private readonly IClasificacionService _clasificacionService;

    public OperariosController(IOperarioService operarioService, IClasificacionService clasificacionService)
    {
        _operarioService = operarioService;
        _clasificacionService = clasificacionService;
    }

    /// <summary>
    /// Obtiene la información del CTA y rol del operario autenticado.
    /// Cada operario ve a qué CTA está asignado y cuál es su rol.
    /// Si tiene múltiples CTAs, devuelve el primero.
    /// </summary>
    [HttpGet("mi-cta")]
    [ProducesResponseType(typeof(MiCtaInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MiCtaInfoDto>> ObtenerMiCta()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Usuario no autenticado" });

        if (User.IsInRole("Admin"))
        {
            var ctas = await _clasificacionService.ObtenerTodosCtas();
            var primero = ctas.FirstOrDefault();
            if (primero == null)
                return NotFound(new { message = "No hay CTAs disponibles" });

            return Ok(new MiCtaInfoDto
            {
                OperarioId = 0,
                NombreCompleto = GetNombreUsuario() ?? "Administrador",
                CodigoEmpleado = "ADMIN",
                Rol = "Admin",
                CtaId = primero.Id,
                CtaCodigo = primero.Codigo,
                CtaNombre = primero.Nombre,
                Area = primero.Area
            });
        }

        var info = await _operarioService.ObtenerMiCtaInfo(userId);
        if (info == null)
            return NotFound(new { message = "No estás asignado a ningún CTA" });

        return Ok(info);
    }

    /// <summary>
    /// Obtiene TODOS los CTAs a los que está asignado el operario autenticado.
    /// Un operario puede trabajar en múltiples CTAs.
    /// </summary>
    [HttpGet("mis-ctas")]
    [ProducesResponseType(typeof(MisCtasInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MisCtasInfoDto>> ObtenerMisCtas()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Usuario no autenticado" });

        if (User.IsInRole("Admin"))
        {
            var ctas = await _clasificacionService.ObtenerTodosCtas();
            var infoAdmin = new MisCtasInfoDto
            {
                NombreCompleto = GetNombreUsuario() ?? "Administrador",
                CodigoEmpleado = "ADMIN",
                Rol = "Admin",
                Ctas = ctas.Select(c => new CtaAsignacionDto
                {
                    OperarioCtaId = 0,
                    CtaId = c.Id,
                    CtaCodigo = c.Codigo,
                    CtaNombre = c.Nombre,
                    Area = c.Area
                }).ToList()
            };

            return Ok(infoAdmin);
        }

        var info = await _operarioService.ObtenerMisCtasInfo(userId);
        if (info == null)
            return NotFound(new { message = "No estás asignado a ningún CTA" });

        return Ok(info);
    }

    private string? GetNombreUsuario()
    {
        return User.FindFirstValue("Nombre")
               ?? User.FindFirstValue(ClaimTypes.Name)
               ?? User.FindFirstValue("name");
    }

    /// <summary>
    /// Obtiene todos los operarios asignados a un CTA.
    /// </summary>
    [HttpGet("cta/{ctaId:int}")]
    [ProducesResponseType(typeof(List<OperarioResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OperarioResumenDto>>> ObtenerOperariosCta(int ctaId)
    {
        var operarios = await _operarioService.ObtenerOperariosCta(ctaId);
        return Ok(operarios);
    }

    /// <summary>
    /// Obtiene el detalle de un operario con sus estadísticas de tareas.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(OperarioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperarioDetalleDto>> ObtenerDetalle(int id)
    {
        var operario = await _operarioService.ObtenerDetalle(id);
        if (operario == null) return NotFound(new { message = "Operario no encontrado" });
        return Ok(operario);
    }

    /// <summary>
    /// Obtiene el detalle operativo (asignaciones CTA) por IdentityUserId para administración.
    /// </summary>
    [HttpGet("admin/identity/{identityUserId}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(AdminOperarioDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminOperarioDetalleDto>> ObtenerDetalleAdmin(string identityUserId)
    {
        var detalle = await _operarioService.ObtenerDetalleAdminPorIdentityUserId(identityUserId);
        if (detalle == null)
            return NotFound(new { message = "El usuario no tiene asignaciones CTA activas." });

        return Ok(detalle);
    }

    /// <summary>
    /// Mueve la asignación de CTA de un usuario (operación de administración).
    /// </summary>
    [HttpPut("admin/identity/{identityUserId}/cta")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActualizarCtaAdmin(string identityUserId, [FromBody] AdminActualizarCtaDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var (ok, error, conflict) = await _operarioService.ActualizarCtaAdmin(identityUserId, dto);
        if (ok)
            return NoContent();

        return conflict
            ? Conflict(new { message = error })
            : BadRequest(new { message = error });
    }

    /// <summary>
    /// Crea un nuevo operario y lo asigna a un CTA.
    /// Solo Admin y Supervisor pueden crear operarios.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor")]
    [ProducesResponseType(typeof(OperarioResumenDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OperarioResumenDto>> Crear([FromBody] CrearOperarioDto dto)
    {
        try
        {
            var operario = await _operarioService.CrearOperario(dto);
            return CreatedAtAction(nameof(ObtenerDetalle), new { id = operario.Id }, operario);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Desactiva un operario (soft delete).
    /// Solo Admin y Supervisor pueden desactivar operarios.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Supervisor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Desactivar(int id)
    {
        var resultado = await _operarioService.DesactivarOperario(id);
        if (!resultado) return NotFound(new { message = "Operario no encontrado" });
        return NoContent();
    }
}
