using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Services;
using System.Security.Claims;
using MediatR;

namespace Nexopostal.Intranet.Controllers;
/// <summary>
/// Controlador para la gestión de incidencias en CTAs.
/// 
/// Solo el Supervisor puede:
///   - Reportar nuevas incidencias (paquetes dañados, extraviados, etc.)
///   - Actualizar su estado (Abierta → EnRevision → Resuelta → Cerrada)
///   - Registrar la resolución aplicada
/// 
/// Ciclo de vida: Abierta → EnRevision → Resuelta → Cerrada
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,Supervisor")]
public class IncidenciasController : ControllerBase
{
    public IncidenciasController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Reporta una nueva incidencia en el CTA del OperarioJefe autenticado.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IncidenciaDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidenciaDetalleDto>> Crear([FromBody] CrearIncidenciaDto dto)
    {
        var operario = await ObtenerOperarioActual();
        if (operario == null)
            return Forbid();
        if (operario.Rol != RolOperario.Supervisor && !User.IsInRole("Admin"))
            return Forbid();
        try
        {
            var incidencia = await _sender.Send(new Nexopostal.Intranet.Application.Incidencia.CrearIncidenciaCommand(dto, operario.Id, operario.CentroTratamientoId), HttpContext?.RequestAborted ?? CancellationToken.None);
            return CreatedAtAction(nameof(ObtenerDetalle), new { id = incidencia.Id }, incidencia);
        }
        catch (Exception ex)when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Endpoint reservado a operarios (OperarioCTA / OperarioOficina) para reportar
    /// que han escaneado un paquete fuera de sus tareas asignadas. Crea siempre una
    /// incidencia tipo <see cref = "TipoIncidencia.PaqueteFueraDeTareas"/>.
    /// </summary>
    [HttpPost("reportar-fuera-tareas")]
    [Authorize(Roles = "Admin,Supervisor,OperarioCTA,OperarioOficina")]
    [ProducesResponseType(typeof(IncidenciaDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidenciaDetalleDto>> ReportarFueraDeTareas([FromBody] ReportarFueraTareasDto dto)
    {
        var operario = await ObtenerOperarioActual();
        if (operario == null)
            return Forbid();
        if (string.IsNullOrWhiteSpace(dto.NumeroExpedicion) || string.IsNullOrWhiteSpace(dto.Motivo))
            return BadRequest(new { message = "Número de expedición y motivo obligatorios" });
        var crearDto = new CrearIncidenciaDto
        {
            NumeroExpedicion = dto.NumeroExpedicion.Trim(),
            Tipo = TipoIncidencia.PaqueteFueraDeTareas.ToString(),
            Descripcion = dto.Motivo.Trim()
        };
        try
        {
            var incidencia = await _sender.Send(new Nexopostal.Intranet.Application.Incidencia.CrearIncidenciaCommand(crearDto, operario.Id, operario.CentroTratamientoId), HttpContext?.RequestAborted ?? CancellationToken.None);
            return CreatedAtAction(nameof(ObtenerDetalle), new { id = incidencia.Id }, incidencia);
        }
        catch (Exception ex)when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene las incidencias de un CTA, opcionalmente filtradas por estado.
    /// </summary>
    [HttpGet("cta/{ctaId:int}")]
    [ProducesResponseType(typeof(List<IncidenciaResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IncidenciaResumenDto>>> ObtenerPorCta(int ctaId, [FromQuery] string? estado = null)
    {
        EstadoIncidencia? filtro = null;
        if (!string.IsNullOrEmpty(estado) && Enum.TryParse<EstadoIncidencia>(estado, true, out var e))
            filtro = e;
        var incidencias = await _sender.Send(new Nexopostal.Intranet.Application.Incidencia.ObtenerIncidenciasCtaQuery(ctaId, filtro), HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(incidencias);
    }

    /// <summary>
    /// Vista global de incidencias (solo Admin). Filtros opcionales por estado, CTA y tipo.
    /// </summary>
    [HttpGet("global")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(List<IncidenciaResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IncidenciaResumenDto>>> ObtenerGlobales([FromQuery] string? estado = null, [FromQuery] int? ctaId = null, [FromQuery] string? tipo = null)
    {
        EstadoIncidencia? filtroEstado = null;
        if (!string.IsNullOrEmpty(estado) && Enum.TryParse<EstadoIncidencia>(estado, true, out var e))
            filtroEstado = e;
        TipoIncidencia? filtroTipo = null;
        if (!string.IsNullOrEmpty(tipo) && Enum.TryParse<TipoIncidencia>(tipo, true, out var t))
            filtroTipo = t;
        var incidencias = await _sender.Send(new Nexopostal.Intranet.Application.Incidencia.ObtenerIncidenciasGlobalesQuery(filtroEstado, ctaId, filtroTipo), HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(incidencias);
    }

    /// <summary>
    /// Obtiene el detalle completo de una incidencia.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(IncidenciaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncidenciaDetalleDto>> ObtenerDetalle(int id)
    {
        var detalle = await _sender.Send(new Nexopostal.Intranet.Application.Incidencia.ObtenerDetalleQuery(id), HttpContext?.RequestAborted ?? CancellationToken.None);
        if (detalle == null)
            return NotFound(new { message = "Incidencia no encontrada" });
        return Ok(detalle);
    }

    /// <summary>
    /// Obtiene las incidencias de un paquete por su número de expedición.
    /// </summary>
    [HttpGet("paquete/{numeroExpedicion}")]
    [ProducesResponseType(typeof(List<IncidenciaResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<IncidenciaResumenDto>>> ObtenerPorPaquete(string numeroExpedicion)
    {
        var incidencias = await _sender.Send(new Nexopostal.Intranet.Application.Incidencia.ObtenerIncidenciasPaqueteQuery(numeroExpedicion), HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(incidencias);
    }

    /// <summary>
    /// Actualiza el estado de una incidencia.
    /// Si se marca como Resuelta, es obligatorio incluir la resolución.
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(IncidenciaDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncidenciaDetalleDto>> Actualizar(int id, [FromBody] ActualizarIncidenciaDto dto)
    {
        try
        {
            var resultado = await _sender.Send(new Nexopostal.Intranet.Application.Incidencia.ActualizarIncidenciaCommand(id, dto), HttpContext?.RequestAborted ?? CancellationToken.None);
            if (resultado == null)
                return NotFound(new { message = "Incidencia no encontrada" });
            return Ok(resultado);
        }
        catch (Exception ex)when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // === Helper privado ===
    private async Task<OperarioCta?> ObtenerOperarioActual()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return null;
        return await _sender.Send(new Nexopostal.Intranet.Application.Operario.ObtenerPorIdentityUserIdQuery(userId), HttpContext?.RequestAborted ?? CancellationToken.None);
    }

    private readonly ISender _sender;
}
