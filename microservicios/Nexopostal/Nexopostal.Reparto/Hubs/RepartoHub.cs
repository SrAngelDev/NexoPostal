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

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("SignalR conexión abierta: User={UserId} Conn={ConnId}",
            Context.UserIdentifier, Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR conexión cerrada: User={UserId} Conn={ConnId}",
            Context.UserIdentifier, Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
