using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.Models;

/// <summary>
/// Representa un envío postal creado por un ciudadano.
/// Tiene dos niveles de seguimiento:
///   - Público (NumeroSeguimiento, NX...ES): visible por clientes en la web
///   - Interno (NumeroExpedicion, NXI-...): usado por operarios y repartidores en intranet/driver-app
/// </summary>
public class Envio
{
    // ===== IDENTIFICADORES =====

    /// <summary>
    /// Número de seguimiento PÚBLICO (tracking number para clientes).
    /// Se usa en el código QR y código de barras público de la etiqueta.
    /// Ejemplo: NX123456789ES
    /// </summary>
    [Key]
    [MaxLength(20)]
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>
    /// Número de expedición INTERNO (para operarios y repartidores).
    /// Se usa en el código de barras interno de la etiqueta.
    /// Ejemplo: NXI-7A3F2K9B
    /// </summary>
    [MaxLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>
    /// ID del usuario de Identity que creó el envío
    /// Puede ser null si el envío fue creado de forma anónima
    /// </summary>
    [MaxLength(450)]
    public string? IdentityUserId { get; set; }

    // ===== DATOS FÍSICOS DEL PAQUETE =====
    
    /// <summary>
    /// Peso del paquete en kilogramos
    /// </summary>
    [Range(0.1, 30)]
    public decimal PesoKg { get; set; }

    /// <summary>
    /// Dimensiones del paquete (formato: "LxAxH cm")
    /// Ejemplo: "20x15x10"
    /// </summary>
    [MaxLength(50)]
    public string Dimensiones { get; set; } = string.Empty;

    // ===== DATOS LOGÍSTICOS =====

    /// <summary>
    /// Dirección completa de origen
    /// </summary>
    [MaxLength(500)]
    public string Origen { get; set; } = string.Empty;

    /// <summary>
    /// Dirección completa de destino
    /// </summary>
    [MaxLength(500)]
    public string Destino { get; set; } = string.Empty;

    /// <summary>
    /// Código postal de destino (para enrutamiento)
    /// </summary>
    [MaxLength(10)]
    public string CodigoPostalDestino { get; set; } = string.Empty;

    // ===== ESTADOS DEL ENVÍO =====

    /// <summary>
    /// Estado PÚBLICO actual del envío — visible para el cliente en la web.
    /// Se actualiza automáticamente al cambiar el EstadoInternoActual.
    /// </summary>
    public EstadoEnvio EstadoActual { get; set; } = EstadoEnvio.Admitido;

    /// <summary>
    /// Estado INTERNO detallado del envío — solo visible en intranet y driver-app.
    /// Es el estado sobre el que trabajan operarios y repartidores.
    /// </summary>
    public EstadoInterno EstadoInternoActual { get; set; } = EstadoInterno.PendienteRecogida;

    // ===== DATOS ADMINISTRATIVOS =====

    /// <summary>
    /// Fecha de creación del envío
    /// </summary>
    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Coste calculado del envío en EUR
    /// </summary>
    [Range(0, 1000)]
    public decimal CosteCalculado { get; set; }

    /// <summary>
    /// Indica si el envío ha sido pagado
    /// </summary>
    public bool Pagado { get; set; } = false;

    /// <summary>
    /// Notas adicionales del remitente
    /// </summary>
    [MaxLength(1000)]
    public string? Observaciones { get; set; }

    // ===== DATOS DEL REMITENTE =====

    [MaxLength(100)]
    public string NombreRemitente { get; set; } = string.Empty;

    [MaxLength(150)]
    public string ApellidosRemitente { get; set; } = string.Empty;

    [MaxLength(20)]
    public string TelefonoRemitente { get; set; } = string.Empty;

    [MaxLength(200)]
    public string EmailRemitente { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DniRemitente { get; set; }

    // ===== DATOS DEL DESTINATARIO =====

    [MaxLength(100)]
    public string NombreDestinatario { get; set; } = string.Empty;

    [MaxLength(150)]
    public string ApellidosDestinatario { get; set; } = string.Empty;

    [MaxLength(20)]
    public string TelefonoDestinatario { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? EmailDestinatario { get; set; }

    [MaxLength(20)]
    public string? DniDestinatario { get; set; }

    // ===== DATOS ADICIONALES DE LOGÍSTICA =====

    [MaxLength(10)]
    public string CodigoPostalOrigen { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de tarifa seleccionada (Estandar / Premium)
    /// </summary>
    [MaxLength(20)]
    public string TipoTarifa { get; set; } = "Estandar";

    /// <summary>
    /// Tiempo de entrega estimado (ej: "24h", "48-72h")
    /// </summary>
    [MaxLength(20)]
    public string TiempoEntregaEstimado { get; set; } = string.Empty;

    // ===== DATOS DE PAGO (STRIPE) =====

    /// <summary>
    /// ID de la sesión de Stripe Checkout
    /// </summary>
    [MaxLength(500)]
    public string? StripeSessionId { get; set; }

    /// <summary>
    /// Fecha en la que se confirmó el pago
    /// </summary>
    public DateTime? FechaPago { get; set; }
}
