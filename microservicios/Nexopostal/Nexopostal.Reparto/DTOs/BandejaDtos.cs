using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Reparto.DTOs;

/// <summary>
/// Payload del endpoint interno que la Intranet usa para registrar un paquete
/// en la bandeja del JefeReparto cuando se escanea como DisponibleParaReparto.
/// </summary>
public class RegistrarPaqueteBandejaRequestDto
{
    [Required, StringLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    [StringLength(20)]
    public string? NumeroSeguimiento { get; set; }

    [Required]
    public int CtaId { get; set; }

    [StringLength(20)]
    public string? CtaCodigo { get; set; }

    [StringLength(150)]
    public string? NombreDestinatario { get; set; }

    [StringLength(30)]
    public string? TelefonoDestinatario { get; set; }

    [StringLength(250)]
    public string? DireccionEntrega { get; set; }

    [StringLength(10)]
    public string? CodigoPostalDestino { get; set; }

    [StringLength(100)]
    public string? CiudadDestino { get; set; }

    public bool EsUrgente { get; set; }

    [StringLength(500)]
    public string? Observaciones { get; set; }
}

/// <summary>Resultado del registro/idempotencia en bandeja.</summary>
public class RegistrarPaqueteBandejaResponseDto
{
    public bool Success { get; set; }
    public bool Idempotente { get; set; }
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>Vista que ve el JefeReparto en su bandeja.</summary>
public class PaqueteBandejaDto
{
    public int Id { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public int CtaId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;
    public string NombreDestinatario { get; set; } = string.Empty;
    public string? TelefonoDestinatario { get; set; }
    public string DireccionEntrega { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;
    public string? CiudadDestino { get; set; }
    public bool EsUrgente { get; set; }
    public string? Observaciones { get; set; }
    public DateTime FechaRegistro { get; set; }
    public int? AsignadoARutaId { get; set; }
    public int? EntregaPaqueteId { get; set; }
    public DateTime? FechaAsignacion { get; set; }
}

/// <summary>Petición para añadir un pendiente a una ruta planificada.</summary>
public class AsignarPendienteARutaDto
{
    /// <summary>Ruta planificada que recibirá el paquete pendiente.</summary>
    [Required]
    public int RutaRepartoId { get; set; }

    /// <summary>Opcional: orden manual en la ruta. Si es null, se asigna al final.</summary>
    public int? OrdenEnRuta { get; set; }
}
