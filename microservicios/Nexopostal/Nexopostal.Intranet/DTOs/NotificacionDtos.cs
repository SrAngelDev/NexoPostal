namespace Nexopostal.Intranet.DTOs;

// ============================================================
//  DTOs para el sistema de notificaciones en tiempo real (SignalR)
// ============================================================

/// <summary>
/// Notificación genérica enviada a través de SignalR.
/// Contiene el tipo de evento, los datos asociados y metadatos de contexto.
/// </summary>
public class NotificacionDto
{
    /// <summary>Tipo de evento SignalR (ej: "PaqueteRecibidoEnCta", "TareaAsignada")</summary>
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Título breve de la notificación</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Mensaje descriptivo de la notificación</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>ID del CTA donde ocurre el evento</summary>
    public int CtaId { get; set; }

    /// <summary>Código del CTA (ej: "CTA-MAD")</summary>
    public string CtaCodigo { get; set; } = string.Empty;

    /// <summary>Número de expedición del paquete afectado (si aplica)</summary>
    public string? NumeroExpedicion { get; set; }

    /// <summary>Si el paquete es urgente</summary>
    public bool EsUrgente { get; set; }

    /// <summary>Fecha y hora UTC del evento</summary>
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;

    /// <summary>Datos adicionales específicos del tipo de notificación</summary>
    public object? Datos { get; set; }
}

/// <summary>
/// DTO para la admisión de un paquete en un CTA (simula la llegada).
/// El sistema resuelve automáticamente el CTA de destino según el código postal.
/// </summary>
public class AdmisionPaqueteDto
{
    /// <summary>Número de expedición interno del paquete (NXI-...)</summary>
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>Número de seguimiento visible al cliente (NXP-...)</summary>
    public string? NumeroSeguimiento { get; set; }

    /// <summary>Código postal de destino del paquete</summary>
    public string CodigoPostalDestino { get; set; } = string.Empty;

    /// <summary>Código postal de origen del paquete</summary>
    public string? CodigoPostalOrigen { get; set; }

    /// <summary>Si el envío es urgente (prioridad VIP)</summary>
    public bool EsUrgente { get; set; } = false;

    /// <summary>Nombre del remitente</summary>
    public string? Remitente { get; set; }

    /// <summary>Nombre del destinatario</summary>
    public string? Destinatario { get; set; }

    /// <summary>Nombre del destinatario para reparto de última milla</summary>
    public string? NombreDestinatario { get; set; }

    /// <summary>Teléfono del destinatario para contacto en entrega</summary>
    public string? TelefonoDestinatario { get; set; }

    /// <summary>Dirección completa para la entrega de última milla</summary>
    public string? DireccionEntrega { get; set; }

    /// <summary>Ciudad de entrega para la ruta de última milla</summary>
    public string? CiudadDestino { get; set; }

    /// <summary>Observaciones adicionales</summary>
    public string? Observaciones { get; set; }
}

/// <summary>
/// Respuesta tras admitir un paquete: incluye el CTA asignado y la notificación enviada.
/// </summary>
public class AdmisionPaqueteResponseDto
{
    /// <summary>Número de expedición del paquete admitido</summary>
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>CTA al que se ha enrutado el paquete</summary>
    public int CtaDestinoId { get; set; }
    public string CtaDestinoCodigo { get; set; } = string.Empty;
    public string CtaDestinoNombre { get; set; } = string.Empty;
    public string AreaZonal { get; set; } = string.Empty;

    /// <summary>CTA de origen (si se resolvió desde el CP de origen)</summary>
    public int? CtaOrigenId { get; set; }
    public string? CtaOrigenCodigo { get; set; }

    /// <summary>Si el paquete es urgente</summary>
    public bool EsUrgente { get; set; }

    /// <summary>Provincia de destino</summary>
    public string Provincia { get; set; } = string.Empty;

    /// <summary>Si se necesita movimiento troncal entre CTAs</summary>
    public bool RequiereMovimientoTroncal { get; set; }

    /// <summary>Tipo de transporte determinado (si requiere movimiento)</summary>
    public string? TipoTransporte { get; set; }

    /// <summary>Mensaje de confirmación</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Indica si se intentó orquestar la generación automática en Reparto</summary>
    public bool OrquestacionRepartoIntentada { get; set; }

    /// <summary>Indica si la orquestación de Reparto fue exitosa</summary>
    public bool OrquestacionRepartoExitosa { get; set; }

    /// <summary>Indica si la operación en Reparto fue idempotente</summary>
    public bool RepartoIdempotente { get; set; }

    /// <summary>ID de la ruta de reparto autoasignada (si aplica)</summary>
    public int? RutaRepartoId { get; set; }

    /// <summary>Código de la ruta de reparto autoasignada (si aplica)</summary>
    public string? RutaRepartoCodigo { get; set; }

    /// <summary>ID del repartidor asignado (si aplica)</summary>
    public int? RepartidorAsignadoId { get; set; }

    /// <summary>Nombre del repartidor asignado (si aplica)</summary>
    public string? RepartidorAsignadoNombre { get; set; }

    /// <summary>ID de la entrega creada en Reparto (si aplica)</summary>
    public int? EntregaRepartoId { get; set; }

    /// <summary>Mensaje de resultado de la orquestación con Reparto</summary>
    public string? MensajeOrquestacionReparto { get; set; }

    /// <summary>Fecha y hora de la admisión</summary>
    public DateTime FechaAdmision { get; set; } = DateTime.UtcNow;
}
