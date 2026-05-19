using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Hubs;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio de notificaciones en tiempo real vía SignalR.
/// Envía eventos a los operarios conectados según su CTA y rol.
/// 
/// Grupos definidos en IntranetHub:
///   - "cta-{ctaId}" → Todos los operarios del CTA
///   - "cta-{ctaId}-cta" → Solo OperarioCTA
///   - "cta-{ctaId}-supervisor" → Solo Supervisores
///   - "cta-{ctaId}-operarios" → Solo OperarioOficina
///   - "operario-{operarioId}" → Operario individual
/// </summary>
public interface INotificacionService
{
    /// <summary>Notifica al rol OperarioCTA de un CTA que un paquete ha llegado y necesita clasificación</summary>
    Task NotificarPaqueteRecibidoEnCta(int ctaId, string ctaCodigo, string numeroExpedicion,
        bool esUrgente, string provincia, string? observaciones = null);

    /// <summary>Notifica a un operario que se le ha asignado una tarea</summary>
    Task NotificarTareaAsignada(int operarioId, int ctaId, string ctaCodigo,
        string numeroExpedicion, string tipoTarea, bool esUrgente, string asignadoPor);

    /// <summary>Notifica al rol OperarioCTA del CTA que un operario ha iniciado una tarea</summary>
    Task NotificarTareaIniciada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoTarea, string operarioNombre);

    /// <summary>Notifica al rol OperarioCTA del CTA que un operario ha completado una tarea</summary>
    Task NotificarTareaCompletada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoTarea, string operarioNombre);

    /// <summary>Notifica al rol OperarioCTA del CTA que se ha cancelado una tarea</summary>
    Task NotificarTareaCancelada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoTarea, string canceladoPor);

    /// <summary>Notifica al rol OperarioCTA del CTA destino que un movimiento ha sido despachado desde otro CTA</summary>
    Task NotificarMovimientoDespachado(int ctaDestinoId, string ctaDestinoCodigo,
        string ctaOrigenCodigo, string numeroExpedicion, string tipoTransporte, bool esUrgente);

    /// <summary>Notifica al rol OperarioCTA del CTA que un paquete ha llegado desde otro CTA (movimiento recibido)</summary>
    Task NotificarMovimientoRecibido(int ctaDestinoId, string ctaDestinoCodigo,
        string ctaOrigenCodigo, string numeroExpedicion, bool esUrgente);

    /// <summary>Notifica a todo el CTA que se ha reportado una nueva incidencia</summary>
    Task NotificarIncidenciaCreada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoIncidencia, string reportadaPor);

    /// <summary>Notifica a todo el CTA que una incidencia ha cambiado de estado</summary>
    Task NotificarIncidenciaActualizada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoIncidencia, string nuevoEstado, string? resolucion);

    /// <summary>Envía una notificación general a todo un CTA</summary>
    Task NotificarGeneralCta(int ctaId, string ctaCodigo, string titulo, string mensaje);
}

public class NotificacionService : INotificacionService
{
    private readonly IHubContext<IntranetHub> _hubContext;
    private readonly ILogger<NotificacionService> _logger;

