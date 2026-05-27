namespace Nexopostal.Intranet.DTOs;

// DTOs del sistema de notificaciones en tiempo real y de la admisión operativa.

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

    // Modalidad de entrega y oficinas implicadas en el nuevo flujo.

    /// <summary>Oficina postal de origen (OficinaJsonId) donde el cliente entregó el paquete.</summary>
    public int? OficinaOrigenId { get; set; }

    /// <summary>Oficina postal de destino (OficinaJsonId) donde el destinatario lo recogerá. Solo si TipoEntrega == "Oficina".</summary>
    public int? OficinaDestinoId { get; set; }

    /// <summary>"Domicilio" por defecto o "Oficina".</summary>
    public string TipoEntrega { get; set; } = "Domicilio";

    /// <summary>
    /// True si el paquete ya está físicamente en la oficina origen (alta presencial).
    /// En ese caso el flujo arranca en RecogidoEnOrigen y se autoasigna tarea SalidaOficinaACta al operario.
    /// </summary>
    public bool YaRecogidoEnOrigen { get; set; } = false;

    /// <summary>
    /// Id del OperarioOficina que dio de alta el paquete (solo en alta presencial).
    /// Se usa para autoasignarle la tarea inicial SalidaOficinaACta.
    /// </summary>
    public int? OperarioOficinaId { get; set; }

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

    /// <summary>Indica si se intentó crear asignación automática en CTA</summary>
    public bool AsignacionAutomaticaIntentada { get; set; }

    /// <summary>Indica si la asignación automática en CTA fue exitosa</summary>
    public bool AsignacionAutomaticaExitosa { get; set; }

    /// <summary>Indica si la asignación automática fue idempotente (ya existía)</summary>
    public bool AsignacionAutomaticaIdempotente { get; set; }

    /// <summary>ID de la asignación creada o reutilizada (si aplica)</summary>
    public int? AsignacionAutomaticaId { get; set; }

    /// <summary>ID del operario de oficina asignado automáticamente (si aplica)</summary>
    public int? OperarioAsignadoId { get; set; }

    /// <summary>Nombre del operario de oficina asignado automáticamente (si aplica)</summary>
    public string? OperarioAsignadoNombre { get; set; }

    /// <summary>Mensaje de resultado de la autoasignación en CTA</summary>
    public string? MensajeAsignacionAutomatica { get; set; }

    /// <summary>Fecha y hora de la admisión</summary>
    public DateTime FechaAdmision { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO de alta presencial de envio en oficina (consumido por endpoint Intranet).
/// El operario de oficina rellena este formulario; Intranet llama a Ciudadano
/// para crear el envio y luego ejecuta AdmitirPaquete con YaRecogidoEnOrigen=true.
/// </summary>
public class AltaEnvioOficinaIntranetDto
{
    public decimal Peso { get; set; }
    public string Dimensiones { get; set; } = string.Empty;

    public string NombreRemitente { get; set; } = string.Empty;
    public string? ApellidosRemitente { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string CodigoPostalOrigen { get; set; } = string.Empty;
    public string TelefonoRemitente { get; set; } = string.Empty;
    public string? EmailRemitente { get; set; }
    public string? DniRemitente { get; set; }

    public string NombreDestinatario { get; set; } = string.Empty;
    public string? ApellidosDestinatario { get; set; }
    public string Destino { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;
    public string TelefonoDestinatario { get; set; } = string.Empty;
    public string? EmailDestinatario { get; set; }

    public string TipoEntrega { get; set; } = "Domicilio";
    public int? OficinaDestinoId { get; set; }

    public string MetodoCobro { get; set; } = "Efectivo";
    public string? Observaciones { get; set; }
}

/// <summary>
/// Respuesta del endpoint POST /api/admision/oficina/alta.
/// </summary>
public class AltaEnvioOficinaResponseDto
{
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public decimal CosteCalculado { get; set; }
    public string TipoEntrega { get; set; } = "Domicilio";
    public int? OficinaOrigenId { get; set; }
    public int? OficinaDestinoId { get; set; }
    public string? CtaDestinoCodigo { get; set; }
    public string Mensaje { get; set; } = string.Empty;
}
