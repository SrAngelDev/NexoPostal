using System.ComponentModel.DataAnnotations;
using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>Resumen de un envío para listados administrativos.</summary>
public class AdminEnvioListItemDto
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string NumeroExpedicion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public EstadoEnvio EstadoActual { get; set; }
    public EstadoInterno EstadoInternoActual { get; set; }
    public bool Pagado { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;
    public string NombreRemitente { get; set; } = string.Empty;
    public string EmailRemitente { get; set; } = string.Empty;
    public string NombreDestinatario { get; set; } = string.Empty;
    public string TipoTarifa { get; set; } = string.Empty;
    public decimal CosteCalculado { get; set; }
}

/// <summary>Detalle completo de un envío para vista admin.</summary>
public class AdminEnvioDetalleDto : AdminEnvioListItemDto
{
    public string? IdentityUserId { get; set; }
    public decimal PesoKg { get; set; }
    public string Dimensiones { get; set; } = string.Empty;
    public string CodigoPostalOrigen { get; set; } = string.Empty;
    public string TiempoEntregaEstimado { get; set; } = string.Empty;
    public string ApellidosRemitente { get; set; } = string.Empty;
    public string TelefonoRemitente { get; set; } = string.Empty;
    public string? DniRemitente { get; set; }
    public string ApellidosDestinatario { get; set; } = string.Empty;
    public string TelefonoDestinatario { get; set; } = string.Empty;
    public string? EmailDestinatario { get; set; }
    public string? DniDestinatario { get; set; }
    public string? Observaciones { get; set; }
    public DateTime? FechaPago { get; set; }
}

public class CambiarEstadoEnvioDto
{
    [Required]
    public EstadoEnvio EstadoPublico { get; set; }

    [Required]
    public EstadoInterno EstadoInterno { get; set; }

    [MaxLength(500)]
    public string? Motivo { get; set; }
}

public class AccionEnvioDto
{
    [MaxLength(500)]
    public string? Motivo { get; set; }
}
