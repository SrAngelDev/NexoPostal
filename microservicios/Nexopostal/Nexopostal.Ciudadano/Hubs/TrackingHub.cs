using Microsoft.AspNetCore.SignalR;

namespace Nexopostal.Ciudadano.Hubs;

/// <summary>
/// Hub de SignalR para tracking en tiempo real de paquetes.
/// Permite a los clientes suscribirse a actualizaciones de estado de sus envíos.
/// 
/// Grupos:
///   - "tracking-{numeroSeguimiento}" → Clientes que siguen un envío específico
/// 
/// Eventos que emite el servidor:
///   - "EstadoActualizado" → El estado del paquete ha cambiado
///   - "UbicacionActualizada" → El paquete ha cambiado de ubicación
///   - "EntregaCompletada" → El paquete ha sido entregado
///   - "IncidenciaDetectada" → Se ha detectado una incidencia en el envío
/// 
/// Métodos que el cliente puede invocar:
///   - "SuscribirTracking(numeroSeguimiento)" → Unirse al grupo de un envío
///   - "DesuscribirTracking(numeroSeguimiento)" → Salir del grupo de un envío
/// 
/// No requiere autenticación: el tracking es público con el número de seguimiento.
/// </summary>
public class TrackingHub : Hub
{
    private readonly ILogger<TrackingHub> _logger;

    public TrackingHub(ILogger<TrackingHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Suscribe al cliente a las actualizaciones de un envío específico.
    /// El cliente recibirá notificaciones en tiempo real cuando el estado cambie.
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    public async Task SuscribirTracking(string numeroSeguimiento)
    {
        if (string.IsNullOrWhiteSpace(numeroSeguimiento))
        {
            await Clients.Caller.SendAsync("Error", new { mensaje = "Número de seguimiento requerido." });
            return;
        }

        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";
        await Groups.AddToGroupAsync(Context.ConnectionId, grupo);

        _logger.LogInformation(
            "📡 TrackingHub → Cliente suscrito a {Grupo} · ConnectionId: {ConnId}",
            grupo, Context.ConnectionId);

        await Clients.Caller.SendAsync("SuscripcionConfirmada", new
        {
            numeroSeguimiento = numeroSeguimiento.ToUpper().Trim(),
            mensaje = $"Suscrito a actualizaciones del envío {numeroSeguimiento}"
        });
    }

    /// <summary>
    /// Desuscribe al cliente de las actualizaciones de un envío.
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    public async Task DesuscribirTracking(string numeroSeguimiento)
    {
        if (string.IsNullOrWhiteSpace(numeroSeguimiento)) return;

        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, grupo);

        _logger.LogInformation(
            "📡 TrackingHub → Cliente desuscrito de {Grupo} · ConnectionId: {ConnId}",
            grupo, Context.ConnectionId);
    }

    /// <summary>
    /// Limpieza al desconectarse (SignalR limpia grupos automáticamente).
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "📡 TrackingHub → Cliente desconectado · ConnectionId: {ConnId} · Razón: {Razon}",
            Context.ConnectionId, exception?.Message ?? "Desconexión normal");

        await base.OnDisconnectedAsync(exception);
    }
}
