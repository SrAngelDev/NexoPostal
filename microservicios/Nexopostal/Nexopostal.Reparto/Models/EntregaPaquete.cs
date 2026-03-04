using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Reparto.Models;

/// <summary>
/// Registro de entrega/intento de entrega de un paquete en una ruta.
/// 
/// Cada paquete en una ruta tiene uno o más registros de entrega:
///   - Intento 1: Ausente → se deja aviso
///   - Intento 2: Entregado → éxito
/// 
/// El número de expedición es la referencia cruzada con el módulo
/// de Logística (Intranet) y el módulo de Ciudadano.
/// </summary>
public class EntregaPaquete
{
    [Key]
    public int Id { get; set; }

    /// <summary>Ruta a la que pertenece esta entrega</summary>
    public int RutaRepartoId { get; set; }
    public RutaReparto RutaReparto { get; set; } = null!;

    /// <summary>
    /// Número de expedición interno (referencia cruzada con Intranet).
    /// Formato: EXP-2025-XXXXXX
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>
    /// Número de seguimiento público (referencia cruzada con Ciudadano).
    /// Formato: NP-2025-XXXXXX
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>Dirección de entrega completa</summary>
    [Required]
    [MaxLength(300)]
    public string DireccionEntrega { get; set; } = string.Empty;

    /// <summary>Código postal de destino</summary>
    [Required]
    [MaxLength(5)]
    public string CodigoPostal { get; set; } = string.Empty;

    /// <summary>Ciudad de destino</summary>
    [Required]
    [MaxLength(100)]
    public string Ciudad { get; set; } = string.Empty;

    /// <summary>Nombre del destinatario</summary>
    [Required]
    [MaxLength(200)]
    public string NombreDestinatario { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto del destinatario</summary>
    [MaxLength(20)]
    public string? TelefonoDestinatario { get; set; }

    /// <summary>Número de intento (1, 2, 3...)</summary>
    public int NumeroIntento { get; set; } = 1;

    /// <summary>Orden de entrega dentro de la ruta (optimización)</summary>
    public int OrdenEnRuta { get; set; }

    /// <summary>Estado actual de esta entrega</summary>
    public EstadoEntrega Estado { get; set; } = EstadoEntrega.Pendiente;

    /// <summary>Fecha/hora del intento de entrega</summary>
    public DateTime? FechaIntento { get; set; }

    /// <summary>Nombre de quien recibió el paquete (si fue entregado)</summary>
    [MaxLength(200)]
    public string? ReceptorNombre { get; set; }

    /// <summary>DNI del receptor (opcional, para paquetes de valor)</summary>
    [MaxLength(15)]
    public string? ReceptorDni { get; set; }

    /// <summary>Observaciones del repartidor sobre esta entrega</summary>
    [MaxLength(500)]
    public string? Observaciones { get; set; }

    /// <summary>Coordenadas GPS de donde se realizó la entrega</summary>
    public double? LatitudEntrega { get; set; }
    public double? LongitudEntrega { get; set; }

    /// <summary>Firma digital del receptor (base64 de la imagen)</summary>
    public string? FirmaDigital { get; set; }

    /// <summary>Foto de prueba de entrega (ruta del archivo)</summary>
    [MaxLength(500)]
    public string? FotoEntrega { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;
}
