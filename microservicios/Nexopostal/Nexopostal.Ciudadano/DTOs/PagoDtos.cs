using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>
/// Petición completa para crear una sesión de pago en Stripe Checkout.
/// Incluye los datos del envío y la URL base a la que debe volver el cliente.
/// </summary>
public class CrearSesionPagoDto
{
    // Datos del paquete que afectan al cálculo del precio.

    [Required]
    [Range(0.1, 30)]
    public decimal Peso { get; set; }

    [Required]
    [MaxLength(50)]
    public string Dimensiones { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalOrigen { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalDestino { get; set; } = string.Empty;

    // Tarifa elegida por el cliente antes de pagar.

    [Required]
    [MaxLength(20)]
    public string TipoTarifa { get; set; } = string.Empty;

    // Datos del remitente.

    [Required]
    [MaxLength(100)]
    public string NombreRemitente { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ApellidosRemitente { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string TelefonoRemitente { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string EmailRemitente { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? DniRemitente { get; set; }

    [Required]
    [MaxLength(500)]
    public string DireccionOrigen { get; set; } = string.Empty;

    // Datos del destinatario.

    [Required]
    [MaxLength(100)]
    public string NombreDestinatario { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string ApellidosDestinatario { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string TelefonoDestinatario { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? EmailDestinatario { get; set; }

    [MaxLength(20)]
    public string? DniDestinatario { get; set; }

    [Required]
    [MaxLength(500)]
    public string DireccionDestino { get; set; } = string.Empty;

    // Modalidad de entrega y oficinas implicadas.

    /// <summary>
    /// Oficina origen donde el remitente entrega el paquete.
    /// Obligatoria para alta online (el cliente siempre lleva el paquete a una oficina;
    /// no realizamos recogidas a domicilio).
    /// </summary>
    [Required]
    public int? OficinaOrigenId { get; set; }

    /// <summary>"Domicilio" o "Oficina".</summary>
    [MaxLength(20)]
    public string TipoEntrega { get; set; } = "Domicilio";

    /// <summary>Oficina destino donde el destinatario recogerá el paquete (si TipoEntrega=="Oficina").</summary>
    public int? OficinaDestinoId { get; set; }

    // URL base del frontend para construir los retornos de Stripe.

    [Required]
    public string UrlBase { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta devuelta al frontend tras crear la sesión de pago.
/// </summary>
public class SesionPagoCreadaDto
{
    /// <summary>URL de Stripe Checkout a la que hay que redirigir al cliente.</summary>
    public string SessionUrl { get; set; } = string.Empty;

    /// <summary>Identificador de la sesión de Stripe, útil para verificaciones posteriores.</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Número de seguimiento reservado para el envío asociado al pago.</summary>
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>Precio final calculado por el servidor (con IVA). Coincide exactamente con lo que Stripe cobrará.</summary>
    public decimal PrecioCalculado { get; set; }

    /// <summary>Promesa de plazo que se mostrará al cliente.</summary>
    public string TiempoEntregaEstimado { get; set; } = string.Empty;

    /// <summary>Zona tarifaria que ha usado el motor de precios.</summary>
    public string Zona { get; set; } = string.Empty;

    /// <summary>Tarifa comercial finalmente aplicada.</summary>
    public string TipoTarifa { get; set; } = string.Empty;
}

/// <summary>
/// Estado resumido del pago cuando el frontend confirma si la sesión terminó correctamente.
/// </summary>
public class VerificarPagoResultadoDto
{
    /// <summary>Indica si el cobro figura como completado.</summary>
    public bool Pagado { get; set; }

    /// <summary>Número de seguimiento del envío que se intentó pagar.</summary>
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>Estado público actual del envío.</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Importe final cobrado o previsto.</summary>
    public decimal Precio { get; set; }

    /// <summary>Destino principal del envío para mostrar contexto al usuario.</summary>
    public string Destino { get; set; } = string.Empty;

    /// <summary>Tarifa comercial elegida.</summary>
    public string TipoTarifa { get; set; } = string.Empty;

    /// <summary>Plazo estimado de entrega comunicado al cliente.</summary>
    public string TiempoEntregaEstimado { get; set; } = string.Empty;

    /// <summary>Correo del remitente al que pertenece el intento de pago.</summary>
    public string EmailRemitente { get; set; } = string.Empty;

    /// <summary>Momento en que Stripe confirmó el pago, si ya existe.</summary>
    public DateTime? FechaPago { get; set; }
}

/// <summary>
/// Petición mínima para reabrir un pago pendiente de un envío existente.
/// </summary>
public class ReintentarPagoDto
{
    [Required]
    public string UrlBase { get; set; } = string.Empty;
}
