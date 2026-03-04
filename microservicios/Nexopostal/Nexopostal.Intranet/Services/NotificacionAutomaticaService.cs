using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.Hubs;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio de notificaciones automáticas vía SignalR.
/// Envía alertas en tiempo real ante cambios de estado, incidencias,
/// retrasos y recordatorios de recogida.
/// </summary>
public interface INotificacionAutomaticaService
{
    /// <summary>
    /// Notifica a los operarios conectados sobre un cambio de estado de un paquete.
    /// </summary>
    Task NotificarCambioEstado(string numeroExpedicion, string estadoAnterior, string estadoNuevo);

    /// <summary>
    /// Notifica sobre una incidencia detectada en un paquete.
    /// </summary>
    Task NotificarIncidencia(int incidenciaId, string numeroExpedicion, string tipoIncidencia);

    /// <summary>
    /// Alerta sobre un paquete que lleva demasiados días sin movimiento.
    /// </summary>
    Task NotificarRetrasoPaquete(string numeroExpedicion, int diasSinMovimiento);

    /// <summary>
    /// Envía un recordatorio de recogida para un paquete.
    /// </summary>
    Task NotificarRecordatorioRecogida(string numeroExpedicion, string mensaje);
}

public class NotificacionAutomaticaService : INotificacionAutomaticaService
{
    private readonly IHubContext<IntranetHub> _hubContext;
    private readonly ILogger<NotificacionAutomaticaService> _logger;

    public NotificacionAutomaticaService(
        IHubContext<IntranetHub> hubContext,
        ILogger<NotificacionAutomaticaService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotificarCambioEstado(string numeroExpedicion, string estadoAnterior, string estadoNuevo)
    {
        var payload = new
        {
            Tipo = "CambioEstadoAutomatico",
            NumeroExpedicion = numeroExpedicion,
            EstadoAnterior = estadoAnterior,
            EstadoNuevo = estadoNuevo,
            Mensaje = $"El paquete {numeroExpedicion} ha cambiado de estado: {estadoAnterior} → {estadoNuevo}.",
            Timestamp = DateTime.UtcNow
        };

        // Notificar a todos los clientes conectados (broadcast general)
        await _hubContext.Clients.All.SendAsync("CambioEstadoAutomatico", payload);

        _logger.LogInformation(
            "📡 NotificaciónAutomática → CambioEstado · {Expedicion}: {Anterior} → {Nuevo}",
            numeroExpedicion, estadoAnterior, estadoNuevo);
    }

    /// <inheritdoc />
    public async Task NotificarIncidencia(int incidenciaId, string numeroExpedicion, string tipoIncidencia)
    {
        var payload = new
        {
            Tipo = "IncidenciaAutomatica",
            IncidenciaId = incidenciaId,
            NumeroExpedicion = numeroExpedicion,
            TipoIncidencia = tipoIncidencia,
            Mensaje = $"Se ha detectado una incidencia ({tipoIncidencia}) en el paquete {numeroExpedicion}.",
            Severidad = "Alta",
            Timestamp = DateTime.UtcNow
        };

        // Notificar a todos los jefes de CTA conectados
        await _hubContext.Clients.All.SendAsync("IncidenciaAutomatica", payload);

        _logger.LogInformation(
            "📡 NotificaciónAutomática → Incidencia · Id {Id}, Expedicion {Expedicion}, Tipo {Tipo}",
            incidenciaId, numeroExpedicion, tipoIncidencia);
    }

    /// <inheritdoc />
    public async Task NotificarRetrasoPaquete(string numeroExpedicion, int diasSinMovimiento)
    {
        var severidad = diasSinMovimiento switch
        {
            >= 7 => "Critica",
            >= 3 => "Alta",
            _ => "Media"
        };

        var payload = new
        {
            Tipo = "RetrasoPaquete",
            NumeroExpedicion = numeroExpedicion,
            DiasSinMovimiento = diasSinMovimiento,
            Mensaje = $"⚠️ El paquete {numeroExpedicion} lleva {diasSinMovimiento} día(s) sin movimiento.",
            Severidad = severidad,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.All.SendAsync("RetrasoPaquete", payload);

        _logger.LogWarning(
            "📡 NotificaciónAutomática → Retraso · {Expedicion}: {Dias} día(s) sin movimiento (severidad: {Severidad})",
            numeroExpedicion, diasSinMovimiento, severidad);
    }

    /// <inheritdoc />
    public async Task NotificarRecordatorioRecogida(string numeroExpedicion, string mensaje)
    {
        var payload = new
        {
            Tipo = "RecordatorioRecogida",
            NumeroExpedicion = numeroExpedicion,
            Mensaje = mensaje,
            Timestamp = DateTime.UtcNow
        };

        await _hubContext.Clients.All.SendAsync("RecordatorioRecogida", payload);

        _logger.LogInformation(
            "📡 NotificaciónAutomática → RecordatorioRecogida · {Expedicion}: {Mensaje}",
            numeroExpedicion, mensaje);
    }
}
