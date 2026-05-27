using Microsoft.AspNetCore.SignalR;
using Nexopostal.Reparto.Hubs;

namespace Nexopostal.Reparto.Services;

/// <summary>
/// Contrato de notificación en tiempo real para la app de reparto.
/// </summary>
public interface IRepartoNotifier
{
    /// <summary>Envía un evento SignalR al usuario repartidor identificado por su cuenta de Identity.</summary>
    Task NotificarRepartidorAsync(string identityUserId, string evento, object payload);
}

/// <summary>
/// Implementación SignalR usada para avisar al repartidor de cambios en rutas, entregas o reasignaciones.
/// </summary>
public class RepartoNotifier : IRepartoNotifier
{
    private readonly IHubContext<RepartoHub> _hub;
    private readonly ILogger<RepartoNotifier> _logger;

    public RepartoNotifier(IHubContext<RepartoHub> hub, ILogger<RepartoNotifier> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Intenta entregar un evento en tiempo real al repartidor. Si falla, lo deja trazado en logs.
    /// </summary>
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
