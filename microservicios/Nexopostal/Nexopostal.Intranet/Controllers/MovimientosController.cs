using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Controlador para la gestión de movimientos de paquetes entre CTAs (rutas troncales).
/// 
/// Los movimientos representan el transporte de larga distancia:
///   - Terrestre: camiones nocturnos entre áreas zonales
///   - Aéreo: avión para destinos insulares y urgentes
///   - Marítimo: barco para Canarias, Baleares, Ceuta, Melilla
/// 
/// Flujo: Programado → EnTransito (despacho) → Recibido (llegada)
/// 
/// Accesible por: Admin, OperarioJefe, OperarioLogistico
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,OperarioJefe,OperarioLogistico")]
public class MovimientosController : ControllerBase
{
    private readonly IMovimientoService _movimientoService;

    public MovimientosController(IMovimientoService movimientoService)
    {
        _movimientoService = movimientoService;
    }

    /// <summary>
    /// Crea un nuevo movimiento entre CTAs.
    /// El tipo de transporte se determina automáticamente según las reglas logísticas
    /// (insular → aéreo/marítimo, urgente larga distancia → aéreo, etc.).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(MovimientoDetalleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoDetalleDto>> Crear([FromBody] CrearMovimientoDto dto)
    {
        try
        {
            var movimiento = await _movimientoService.CrearMovimiento(dto);
            return CreatedAtAction(nameof(ObtenerDetalle), new { id = movimiento.Id }, movimiento);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Obtiene los movimientos de un CTA (como origen o destino).
    /// Puede filtrarse por estado: Programado, EnTransito, Recibido, Cancelado.
    /// </summary>
    [HttpGet("cta/{ctaId:int}")]
    [ProducesResponseType(typeof(List<MovimientoResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MovimientoResumenDto>>> ObtenerPorCta(
        int ctaId, [FromQuery] string? estado = null)
    {
        EstadoMovimiento? filtro = null;
        if (!string.IsNullOrEmpty(estado) && Enum.TryParse<EstadoMovimiento>(estado, true, out var e))
            filtro = e;

        var movimientos = await _movimientoService.ObtenerMovimientosCta(ctaId, filtro);
        return Ok(movimientos);
    }

    /// <summary>
    /// Obtiene el detalle completo de un movimiento.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(MovimientoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MovimientoDetalleDto>> ObtenerDetalle(int id)
    {
        var detalle = await _movimientoService.ObtenerDetalle(id);
        if (detalle == null) return NotFound(new { message = "Movimiento no encontrado" });
        return Ok(detalle);
    }

    /// <summary>
    /// Obtiene el historial de movimientos de un paquete por su número de expedición.
    /// </summary>
    [HttpGet("paquete/{numeroExpedicion}")]
    [ProducesResponseType(typeof(List<MovimientoResumenDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MovimientoResumenDto>>> ObtenerHistorial(string numeroExpedicion)
    {
        var historial = await _movimientoService.ObtenerHistorialPaquete(numeroExpedicion);
        return Ok(historial);
    }

    /// <summary>
    /// Despacha un movimiento (Programado → EnTransito).
    /// Registra la fecha de salida del CTA de origen.
    /// </summary>
    [HttpPut("{id:int}/despachar")]
    [ProducesResponseType(typeof(MovimientoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoDetalleDto>> Despachar(int id)
    {
        try
        {
            var resultado = await _movimientoService.DespacharMovimiento(id);
            if (resultado == null) return NotFound(new { message = "Movimiento no encontrado" });
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Registra la recepción de un movimiento (EnTransito → Recibido).
    /// Registra la fecha de llegada al CTA de destino.
    /// </summary>
    [HttpPut("{id:int}/recibir")]
    [ProducesResponseType(typeof(MovimientoDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MovimientoDetalleDto>> Recibir(int id)
    {
        try
        {
            var resultado = await _movimientoService.RecibirMovimiento(id);
            if (resultado == null) return NotFound(new { message = "Movimiento no encontrado" });
            return Ok(resultado);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cancela un movimiento (solo si no ha sido recibido aún).
    /// </summary>
    [HttpPut("{id:int}/cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancelar(int id)
    {
        try
        {
            var resultado = await _movimientoService.CancelarMovimiento(id);
            if (!resultado) return NotFound(new { message = "Movimiento no encontrado" });
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
