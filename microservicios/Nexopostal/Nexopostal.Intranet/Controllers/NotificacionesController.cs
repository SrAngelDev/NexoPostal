using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Controllers;

/// <summary>
/// Endpoints administrativos para enviar notificaciones broadcast vía SignalR.
/// </summary>
[ApiController]
[Route("api/notificaciones")]
[Authorize(Roles = "Admin")]
public class NotificacionesController : ControllerBase
{
    private readonly IBroadcastService _broadcast;

    public NotificacionesController(IBroadcastService broadcast)
    {
        _broadcast = broadcast;
    }

    /// <summary>Envía un mensaje broadcast por el hub.</summary>
    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastRequest req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.Titulo) || string.IsNullOrWhiteSpace(req.Mensaje))
            return BadRequest(new { message = "Título y mensaje son obligatorios." });

        try
        {
            await _broadcast.BroadcastAsync(req);
            return Ok(new { ok = true, fechaUtc = DateTime.UtcNow });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