    public NotificacionService(IHubContext<IntranetHub> hubContext, ILogger<NotificacionService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task NotificarPaqueteRecibidoEnCta(int ctaId, string ctaCodigo, string numeroExpedicion,
        bool esUrgente, string provincia, string? observaciones = null)
    {
        var prioridad = esUrgente ? "🔴 URGENTE" : "📦 Normal";
        var notificacion = new NotificacionDto
        {
            Tipo = "PaqueteRecibidoEnCta",
            Titulo = $"{prioridad} · Paquete pendiente de gestión",
            Mensaje = $"El paquete {numeroExpedicion} con destino a {provincia} ha llegado al {ctaCodigo} y requiere gestión operativa.",
            CtaId = ctaId,
            CtaCodigo = ctaCodigo,
            NumeroExpedicion = numeroExpedicion,
            EsUrgente = esUrgente,
            Datos = new
            {
                provincia,
                observaciones,
                accionRequerida = "Revisar y gestionar el paquete en el CTA"
            }
        };

        // Notificar al rol OperarioCTA del CTA
        await _hubContext.Clients.Group($"cta-{ctaId}-cta")
            .SendAsync("PaqueteRecibidoEnCta", notificacion);

        _logger.LogInformation(
            "📡 SignalR → PaqueteRecibidoEnCta · {Expedicion} → CTA {Cta} (urgente: {Urgente})",
            numeroExpedicion, ctaCodigo, esUrgente);
    }

    /// <inheritdoc />
    public async Task NotificarTareaAsignada(int operarioId, int ctaId, string ctaCodigo,
        string numeroExpedicion, string tipoTarea, bool esUrgente, string asignadoPor)
    {
        var prioridad = esUrgente ? "🔴 URGENTE" : "";
        var notificacion = new NotificacionDto
        {
            Tipo = "TareaAsignada",
            Titulo = $"Nueva tarea: {tipoTarea} {prioridad}",
            Mensaje = $"{asignadoPor} te ha asignado la tarea de {tipoTarea} para el paquete {numeroExpedicion}.",
            CtaId = ctaId,
            CtaCodigo = ctaCodigo,
            NumeroExpedicion = numeroExpedicion,
            EsUrgente = esUrgente,
            Datos = new
            {
                tipoTarea,
                asignadoPor,
                accionRequerida = "Iniciar la tarea cuando estés listo"
            }
        };

        // Notificar al operario individual asignado
        await _hubContext.Clients.Group($"operario-{operarioId}")
            .SendAsync("TareaAsignada", notificacion);

        _logger.LogInformation(
            "📡 SignalR → TareaAsignada · {Tarea} para {Expedicion} → Operario {OpId} en {Cta}",
            tipoTarea, numeroExpedicion, operarioId, ctaCodigo);
    }

    /// <inheritdoc />
    public async Task NotificarTareaIniciada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoTarea, string operarioNombre)
    {
        var notificacion = new NotificacionDto
        {
            Tipo = "TareaIniciada",
            Titulo = $"Tarea iniciada: {tipoTarea}",
            Mensaje = $"{operarioNombre} ha iniciado la tarea de {tipoTarea} para el paquete {numeroExpedicion}.",
            CtaId = ctaId,
            CtaCodigo = ctaCodigo,
            NumeroExpedicion = numeroExpedicion,
            Datos = new { tipoTarea, operarioNombre }
        };

        // Notificar al rol OperarioCTA del CTA para seguimiento
        await _hubContext.Clients.Group($"cta-{ctaId}-cta")
            .SendAsync("TareaIniciada", notificacion);

        _logger.LogInformation(
            "📡 SignalR → TareaIniciada · {Tarea} por {Operario} en {Cta}",
            tipoTarea, operarioNombre, ctaCodigo);
    }

