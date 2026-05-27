using System.ComponentModel.DataAnnotations;
using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>Resumen de un envío para listados administrativos.</summary>
public class AdminEnvioListItemDto
{
    /// <summary>Número público que el cliente usa para el seguimiento.</summary>
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>Número interno con el que opera la red logística.</summary>
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>Fecha en que se generó el envío.</summary>
    public DateTime FechaCreacion { get; set; }

    /// <summary>Estado público visible para el cliente.</summary>
    public EstadoEnvio EstadoActual { get; set; }

    /// <summary>Estado operativo interno usado por la red de tratamiento y reparto.</summary>
    public EstadoInterno EstadoInternoActual { get; set; }

    /// <summary>Indica si el envío consta como pagado.</summary>
    public bool Pagado { get; set; }

    /// <summary>Dirección de origen declarada por el remitente.</summary>
    public string Origen { get; set; } = string.Empty;

    /// <summary>Dirección de destino declarada para la entrega.</summary>
    public string Destino { get; set; } = string.Empty;

    /// <summary>Código postal de destino, útil para filtros y búsquedas.</summary>
    public string CodigoPostalDestino { get; set; } = string.Empty;

    /// <summary>Nombre del remitente asociado al envío.</summary>
    public string NombreRemitente { get; set; } = string.Empty;

    /// <summary>Correo del remitente para localizar rápidamente la operación.</summary>
    public string EmailRemitente { get; set; } = string.Empty;

    /// <summary>Nombre del destinatario final.</summary>
    public string NombreDestinatario { get; set; } = string.Empty;

    /// <summary>Tarifa comercial aplicada al envío.</summary>
    public string TipoTarifa { get; set; } = string.Empty;

    /// <summary>Coste calculado por el sistema para ese envío.</summary>
    public decimal CosteCalculado { get; set; }
}

/// <summary>Detalle completo de un envío para vista admin.</summary>
public class AdminEnvioDetalleDto : AdminEnvioListItemDto
{
    /// <summary>Identificador del ciudadano propietario, si el envío está ligado a una cuenta.</summary>
    public string? IdentityUserId { get; set; }

    /// <summary>Peso declarado del paquete en kilogramos.</summary>
    public decimal PesoKg { get; set; }

    /// <summary>Dimensiones informadas por el remitente.</summary>
    public string Dimensiones { get; set; } = string.Empty;

    /// <summary>Código postal del origen.</summary>
    public string CodigoPostalOrigen { get; set; } = string.Empty;

    /// <summary>Plazo comercial previsto para la entrega.</summary>
    public string TiempoEntregaEstimado { get; set; } = string.Empty;

    /// <summary>Apellidos del remitente.</summary>
    public string ApellidosRemitente { get; set; } = string.Empty;

    /// <summary>Teléfono del remitente.</summary>
    public string TelefonoRemitente { get; set; } = string.Empty;

    /// <summary>DNI del remitente, si se registró.</summary>
    public string? DniRemitente { get; set; }

    /// <summary>Apellidos del destinatario.</summary>
    public string ApellidosDestinatario { get; set; } = string.Empty;

    /// <summary>Teléfono del destinatario.</summary>
    public string TelefonoDestinatario { get; set; } = string.Empty;

    /// <summary>Correo del destinatario, si se informó.</summary>
    public string? EmailDestinatario { get; set; }

    /// <summary>DNI del destinatario, cuando se dispone de él.</summary>
    public string? DniDestinatario { get; set; }

    /// <summary>Observaciones libres registradas en el envío.</summary>
    public string? Observaciones { get; set; }

    /// <summary>Fecha de pago, si el cobro quedó confirmado.</summary>
    public DateTime? FechaPago { get; set; }
}

/// <summary>
/// Cambio manual de estado desde herramientas administrativas.
/// Permite ajustar a la vez la vista pública y el estado operativo interno.
/// </summary>
public class CambiarEstadoEnvioDto
{
    [Required]
    public EstadoEnvio EstadoPublico { get; set; }

    [Required]
    public EstadoInterno EstadoInterno { get; set; }

    [MaxLength(500)]
    public string? Motivo { get; set; }
}

/// <summary>
/// DTO auxiliar para acciones administrativas simples que solo requieren un motivo.
/// </summary>
public class AccionEnvioDto
{
    [MaxLength(500)]
    public string? Motivo { get; set; }
}
