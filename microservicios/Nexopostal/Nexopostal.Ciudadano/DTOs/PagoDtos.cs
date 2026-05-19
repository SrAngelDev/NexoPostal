using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>
/// DTO para crear una sesión de pago con Stripe Checkout
/// Incluye todos los datos del envío + URL de retorno
/// </summary>
public class CrearSesionPagoDto
{
    // ===== DATOS DEL PAQUETE =====

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

    // ===== TARIFA SELECCIONADA =====

    [Required]
    [MaxLength(20)]
    public string TipoTarifa { get; set; } = string.Empty;

    // ===== REMITENTE =====

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

    // ===== DESTINATARIO =====

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

    // ===== URL BASE PARA RETORNO DE STRIPE =====

    [Required]
    public string UrlBase { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta al crear una sesión de pago
/// </summary>
public class SesionPagoCreadaDto
{
    public string SessionUrl { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string NumeroSeguimiento { get; set; } = string.Empty;
}

/// <summary>
/// Respuesta al verificar el estado de un pago
/// </summary>
public class VerificarPagoResultadoDto
{
    public bool Pagado { get; set; }
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public string Destino { get; set; } = string.Empty;
    public string TipoTarifa { get; set; } = string.Empty;
    public string TiempoEntregaEstimado { get; set; } = string.Empty;
    public string EmailRemitente { get; set; } = string.Empty;
    public DateTime? FechaPago { get; set; }
}

/// <summary>
/// DTO para reintentar un pago de un envío existente
/// </summary>
public class ReintentarPagoDto
{
    [Required]
    public string UrlBase { get; set; } = string.Empty;
}
