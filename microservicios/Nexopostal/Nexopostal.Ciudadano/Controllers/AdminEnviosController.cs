using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Controllers;

/// <summary>
/// Panel global de envíos para administradores. Sin reembolsos ni operaciones de pago.
/// </summary>
[ApiController]
[Route("api/admin-envios")]
[Authorize(Roles = "Admin")]
public class AdminEnviosController : ControllerBase
{
    private readonly IAdminEnviosService _service;

    public AdminEnviosController(IAdminEnviosService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminEnvioListItemDto>>> Listar(
        [FromQuery] EstadoEnvio? estado,
        [FromQuery] EstadoInterno? estadoInterno,
        [FromQuery] DateTime? fechaDesde,
        [FromQuery] DateTime? fechaHasta,
        [FromQuery] string? q,
        [FromQuery] string? cp,
        [FromQuery] bool? pagado,
        [FromQuery] int limit = 500)
    {
        var lista = await _service.ListarAsync(estado, estadoInterno, fechaDesde, fechaHasta, q, cp, pagado, limit);
        return Ok(lista);
    }

    [HttpGet("{numero}")]
    public async Task<ActionResult<AdminEnvioDetalleDto>> Obtener(string numero)
    {
        var e = await _service.ObtenerAsync(numero);
        if (e == null) return NotFound(new { mensaje = "Envío no encontrado" });
        return Ok(e);
    }

    [HttpPut("{numero}/estado")]
    public async Task<ActionResult<AdminEnvioDetalleDto>> CambiarEstado(string numero, [FromBody] CambiarEstadoEnvioDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var (e, error) = await _service.CambiarEstadoAsync(numero, dto, GetUserId());
        if (error == "Envío no encontrado") return NotFound(new { mensaje = error });
        if (error != null) return Conflict(new { mensaje = error });
        return Ok(e);
    }

    [HttpPost("{numero}/anular")]
    public async Task<ActionResult<AdminEnvioDetalleDto>> Anular(string numero, [FromBody] AccionEnvioDto dto)
    {
        var (e, error) = await _service.AnularAsync(numero, dto, GetUserId());
        if (error == "Envío no encontrado") return NotFound(new { mensaje = error });
        if (error != null) return Conflict(new { mensaje = error });
        return Ok(e);
    }

    [HttpPost("{numero}/reabrir")]
    public async Task<ActionResult<AdminEnvioDetalleDto>> Reabrir(string numero, [FromBody] AccionEnvioDto dto)
    {
        var (e, error) = await _service.ReabrirAsync(numero, dto, GetUserId());
        if (error == "Envío no encontrado") return NotFound(new { mensaje = error });
        if (error != null) return Conflict(new { mensaje = error });
        return Ok(e);
    }

    private string? GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? User.FindFirst("nameid")?.Value;
}
