using Microsoft.AspNetCore.SignalR;
using Nexopostal.Ciudadano.Hubs;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio para enviar notificaciones de tracking en tiempo real a los clientes.
/// Utiliza SignalR para push de actualizaciones de estado de envíos.
/// 
/// Los clientes se suscriben a un número de seguimiento específico a través del
/// TrackingHub y reciben actualizaciones automáticas cuando:
///   - El estado del paquete cambia (Admitido → EnTransito → Entregado)
///   - El paquete cambia de ubicación (Oficina → CTA → CTA → Oficina)
///   - Se detecta una incidencia
///   - El paquete es entregado
/// </summary>
public interface ITrackingNotificacionService
{
    /// <summary>
    /// Notifica a todos los clientes suscritos que el estado de un envío ha cambiado.
    /// </summary>
    Task NotificarCambioEstado(string numeroSeguimiento, string estadoPublico,
        string estadoInterno, string descripcion, string? ubicacion = null);

    /// <summary>
    /// Notifica que el paquete ha cambiado de ubicación física.
    /// </summary>
    Task NotificarCambioUbicacion(string numeroSeguimiento, string ubicacion,
        string tipoUbicacion, string descripcion);

    /// <summary>
    /// Notifica que el paquete ha sido entregado exitosamente.
    /// </summary>
    Task NotificarEntregaCompletada(string numeroSeguimiento, string tipoEntrega,
        string descripcion);

    /// <summary>
    /// Notifica que se ha detectado una incidencia en el envío.
    /// </summary>
    Task NotificarIncidencia(string numeroSeguimiento, string tipoIncidencia,
        string descripcion);
}

public class TrackingNotificacionService : ITrackingNotificacionService
{
    private readonly IHubContext<TrackingHub> _hubContext;
    private readonly ILogger<TrackingNotificacionService> _logger;

    public TrackingNotificacionService(
        IHubContext<TrackingHub> hubContext,
        ILogger<TrackingNotificacionService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotificarCambioEstado(string numeroSeguimiento, string estadoPublico,
        string estadoInterno, string descripcion, string? ubicacion = null)
    {
        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";

        await _hubContext.Clients.Group(grupo).SendAsync("EstadoActualizado", new
        {
            numeroSeguimiento,
            estadoPublico,
            estadoInterno,
            descripcion,
            ubicacion,
            fechaHora = DateTime.UtcNow
        });

        _logger.LogInformation(
            "📡 Tracking → EstadoActualizado · {Seguimiento}: {EstadoPrevio} → {Estado}",
            numeroSeguimiento, estadoInterno, estadoPublico);
    }

    /// <inheritdoc />
    public async Task NotificarCambioUbicacion(string numeroSeguimiento, string ubicacion,
        string tipoUbicacion, string descripcion)
    {
        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";

        await _hubContext.Clients.Group(grupo).SendAsync("UbicacionActualizada", new
        {
            numeroSeguimiento,
            ubicacion,
            tipoUbicacion,
            descripcion,
            fechaHora = DateTime.UtcNow
        });

        _logger.LogInformation(
            "📡 Tracking → UbicacionActualizada · {Seguimiento}: {Ubicacion} ({Tipo})",
            numeroSeguimiento, ubicacion, tipoUbicacion);
    }

    /// <inheritdoc />
    public async Task NotificarEntregaCompletada(string numeroSeguimiento, string tipoEntrega,
        string descripcion)
    {
        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";

        await _hubContext.Clients.Group(grupo).SendAsync("EntregaCompletada", new
        {
            numeroSeguimiento,
            tipoEntrega,
            descripcion,
            fechaHora = DateTime.UtcNow
        });

        _logger.LogInformation(
            "📡 Tracking → EntregaCompletada · {Seguimiento}: {Tipo}",
            numeroSeguimiento, tipoEntrega);
    }

    /// <inheritdoc />
    public async Task NotificarIncidencia(string numeroSeguimiento, string tipoIncidencia,
        string descripcion)
    {
        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";

        await _hubContext.Clients.Group(grupo).SendAsync("IncidenciaDetectada", new
        {
            numeroSeguimiento,
            tipoIncidencia,
            descripcion,
            fechaHora = DateTime.UtcNow
        });

        _logger.LogInformation(
            "📡 Tracking → IncidenciaDetectada · {Seguimiento}: {Tipo}",
            numeroSeguimiento, tipoIncidencia);
    }
}
