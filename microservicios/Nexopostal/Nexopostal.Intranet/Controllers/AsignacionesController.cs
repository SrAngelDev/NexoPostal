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
///   1. El OperarioCTA escanea un paquete → POST /api/asignaciones (crea tarea)
///   2. El OperarioOficina ve sus tareas pendientes → GET /api/asignaciones/mis-pendientes
///   3. El OperarioOficina inicia la tarea → PUT /api/asignaciones/{id}/iniciar
///   4. El OperarioOficina completa la tarea → PUT /api/asignaciones/{id}/completar
/// 
/// Los envíos urgentes aparecen siempre primero (pase VIP).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor,OperarioCTA,OperarioOficina")]
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
    /// El OperarioCTA asigna paquetes a operarios de su CTA.
    /// El OperarioOficina puede auto-asignarse tareas de la cola de su oficina.
    /// </summary>
    [HttpPost]
    [HttpPost("crear")]
    [Authorize(Roles = "Admin,OperarioCTA,OperarioOficina")]
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
    /// Busca una tarea (pendiente o en progreso) del operario por número de expedición.
    /// Si no la encuentra devuelve 404 — el frontend debe ofrecer crear incidencia
    /// "PaqueteFueraDeTareas".
    /// </summary>
    [HttpGet("buscar")]
    [ProducesResponseType(typeof(AsignacionResumenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsignacionResumenDto>> BuscarEnMisTareas([FromQuery] string codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo))
            return BadRequest(new { message = "Código requerido" });

        var operario = await ObtenerOperarioActual();
        if (operario == null) return Forbid();

        var resultado = await _asignacionService.BuscarEnMisTareasAsync(operario.Id, codigo);
        if (resultado == null) return NotFound(new { message = "Paquete fuera de tus tareas" });
        return Ok(resultado);
    }

    /// <summary>
    /// Obtiene todas las asignaciones de un CTA, opcionalmente filtradas por estado.
    /// </summary>
    [HttpGet("cta/{ctaId:int}")]
    [Authorize(Roles = "Admin,Supervisor,OperarioCTA")]
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
    [Authorize(Roles = "Admin,Supervisor")]
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
    [Authorize(Roles = "Admin,Supervisor")]
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
    /// Cancela una tarea. Solo OperarioCTA o Admin.
    /// </summary>
    [HttpPut("{id:int}/cancelar")]
    [Authorize(Roles = "Admin,Supervisor")]
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
