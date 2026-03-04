using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Controlador para la trazabilidad de paquetes (historial de estados).
/// 
/// Proporciona dos vistas:
///   - Pública: tracking simplificado para clientes (por número de seguimiento)
///   - Interna: auditoría completa para operarios (por número de expedición)
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HistorialController : ControllerBase
{
    private readonly IHistorialService _historialService;

    public HistorialController(IHistorialService historialService)
    {
        _historialService = historialService;
    }

    /// <summary>
    /// Obtiene el historial público de un paquete por número de seguimiento.
    /// Solo incluye eventos visibles para el cliente (barra de progreso).
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    [HttpGet("tracking/{numeroSeguimiento}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<HistorialEventoDto>), 200)]
    public async Task<IActionResult> ObtenerTrackingPublico(string numeroSeguimiento)
    {
        var historial = await _historialService.ObtenerHistorialPublico(numeroSeguimiento);
        return Ok(historial);
    }

    /// <summary>
    /// Obtiene el historial completo interno de un paquete por número de expedición.
    /// Incluye todos los eventos con datos de auditoría (operario, observaciones, etc.).
    /// </summary>
    /// <param name="numeroExpedicion">Número de expedición interno (NXI-...)</param>
    [HttpGet("interno/{numeroExpedicion}")]
    [Authorize(Roles = "Admin,OperarioJefe,OperarioLogistico,OperarioOficina")]
    [ProducesResponseType(typeof(List<HistorialEventoInternoDto>), 200)]
    public async Task<IActionResult> ObtenerHistorialInterno(string numeroExpedicion)
    {
        var historial = await _historialService.ObtenerHistorialInterno(numeroExpedicion);
        return Ok(historial);
    }

    /// <summary>
    /// Obtiene el último evento registrado de un paquete.
    /// </summary>
    /// <param name="numeroExpedicion">Número de expedición interno (NXI-...)</param>
    [HttpGet("ultimo/{numeroExpedicion}")]
    [Authorize(Roles = "Admin,OperarioJefe,OperarioLogistico,OperarioOficina")]
    [ProducesResponseType(typeof(HistorialEventoInternoDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ObtenerUltimoEvento(string numeroExpedicion)
    {
        var evento = await _historialService.ObtenerUltimoEvento(numeroExpedicion);
        if (evento == null) return NotFound(new { mensaje = "No se encontraron eventos para este paquete." });
        return Ok(evento);
    }

    /// <summary>
    /// Registra manualmente un evento de trazabilidad.
    /// Normalmente los eventos se registran automáticamente al cambiar estados,
    /// pero este endpoint permite registros manuales por parte de operarios.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,OperarioJefe,OperarioLogistico")]
    [ProducesResponseType(typeof(HistorialEventoInternoDto), 201)]
    public async Task<IActionResult> RegistrarEvento([FromBody] CrearHistorialEventoDto dto)
    {
        var evento = await _historialService.RegistrarEvento(dto);
        return CreatedAtAction(nameof(ObtenerHistorialInterno),
            new { numeroExpedicion = dto.NumeroExpedicion }, evento);
    }
}
