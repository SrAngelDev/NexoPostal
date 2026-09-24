using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Services;
using MediatR;

namespace Nexopostal.Ciudadano.Controllers;
/// <summary>
/// Panel global de envíos para administradores. Sin reembolsos ni operaciones de pago.
/// </summary>
[ApiController]
[Route("api/admin-envios")]
[Authorize(Roles = "Admin")]
public class AdminEnviosController : ControllerBase
{
    public AdminEnviosController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminEnvioListItemDto>>> Listar([FromQuery] EstadoEnvio? estado, [FromQuery] EstadoInterno? estadoInterno, [FromQuery] DateTime? fechaDesde, [FromQuery] DateTime? fechaHasta, [FromQuery] string? q, [FromQuery] string? cp, [FromQuery] bool? pagado, [FromQuery] int limit = 500)
    {
        var lista = await _sender.Send(new Nexopostal.Ciudadano.Application.AdminEnvios.ListarQuery(estado, estadoInterno, fechaDesde, fechaHasta, q, cp, pagado, limit), HttpContext?.RequestAborted ?? CancellationToken.None);
        return Ok(lista);
    }

    [HttpGet("{numero}")]
    public async Task<ActionResult<AdminEnvioDetalleDto>> Obtener(string numero)
    {
        var e = await _sender.Send(new Nexopostal.Ciudadano.Application.AdminEnvios.ObtenerQuery(numero), HttpContext?.RequestAborted ?? CancellationToken.None);
        if (e == null)
            return NotFound(new { mensaje = "Envío no encontrado" });
        return Ok(e);
    }

    [HttpPut("{numero}/estado")]
    public async Task<ActionResult<AdminEnvioDetalleDto>> CambiarEstado(string numero, [FromBody] CambiarEstadoEnvioDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        var(e, error) = await _sender.Send(new Nexopostal.Ciudadano.Application.AdminEnvios.CambiarEstadoCommand(numero, dto, GetUserId()), HttpContext?.RequestAborted ?? CancellationToken.None);
        if (error == "Envío no encontrado")
            return NotFound(new { mensaje = error });
        if (error != null)
            return Conflict(new { mensaje = error });
        return Ok(e);
    }

    [HttpPost("{numero}/anular")]
    public async Task<ActionResult<AdminEnvioDetalleDto>> Anular(string numero, [FromBody] AccionEnvioDto dto)
    {
        var(e, error) = await _sender.Send(new Nexopostal.Ciudadano.Application.AdminEnvios.AnularCommand(numero, dto, GetUserId()), HttpContext?.RequestAborted ?? CancellationToken.None);
        if (error == "Envío no encontrado")
            return NotFound(new { mensaje = error });
        if (error != null)
            return Conflict(new { mensaje = error });
        return Ok(e);
    }

    [HttpPost("{numero}/reabrir")]
    public async Task<ActionResult<AdminEnvioDetalleDto>> Reabrir(string numero, [FromBody] AccionEnvioDto dto)
    {
        var(e, error) = await _sender.Send(new Nexopostal.Ciudadano.Application.AdminEnvios.ReabrirCommand(numero, dto, GetUserId()), HttpContext?.RequestAborted ?? CancellationToken.None);
        if (error == "Envío no encontrado")
            return NotFound(new { mensaje = error });
        if (error != null)
            return Conflict(new { mensaje = error });
        return Ok(e);
    }

    private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
    private readonly ISender _sender;
}
