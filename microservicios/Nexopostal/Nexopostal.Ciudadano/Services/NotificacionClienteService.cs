using Microsoft.AspNetCore.SignalR;
using Nexopostal.Ciudadano.Hubs;
using Nexopostal.Ciudadano.Repositories;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio para enviar notificaciones automatizadas a los clientes sobre sus envíos.
/// Utiliza SignalR (TrackingHub) para enviar notificaciones push en tiempo real.
/// 
/// Tipos de notificación:
///   - CambioEstado: el estado del envío ha cambiado
///   - EntregaCompletada: el paquete ha sido entregado
///   - Incidencia: se ha detectado un problema con el envío
///   - RecordatorioRecogida: recordatorio para recoger el paquete en oficina
/// </summary>
public interface INotificacionClienteService
{
    /// <summary>
    /// Envía una notificación de cambio de estado a los clientes suscritos al envío.
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    /// <param name="estadoAnterior">Estado anterior del envío</param>
    /// <param name="estadoNuevo">Nuevo estado del envío</param>
    Task NotificarCambioEstado(string numeroSeguimiento, string estadoAnterior, string estadoNuevo);

    /// <summary>
    /// Notifica al cliente que su envío ha sido entregado correctamente.
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    Task NotificarEntregaCompletada(string numeroSeguimiento);

    /// <summary>
    /// Notifica al cliente sobre una incidencia detectada en su envío.
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    /// <param name="descripcion">Descripción de la incidencia</param>
    Task NotificarIncidencia(string numeroSeguimiento, string descripcion);

    /// <summary>
    /// Envía un recordatorio al cliente para que recoja su paquete en oficina.
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    /// <param name="diasEnOficina">Número de días que el paquete lleva en oficina</param>
    Task NotificarRecordatorioRecogida(string numeroSeguimiento, int diasEnOficina);
}

public class NotificacionClienteService : INotificacionClienteService
{
    private readonly IHubContext<TrackingHub> _hubContext;
    private readonly IEnvioRepository _envioRepository;
    private readonly ILogger<NotificacionClienteService> _logger;

    public NotificacionClienteService(
        IHubContext<TrackingHub> hubContext,
        IEnvioRepository envioRepository,
        ILogger<NotificacionClienteService> logger)
    {
        _hubContext = hubContext;
        _envioRepository = envioRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotificarCambioEstado(string numeroSeguimiento, string estadoAnterior, string estadoNuevo)
    {
        var envio = await _envioRepository.GetByTrackingAsync(numeroSeguimiento);

        if (envio is null)
        {
            _logger.LogWarning(
                "Notificación de cambio de estado omitida: envío {NumeroSeguimiento} no encontrado",
                numeroSeguimiento);
            return;
        }

        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";
        var payload = new
        {
            tipo = "CambioEstado",
            mensaje = $"El estado de su envío ha cambiado de {estadoAnterior} a {estadoNuevo}.",
            fecha = DateTime.UtcNow,
            numeroSeguimiento
        };

        await _hubContext.Clients.Group(grupo).SendAsync("notificacion", payload);

        _logger.LogInformation(
            "🔔 Notificación CambioEstado · {NumeroSeguimiento} · {EstadoAnterior} → {EstadoNuevo}",
            numeroSeguimiento, estadoAnterior, estadoNuevo);
    }

    /// <inheritdoc />
    public async Task NotificarEntregaCompletada(string numeroSeguimiento)
    {
        var envio = await _envioRepository.GetByTrackingAsync(numeroSeguimiento);

        if (envio is null)
        {
            _logger.LogWarning(
                "Notificación de entrega omitida: envío {NumeroSeguimiento} no encontrado",
                numeroSeguimiento);
            return;
        }

        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";
        var payload = new
        {
            tipo = "EntregaCompletada",
            mensaje = $"Su envío {numeroSeguimiento} ha sido entregado correctamente a {envio.NombreDestinatario} {envio.ApellidosDestinatario}.",
            fecha = DateTime.UtcNow,
            numeroSeguimiento
        };

        await _hubContext.Clients.Group(grupo).SendAsync("notificacion", payload);

        _logger.LogInformation(
            "🔔 Notificación EntregaCompletada · {NumeroSeguimiento}",
            numeroSeguimiento);
    }

    /// <inheritdoc />
    public async Task NotificarIncidencia(string numeroSeguimiento, string descripcion)
    {
        var envio = await _envioRepository.GetByTrackingAsync(numeroSeguimiento);

        if (envio is null)
        {
            _logger.LogWarning(
                "Notificación de incidencia omitida: envío {NumeroSeguimiento} no encontrado",
                numeroSeguimiento);
            return;
        }

        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";
        var payload = new
        {
            tipo = "Incidencia",
            mensaje = $"Se ha detectado una incidencia en su envío {numeroSeguimiento}: {descripcion}",
            fecha = DateTime.UtcNow,
            numeroSeguimiento
        };

        await _hubContext.Clients.Group(grupo).SendAsync("notificacion", payload);

        _logger.LogInformation(
            "🔔 Notificación Incidencia · {NumeroSeguimiento} · {Descripcion}",
            numeroSeguimiento, descripcion);
    }

    /// <inheritdoc />
    public async Task NotificarRecordatorioRecogida(string numeroSeguimiento, int diasEnOficina)
    {
        var envio = await _envioRepository.GetByTrackingAsync(numeroSeguimiento);

        if (envio is null)
        {
            _logger.LogWarning(
                "Notificación de recordatorio omitida: envío {NumeroSeguimiento} no encontrado",
                numeroSeguimiento);
            return;
        }

        var grupo = $"tracking-{numeroSeguimiento.ToUpper().Trim()}";
        var payload = new
        {
            tipo = "RecordatorioRecogida",
            mensaje = $"Su paquete {numeroSeguimiento} lleva {diasEnOficina} día(s) en oficina esperando recogida. " +
                      "Por favor, acuda a recogerlo lo antes posible.",
            fecha = DateTime.UtcNow,
            numeroSeguimiento
        };

        await _hubContext.Clients.Group(grupo).SendAsync("notificacion", payload);

        _logger.LogInformation(
            "🔔 Notificación RecordatorioRecogida · {NumeroSeguimiento} · {Dias} días en oficina",
            numeroSeguimiento, diasEnOficina);
    }
}
