using Microsoft.AspNetCore.SignalR;
using Nexopostal.Reparto.Hubs;

namespace Nexopostal.Reparto.Services;

public interface IRepartoNotifier
{
    Task NotificarRepartidorAsync(string identityUserId, string evento, object payload);
}

public class RepartoNotifier : IRepartoNotifier
{
    private readonly IHubContext<RepartoHub> _hub;
    private readonly ILogger<RepartoNotifier> _logger;

    public RepartoNotifier(IHubContext<RepartoHub> hub, ILogger<RepartoNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task NotificarRepartidorAsync(string identityUserId, string evento, object payload)
    {
        if (string.IsNullOrWhiteSpace(identityUserId)) return;
        try
        {
            await _hub.Clients.User(identityUserId).SendAsync(evento, payload);
            _logger.LogInformation("SignalR enviado evento={Evento} user={User}", evento, identityUserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enviando notificación SignalR evento={Evento} user={User}", evento, identityUserId);
        }
    }
}
