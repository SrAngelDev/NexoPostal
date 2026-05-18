using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Services;
using System.Security.Claims;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Controlador para la gestión de asignaciones de paquetes a operarios.
/// 
/// Flujo:
///   1. El OperarioLogistico escanea un paquete → POST /api/asignaciones (crea tarea)
///   2. El Operario ve sus tareas pendientes → GET /api/asignaciones/mis-pendientes
///   3. El Operario inicia la tarea → PUT /api/asignaciones/{id}/iniciar
///   4. El Operario completa la tarea → PUT /api/asignaciones/{id}/completar
/// 
/// Los envíos urgentes aparecen siempre primero (pase VIP).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,OperarioJefe,OperarioLogistico,OperarioOficina")]
public class AsignacionesController : ControllerBase
{
    private readonly IAsignacionService _asignacionService;
    private readonly IOperarioService _operarioService;

    public AsignacionesController(IAsignacionService asignacionService, IOperarioService operarioService)
    {
        _asignacionService = asignacionService;
        _operarioService = operarioService;
    }

    /// <summary>
    /// Crea una nueva asignación de tarea.
    /// Solo el OperarioLogistico puede asignar paquetes a operarios de su CTA.
    /// </summary>
    [HttpPost]
    [HttpPost("crear")]
    [Authorize(Roles = "Admin,OperarioLogistico")]
    [ProducesResponseType(typeof(AsignacionDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AsignacionDetalleDto>> Crear([FromBody] CrearAsignacionDto dto)
    {
        var operario = await ObtenerOperarioActual();
        if (operario == null) return Forbid();

        try
        {
            var asignacion = await _asignacionService.CrearAsignacion(dto, operario.Id, operario.CentroTratamientoId);
            return CreatedAtAction(nameof(ObtenerDetalle), new { id = asignacion.Id }, asignacion);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene las tareas pendientes del operario autenticado.
    /// Ordenadas: urgentes primero, luego FIFO.
    /// </summary>
    [HttpGet("mis-pendientes")]
    [ProducesResponseType(typeof(List<AsignacionResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AsignacionResumenDto>>> ObtenerMisPendientes()
    {
        var operario = await ObtenerOperarioActual();
        if (operario == null) return Ok(new List<AsignacionResumenDto>());

        var tareas = await _asignacionService.ObtenerTareasPendientes(operario.Id);
        return Ok(tareas);
    }

    /// <summary>
    /// Obtiene las tareas en progreso del operario autenticado.
    /// </summary>
    [HttpGet("mis-en-progreso")]
    [ProducesResponseType(typeof(List<AsignacionResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AsignacionResumenDto>>> ObtenerMisEnProgreso()
    {
        var operario = await ObtenerOperarioActual();
        if (operario == null) return Ok(new List<AsignacionResumenDto>());

        var tareas = await _asignacionService.ObtenerTareasEnProgreso(operario.Id);
        return Ok(tareas);
    }

    /// <summary>
    /// Obtiene todas las asignaciones de un CTA, opcionalmente filtradas por estado.
    /// </summary>
    [HttpGet("cta/{ctaId:int}")]
    [Authorize(Roles = "Admin,OperarioJefe,OperarioLogistico")]
    [ProducesResponseType(typeof(List<AsignacionResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AsignacionResumenDto>>> ObtenerPorCta(
        int ctaId, [FromQuery] string? estado = null)
    {
        EstadoTarea? filtro = null;
        if (!string.IsNullOrEmpty(estado) && Enum.TryParse<EstadoTarea>(estado, true, out var e))
            filtro = e;

        var asignaciones = await _asignacionService.ObtenerAsignacionesCta(ctaId, filtro);
        return Ok(asignaciones);
    }

    /// <summary>
    /// Obtiene el detalle de una asignación.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AsignacionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsignacionDetalleDto>> ObtenerDetalle(int id)
    {
        var detalle = await _asignacionService.ObtenerDetalle(id);
        if (detalle == null) return NotFound(new { message = "Asignación no encontrada" });
        return Ok(detalle);
    }

    /// <summary>
    /// Inicia una tarea (Pendiente → EnProgreso).
    /// Solo el operario asignado puede iniciar su tarea.
    /// </summary>
    [HttpPut("{id:int}/iniciar")]
    [Authorize(Roles = "Admin,OperarioOficina")]
    [ProducesResponseType(typeof(AsignacionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AsignacionDetalleDto>> Iniciar(int id)
    {
        var operario = await ObtenerOperarioActual();
        if (operario == null) return Forbid();

        try
        {
            var resultado = await _asignacionService.IniciarTarea(id, operario.Id);
            if (resultado == null) return NotFound(new { message = "Asignación no encontrada" });
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Completa una tarea (EnProgreso → Completada).
    /// Solo el operario asignado puede completar su tarea.
    /// </summary>
    [HttpPut("{id:int}/completar")]
    [Authorize(Roles = "Admin,OperarioOficina")]
    [ProducesResponseType(typeof(AsignacionDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AsignacionDetalleDto>> Completar(int id)
    {
        var operario = await ObtenerOperarioActual();
        if (operario == null) return Forbid();

        try
        {
            var resultado = await _asignacionService.CompletarTarea(id, operario.Id);
            if (resultado == null) return NotFound(new { message = "Asignación no encontrada" });
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancela una tarea. Solo OperarioLogistico o Admin.
    /// </summary>
    [HttpPut("{id:int}/cancelar")]
    [Authorize(Roles = "Admin,OperarioLogistico")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancelar(int id)
    {
        var operario = await ObtenerOperarioActual();
        if (operario == null) return Forbid();

        try
        {
            var resultado = await _asignacionService.CancelarTarea(id, operario.Id);
            if (!resultado) return NotFound(new { message = "Asignación no encontrada" });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // === Helper privado ===

    private async Task<OperarioCta?> ObtenerOperarioActual()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) return null;
        return await _operarioService.ObtenerPorIdentityUserId(userId);
    }
}
