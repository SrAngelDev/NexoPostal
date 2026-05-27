using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>
/// DTO para cotizar un envío (sin necesidad de estar autenticado)
/// </summary>
public class CotizarEnvioDto
{
    [Required]
    [Range(0.1, 30, ErrorMessage = "El peso debe estar entre 0.1 y 30 kg")]
    public decimal Peso { get; set; }

    [MaxLength(50)]
    public string? Dimensiones { get; set; }

    [Required]
    [MaxLength(10)]
    public string CodigoPostalOrigen { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalDestino { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de la cotización previa al alta del envío.
/// Resume precio, plazo y observaciones para que el cliente decida si continúa.
/// </summary>
public class CotizacionResultadoDto
{
    /// <summary>Precio final estimado para el envío.</summary>
    public decimal Precio { get; set; }

    /// <summary>Moneda en la que se expresa la cotización.</summary>
    public string Moneda { get; set; } = "EUR";

    /// <summary>Tiempo estimado de entrega expresado en días.</summary>
    public int TiempoEstimadoDias { get; set; }

    /// <summary>Aclaraciones adicionales sobre la tarifa o la cobertura.</summary>
    public string Observaciones { get; set; } = string.Empty;
}

/// <summary>
/// DTO para crear un nuevo envío (requiere autenticación)
/// </summary>
public class CrearEnvioDto
{
    // Datos básicos del paquete.
    [Required]
    [Range(0.1, 30)]
    public decimal Peso { get; set; }

    [Required]
    [MaxLength(50)]
    public string Dimensiones { get; set; } = string.Empty;

    // Datos del remitente.
    [Required]
    [MaxLength(200)]
    public string NombreRemitente { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Origen { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalOrigen { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? TelefonoRemitente { get; set; }

    // Datos del destinatario.
    [Required]
    [MaxLength(200)]
    public string NombreDestinatario { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Destino { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalDestino { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? TelefonoDestinatario { get; set; }

    // Modalidad de entrega y oficinas implicadas.
    /// <summary>
    /// Oficina postal donde el remitente entregará el paquete (OficinaJsonId de oficinas.json).
    /// </summary>
    [Required]
    public int OficinaOrigenId { get; set; }

    /// <summary>
    /// Modalidad de entrega final: "Domicilio" o "Oficina".
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string TipoEntrega { get; set; } = "Domicilio";

    /// <summary>
    /// Oficina postal donde el destinatario recogerá el envío. Obligatorio si TipoEntrega == "Oficina".
    /// </summary>
    public int? OficinaDestinoId { get; set; }

    // Observaciones opcionales para la operativa.
    [MaxLength(1000)]
    public string? Observaciones { get; set; }
}

/// <summary>
/// Respuesta generada al dar de alta un envío antes de pasar por el pago o la impresión.
/// </summary>
public class EnvioCreadoDto
{
    /// <summary>Número público que se mostrará al cliente para el seguimiento.</summary>
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>Número interno que usa la red logística.</summary>
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>Precio calculado para ese envío.</summary>
    public decimal CosteCalculado { get; set; }

    /// <summary>Estado público inicial del envío.</summary>
    public string EstadoActual { get; set; } = string.Empty;

    /// <summary>Modalidad de entrega elegida: domicilio u oficina.</summary>
    public string TipoEntrega { get; set; } = string.Empty;

    /// <summary>Oficina de origen asociada a la admisión.</summary>
    public int? OficinaOrigenId { get; set; }

    /// <summary>Oficina destino, solo cuando aplica recogida en oficina.</summary>
    public int? OficinaDestinoId { get; set; }

    /// <summary>Fecha de creación de la expedición.</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Ruta de la etiqueta generada para imprimir el envío.</summary>
    public string UrlEtiqueta { get; set; } = string.Empty;

    /// <summary>Ruta de la factura, si ya ha sido emitida.</summary>
    public string? UrlFactura { get; set; }
}

/// <summary>
/// DTO para consultar el tracking público de un envío.
/// NO incluye datos sensibles (origen, destino, peso, observaciones).
/// Solo muestra el estado de progreso simplificado del envío.
/// Se consulta con el NumeroSeguimiento (NX...ES) — el que aparece en el QR de la etiqueta.
/// </summary>
public class EnvioTrackingDto
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string EstadoActual { get; set; } = string.Empty;

    /// <summary>
    /// Estado interno detallado (nombre del enum EstadoInterno).
    /// Necesario en la barra de progreso del frontend (8 pasos).
    /// </summary>
    public string EstadoInterno { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }

    /// <summary>
    /// Fecha de entrega cuando EstadoActual == Entregado. Null en otro caso.
    /// </summary>
    public DateTime? FechaEntrega { get; set; }

    public int NumeroBultos { get; set; } = 1;

    /// <summary>
    /// Historial cronológico de eventos. Vacío por ahora (P0 — futuras versiones lo poblarán).
    /// </summary>
    public List<object> Eventos { get; set; } = new();
}

/// <summary>
/// DTO usado por Intranet para sincronizar cambios de estado interno
/// hacia Ciudadano (escaneos manuales y simulación de transporte).
/// </summary>
public class TrackingScanEstadoDto
{
    /// <summary>
    /// Número de seguimiento público (NXP-...). Opcional si se proporciona NumeroExpedicion.
    /// </summary>
    [MaxLength(40)]
    public string? NumeroSeguimiento { get; set; }

    /// <summary>
    /// Número de expedición interno (NXI-...). Opcional si se proporciona NumeroSeguimiento.
    /// </summary>
    [MaxLength(40)]
    public string? NumeroExpedicion { get; set; }

    /// <summary>
    /// Nuevo estado interno (nombre exacto del enum EstadoInterno).
    /// </summary>
    [Required]
    [MaxLength(60)]
    public string EstadoInterno { get; set; } = string.Empty;

    /// <summary>
    /// Descripción operativa del cambio (se incluirá en el evento SignalR).
    /// </summary>
    [MaxLength(300)]
    public string? Descripcion { get; set; }
}

/// <summary>
/// DTO para consultar el tracking INTERNO detallado de un envío.
/// Incluye TODA la información operativa del envío.
/// Se consulta con el NumeroExpedicion (NXI-...) — el código de barras interno de la etiqueta.
/// Solo accesible desde intranet y driver-app (roles: Admin, Operario*, Repartidor*).
/// </summary>
public class EnvioInternoDetalladoDto
{
    // Identificadores
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string NumeroExpedicion { get; set; } = string.Empty;

    // Estado público y estado interno detallado
    public string EstadoPublico { get; set; } = string.Empty;
    public string EstadoInterno { get; set; } = string.Empty;
    public string DescripcionEstadoInterno { get; set; } = string.Empty;

    // Datos del paquete
    public decimal PesoKg { get; set; }
    public string Dimensiones { get; set; } = string.Empty;

    // Datos logísticos
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string CodigoPostalOrigen { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;

    // Datos del remitente
    public string NombreRemitente { get; set; } = string.Empty;
    public string ApellidosRemitente { get; set; } = string.Empty;
    public string TelefonoRemitente { get; set; } = string.Empty;
    public string? EmailRemitente { get; set; }
    public string? DniRemitente { get; set; }

    // Datos del destinatario
    public string NombreDestinatario { get; set; } = string.Empty;
    public string ApellidosDestinatario { get; set; } = string.Empty;
    public string TelefonoDestinatario { get; set; } = string.Empty;
    public string? EmailDestinatario { get; set; }
    public string? DniDestinatario { get; set; }

    // Datos administrativos
    public string TipoTarifa { get; set; } = string.Empty;
    public string TiempoEntregaEstimado { get; set; } = string.Empty;
    public decimal CosteCalculado { get; set; }
    public bool Pagado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? Observaciones { get; set; }

    // Modalidad de entrega y oficinas
    public string TipoEntrega { get; set; } = "Domicilio";
    public int? OficinaOrigenId { get; set; }
    public int? OficinaDestinoId { get; set; }
}

/// <summary>
/// DTO para que un operario o repartidor actualice el estado interno de un envío.
/// </summary>
public class ActualizarEstadoInternoDto
{
    /// <summary>
    /// Nuevo estado interno (nombre del enum EstadoInterno)
    /// </summary>
    [Required]
    public string NuevoEstadoInterno { get; set; } = string.Empty;

    /// <summary>
    /// Observaciones opcionales del cambio de estado
    /// </summary>
    [MaxLength(500)]
    public string? Observaciones { get; set; }
}

/// <summary>
/// DTO con información resumida del envío para listados
/// </summary>
public class EnvioResumenDto
{
    /// <summary>Número de seguimiento visible para el cliente.</summary>
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>Estado público simplificado del envío.</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Fecha en la que se registró el envío.</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Dirección o destino resumido mostrado en el listado.</summary>
    public string Destino { get; set; } = string.Empty;

    /// <summary>Importe final asociado al envío.</summary>
    public decimal Precio { get; set; }

    /// <summary>Indica si el envío ya consta como pagado.</summary>
    public bool Pagado { get; set; }

    /// <summary>Tarifa comercial aplicada.</summary>
    public string TipoTarifa { get; set; } = string.Empty;
}

/// <summary>
/// DTO con información resumida interna del envío para listados en intranet/driver-app
/// </summary>
public class EnvioResumenInternoDto
{
    /// <summary>Número de seguimiento público.</summary>
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>Número de expedición interno.</summary>
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>Estado público visible para el cliente.</summary>
    public string EstadoPublico { get; set; } = string.Empty;

    /// <summary>Estado operativo interno del envío.</summary>
    public string EstadoInterno { get; set; } = string.Empty;

    /// <summary>Fecha de creación del envío.</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Origen resumido para contexto operativo.</summary>
    public string Origen { get; set; } = string.Empty;

    /// <summary>Destino resumido para contexto operativo.</summary>
    public string Destino { get; set; } = string.Empty;

    /// <summary>Código postal de destino.</summary>
    public string CodigoPostalDestino { get; set; } = string.Empty;

    /// <summary>Peso del paquete en kilogramos.</summary>
    public decimal PesoKg { get; set; }

    /// <summary>Tarifa comercial aplicada.</summary>
    public string TipoTarifa { get; set; } = string.Empty;

    /// <summary>Marca si el envío consta como pagado.</summary>
    public bool Pagado { get; set; }

    /// <summary>Modalidad de entrega final.</summary>
    public string TipoEntrega { get; set; } = "Domicilio";

    /// <summary>Oficina de origen que admitió el paquete.</summary>
    public int? OficinaOrigenId { get; set; }

    /// <summary>Oficina destino, cuando la entrega termina en recogida.</summary>
    public int? OficinaDestinoId { get; set; }
}

/// <summary>
/// DTO interno para notificar ubicación del repartidor asociada a un envío.
/// Se usa para emitir eventos SignalR al tracking público.
/// </summary>
public class TrackingUbicacionRepartoDto
{
    [Required]
    [MaxLength(20)]
    public string NumeroSeguimiento { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitud { get; set; }

    [Range(-180, 180)]
    public double Longitud { get; set; }

    [MaxLength(120)]
    public string TipoUbicacion { get; set; } = "RepartidorEnRuta";

    [MaxLength(200)]
    public string? Ubicacion { get; set; }

    [MaxLength(300)]
    public string? Descripcion { get; set; }
}

/// <summary>
/// DTO interno para sincronizar eventos operativos de entrega desde Reparto.
/// Permite unificar estado interno/publico y notificaciones realtime en Ciudadano.
/// </summary>
public class TrackingEventoEntregaDto
{
    /// <summary>Número de seguimiento del envío afectado.</summary>
    [Required]
    [MaxLength(20)]
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>Número interno de expedición, si el emisor lo conoce.</summary>
    [MaxLength(20)]
    public string? NumeroExpedicion { get; set; }

    /// <summary>Estado de entrega notificado por el módulo de reparto.</summary>
    [Required]
    [MaxLength(40)]
    public string EstadoEntrega { get; set; } = string.Empty;

    /// <summary>Número de intento de entrega asociado al evento.</summary>
    [Range(1, 10)]
    public int NumeroIntento { get; set; } = 1;

    /// <summary>Observaciones libres que ayudan a explicar el evento.</summary>
    [MaxLength(500)]
    public string? Observaciones { get; set; }

    /// <summary>Nombre de la persona que recibió el paquete, si se recogió.</summary>
    [MaxLength(200)]
    public string? ReceptorNombre { get; set; }

    /// <summary>DNI de la persona receptora, cuando se ha capturado.</summary>
    [MaxLength(15)]
    public string? ReceptorDni { get; set; }

    /// <summary>Latitud del punto de entrega, si se registró geolocalización.</summary>
    [Range(-90, 90)]
    public double? Latitud { get; set; }

    /// <summary>Longitud del punto de entrega, si se registró geolocalización.</summary>
    [Range(-180, 180)]
    public double? Longitud { get; set; }

    /// <summary>Fotografía de la entrega o del intento, cuando se adjunta evidencia.</summary>
    [MaxLength(500)]
    public string? FotoEntrega { get; set; }

    /// <summary>Firma digital recogida en la entrega, cuando existe.</summary>
    public string? FirmaDigital { get; set; }
}

/// <summary>
/// DTO interno consumido por otros microservicios (Intranet, Reparto) mediante X-Service-Key.
/// Devuelve los datos operativos esenciales de un env\u00edo para encadenar flujos.
/// </summary>
public class EnvioInternoServiceDto
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string EstadoPublico { get; set; } = string.Empty;
    public string EstadoInterno { get; set; } = string.Empty;

    public string TipoEntrega { get; set; } = "Domicilio";
    public int? OficinaOrigenId { get; set; }
    public int? OficinaDestinoId { get; set; }

    public string CodigoPostalOrigen { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;

    public string NombreDestinatario { get; set; } = string.Empty;
    public string ApellidosDestinatario { get; set; } = string.Empty;
    public string TelefonoDestinatario { get; set; } = string.Empty;
    public string? EmailDestinatario { get; set; }

    public decimal PesoKg { get; set; }
    public string Dimensiones { get; set; } = string.Empty;
    public string TipoTarifa { get; set; } = string.Empty;
    public bool Pagado { get; set; }
    public DateTime FechaCreacion { get; set; }
}

/// <summary>
/// DTO para alta presencial en oficina por parte de un OperarioOficina.
/// El operario act\u00faa como remitente operativo: la oficina origen se toma del claim del operario.
/// El env\u00edo arranca en estado RecogidoEnOrigen (ya est\u00e1 f\u00edsicamente en la oficina) y Pagado=true.
/// </summary>
public class AltaEnvioOficinaDto
{
    // Datos del paquete
    [Required]
    [Range(0.1, 30)]
    public decimal Peso { get; set; }

    [Required]
    [MaxLength(50)]
    public string Dimensiones { get; set; } = string.Empty;

    // Remitente (cliente que se persona en la oficina)
    [Required]
    [MaxLength(100)]
    public string NombreRemitente { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ApellidosRemitente { get; set; }

    [Required]
    [MaxLength(500)]
    public string Origen { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalOrigen { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string TelefonoRemitente { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? EmailRemitente { get; set; }

    [MaxLength(20)]
    public string? DniRemitente { get; set; }

    // Destinatario
    [Required]
    [MaxLength(100)]
    public string NombreDestinatario { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? ApellidosDestinatario { get; set; }

    [Required]
    [MaxLength(500)]
    public string Destino { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalDestino { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string TelefonoDestinatario { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? EmailDestinatario { get; set; }

    // Modalidad de entrega
    [Required]
    [MaxLength(20)]
    public string TipoEntrega { get; set; } = "Domicilio";

    public int? OficinaDestinoId { get; set; }

    // Cobro
    /// <summary>"Efectivo" o "TPV"</summary>
    [MaxLength(20)]
    public string MetodoCobro { get; set; } = "Efectivo";

    [MaxLength(1000)]
    public string? Observaciones { get; set; }
}