    /// <inheritdoc />
    public async Task NotificarTareaCompletada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoTarea, string operarioNombre)
    {
        var notificacion = new NotificacionDto
        {
            Tipo = "TareaCompletada",
            Titulo = $"Tarea completada: {tipoTarea}",
            Mensaje = $"{operarioNombre} ha completado la tarea de {tipoTarea} para el paquete {numeroExpedicion}.",
            CtaId = ctaId,
            CtaCodigo = ctaCodigo,
            NumeroExpedicion = numeroExpedicion,
            Datos = new { tipoTarea, operarioNombre }
        };

        // Notificar al rol OperarioCTA del CTA
        await _hubContext.Clients.Group($"cta-{ctaId}-cta")
            .SendAsync("TareaCompletada", notificacion);

        _logger.LogInformation(
            "📡 SignalR → TareaCompletada · {Tarea} por {Operario} en {Cta}",
            tipoTarea, operarioNombre, ctaCodigo);
    }

    /// <inheritdoc />
    public async Task NotificarTareaCancelada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoTarea, string canceladoPor)
    {
        var notificacion = new NotificacionDto
        {
            Tipo = "TareaCancelada",
            Titulo = $"Tarea cancelada: {tipoTarea}",
            Mensaje = $"{canceladoPor} ha cancelado la tarea de {tipoTarea} para el paquete {numeroExpedicion}.",
            CtaId = ctaId,
            CtaCodigo = ctaCodigo,
            NumeroExpedicion = numeroExpedicion,
            Datos = new { tipoTarea, canceladoPor }
        };

        // Notificar a todo el CTA (al operario afectado también)
        await _hubContext.Clients.Group($"cta-{ctaId}")
            .SendAsync("TareaCancelada", notificacion);

        _logger.LogInformation(
            "📡 SignalR → TareaCancelada · {Tarea} cancelada por {Cancelador} en {Cta}",
            tipoTarea, canceladoPor, ctaCodigo);
    }

    /// <inheritdoc />
    public async Task NotificarMovimientoDespachado(int ctaDestinoId, string ctaDestinoCodigo,
        string ctaOrigenCodigo, string numeroExpedicion, string tipoTransporte, bool esUrgente)
    {
        var prioridad = esUrgente ? "🔴 URGENTE" : "";
        var notificacion = new NotificacionDto
        {
            Tipo = "MovimientoDespachado",
            Titulo = $"Movimiento en camino {prioridad}",
            Mensaje = $"El paquete {numeroExpedicion} ha salido de {ctaOrigenCodigo} con destino {ctaDestinoCodigo} vía {tipoTransporte}.",
            CtaId = ctaDestinoId,
            CtaCodigo = ctaDestinoCodigo,
            NumeroExpedicion = numeroExpedicion,
            EsUrgente = esUrgente,
            Datos = new { ctaOrigenCodigo, tipoTransporte }
        };

        // Notificar al rol OperarioCTA del CTA destino
        await _hubContext.Clients.Group($"cta-{ctaDestinoId}-cta")
            .SendAsync("MovimientoDespachado", notificacion);

        _logger.LogInformation(
            "📡 SignalR → MovimientoDespachado · {Expedicion} de {Origen} → {Destino}",
            numeroExpedicion, ctaOrigenCodigo, ctaDestinoCodigo);
    }

    /// <inheritdoc />
    public async Task NotificarMovimientoRecibido(int ctaDestinoId, string ctaDestinoCodigo,
        string ctaOrigenCodigo, string numeroExpedicion, bool esUrgente)
    {
        var prioridad = esUrgente ? "🔴 URGENTE" : "📦";
        var notificacion = new NotificacionDto
        {
            Tipo = "MovimientoRecibido",
            Titulo = $"{prioridad} Paquete recibido desde {ctaOrigenCodigo}",
            Mensaje = $"El paquete {numeroExpedicion} ha llegado a {ctaDestinoCodigo} desde {ctaOrigenCodigo}. Pendiente de descarga y clasificación.",
            CtaId = ctaDestinoId,
            CtaCodigo = ctaDestinoCodigo,
            NumeroExpedicion = numeroExpedicion,
            EsUrgente = esUrgente,
            Datos = new
            {
                ctaOrigenCodigo,
                accionRequerida = "Asignar tarea de descarga y clasificación"
            }
        };

        // Notificar al rol OperarioCTA del CTA destino → deben organizar la descarga
        await _hubContext.Clients.Group($"cta-{ctaDestinoId}-cta")
            .SendAsync("MovimientoRecibido", notificacion);

        _logger.LogInformation(
            "📡 SignalR → MovimientoRecibido · {Expedicion} llegó a {Destino} desde {Origen}",
            numeroExpedicion, ctaDestinoCodigo, ctaOrigenCodigo);
    }

    /// <inheritdoc />
    public async Task NotificarIncidenciaCreada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoIncidencia, string reportadaPor)
    {
        var notificacion = new NotificacionDto
        {
            Tipo = "IncidenciaCreada",
            Titulo = $"⚠️ Incidencia: {tipoIncidencia}",
            Mensaje = $"{reportadaPor} ha reportado una incidencia de tipo '{tipoIncidencia}' para el paquete {numeroExpedicion} en {ctaCodigo}.",
            CtaId = ctaId,
            CtaCodigo = ctaCodigo,
            NumeroExpedicion = numeroExpedicion,
            Datos = new { tipoIncidencia, reportadaPor }
        };

        // Notificar a todo el CTA (la incidencia puede afectar al flujo de todos)
        await _hubContext.Clients.Group($"cta-{ctaId}")
            .SendAsync("IncidenciaCreada", notificacion);

        _logger.LogInformation(
            "📡 SignalR → IncidenciaCreada · {Tipo} para {Expedicion} en {Cta}",
            tipoIncidencia, numeroExpedicion, ctaCodigo);
    }

    /// <inheritdoc />
    public async Task NotificarIncidenciaActualizada(int ctaId, string ctaCodigo, string numeroExpedicion,
        string tipoIncidencia, string nuevoEstado, string? resolucion)
    {
        var notificacion = new NotificacionDto
        {
            Tipo = "IncidenciaActualizada",
            Titulo = $"Incidencia actualizada → {nuevoEstado}",
            Mensaje = $"La incidencia de '{tipoIncidencia}' para el paquete {numeroExpedicion} ha pasado a estado '{nuevoEstado}'.",
            CtaId = ctaId,
            CtaCodigo = ctaCodigo,
            NumeroExpedicion = numeroExpedicion,
            Datos = new { tipoIncidencia, nuevoEstado, resolucion }
        };

        // Notificar a todo el CTA
        await _hubContext.Clients.Group($"cta-{ctaId}")
            .SendAsync("IncidenciaActualizada", notificacion);

        _logger.LogInformation(
            "📡 SignalR → IncidenciaActualizada · {Tipo} → {Estado} en {Cta}",
            tipoIncidencia, nuevoEstado, ctaCodigo);
    }

    /// <inheritdoc />
    public async Task NotificarGeneralCta(int ctaId, string ctaCodigo, string titulo, string mensaje)
    {
        var notificacion = new NotificacionDto
        {
            Tipo = "NotificacionGeneral",
            Titulo = titulo,
            Mensaje = mensaje,
            CtaId = ctaId,
            CtaCodigo = ctaCodigo
        };

        await _hubContext.Clients.Group($"cta-{ctaId}")
            .SendAsync("NotificacionGeneral", notificacion);

        _logger.LogInformation("📡 SignalR → NotificacionGeneral · {Titulo} en {Cta}", titulo, ctaCodigo);
    }
}
