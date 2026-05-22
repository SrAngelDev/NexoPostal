using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.Hubs;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Envío de notificaciones broadcast (Admin → todos / grupo CTA / rol).
/// Se monta sobre el IntranetHub. Emite siempre el evento "NotificacionBroadcast"
/// con un payload genérico que el front interpreta.
/// </summary>
public interface IBroadcastService
{
    Task BroadcastAsync(BroadcastRequest req);
}

public class BroadcastRequest
{
    /// <summary>Título del mensaje.</summary>
    public string Titulo { get; set; } = string.Empty;
    /// <summary>Cuerpo / mensaje.</summary>
    public string Mensaje { get; set; } = string.Empty;
    /// <summary>info | warning | error | success.</summary>
    public string Tipo { get; set; } = "info";
    /// <summary>
    /// Alcance: all | admin | cta | cta-rol.
    /// - all       → Todos los conectados (Clients.All)
    /// - admin     → Solo grupo "admin"
    /// - cta       → cta-{CtaId} (todos los operarios del CTA)
    /// - cta-rol   → cta-{CtaId}-{Rol} (Rol = cta | supervisor | operarios)
    /// </summary>
    public string Alcance { get; set; } = "all";
    public int? CtaId { get; set; }
    public string? Rol { get; set; }
}

public class BroadcastService : IBroadcastService
{
    private readonly IHubContext<IntranetHub> _hub;
    private readonly ILogger<BroadcastService> _logger;

    public BroadcastService(IHubContext<IntranetHub> hub, ILogger<BroadcastService> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task BroadcastAsync(BroadcastRequest req)
    {
        var payload = new
        {
            tipo = string.IsNullOrWhiteSpace(req.Tipo) ? "info" : req.Tipo,
            titulo = req.Titulo,
            mensaje = req.Mensaje,
            fechaUtc = DateTime.UtcNow,
            alcance = req.Alcance,
            ctaId = req.CtaId,
            rol = req.Rol
        };

        switch ((req.Alcance ?? "all").ToLowerInvariant())
        {
            case "admin":
                await _hub.Clients.Group("admin").SendAsync("NotificacionBroadcast", payload);
                _logger.LogInformation("Broadcast a admin · {Titulo}", req.Titulo);
                break;

            case "cta":
                if (req.CtaId is null || req.CtaId <= 0)
                    throw new ArgumentException("Se requiere CtaId para alcance 'cta'.");
                await _hub.Clients.Group($"cta-{req.CtaId}").SendAsync("NotificacionBroadcast", payload);
                _logger.LogInformation("Broadcast a cta-{CtaId} · {Titulo}", req.CtaId, req.Titulo);
                break;

            case "cta-rol":
                if (req.CtaId is null || req.CtaId <= 0 || string.IsNullOrWhiteSpace(req.Rol))
                    throw new ArgumentException("Se requieren CtaId y Rol para alcance 'cta-rol'.");
                var rolNorm = req.Rol!.Trim().ToLowerInvariant();
                if (rolNorm is not ("cta" or "supervisor" or "operarios"))
                    throw new ArgumentException("Rol debe ser 'cta', 'supervisor' u 'operarios'.");
                await _hub.Clients.Group($"cta-{req.CtaId}-{rolNorm}").SendAsync("NotificacionBroadcast", payload);
                _logger.LogInformation("Broadcast a cta-{CtaId}-{Rol} · {Titulo}", req.CtaId, rolNorm, req.Titulo);
                break;

            case "all":
            default:
                await _hub.Clients.All.SendAsync("NotificacionBroadcast", payload);
                _logger.LogInformation("Broadcast a todos · {Titulo}", req.Titulo);
                break;
        }
    }
}
