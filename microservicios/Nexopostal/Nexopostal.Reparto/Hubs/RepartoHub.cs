using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Nexopostal.Reparto.Hubs;

/// <summary>
/// Hub de SignalR para notificaciones en tiempo real al driver-app.
/// El UserIdentifier por defecto utiliza ClaimTypes.NameIdentifier (sub del JWT),
/// por lo que <c>Clients.User(identityUserId)</c> entrega los mensajes a todas
/// las conexiones del repartidor autenticado.
/// </summary>
[Authorize]
public class RepartoHub : Hub
{
    private readonly ILogger<RepartoHub> _logger;

    public RepartoHub(ILogger<RepartoHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR conexión abierta: User={UserId} Conn={ConnId}",
            Context.UserIdentifier, Context.ConnectionId);

        // Los JefeReparto se suscriben automáticamente al grupo "jefes-reparto"
        // para recibir avisos de nuevos paquetes disponibles en bandeja.
        if (Context.User?.IsInRole("JefeReparto") == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "jefes-reparto");
            _logger.LogInformation("SignalR: User={UserId} añadido al grupo jefes-reparto",
                Context.UserIdentifier);
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR conexión cerrada: User={UserId} Conn={ConnId}",
            Context.UserIdentifier, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
