using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.DTOs;

// DTOs del movimiento troncal entre CTAs.

/// <summary>
/// DTO para crear un movimiento entre CTAs (ruta troncal).
/// </summary>
public class CrearMovimientoDto
{
    /// <summary>Número de expedición interno del paquete (NXI-...)</summary>
    [Required]
    [MaxLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>CTA de origen</summary>
    [Required]
    public int CtaOrigenId { get; set; }

    /// <summary>CTA de destino</summary>
    [Required]
    public int CtaDestinoId { get; set; }

    /// <summary>Tipo de transporte: "Terrestre", "Aereo", "Maritimo"</summary>
    public string TipoTransporte { get; set; } = "Terrestre";

    /// <summary>Si el paquete es urgente</summary>
    public bool EsUrgente { get; set; } = false;

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}

/// <summary>
/// Resumen de un movimiento para listados.
/// </summary>
public class MovimientoResumenDto
{
    public int Id { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string CtaOrigenCodigo { get; set; } = string.Empty;
    public string CtaDestinoCodigo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string TipoTransporte { get; set; } = string.Empty;
    public bool EsUrgente { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaSalida { get; set; }
    public DateTime? FechaLlegada { get; set; }
}

/// <summary>
/// Detalle completo de un movimiento.
/// </summary>
public class MovimientoDetalleDto
{
    public int Id { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;

    // CTA Origen
    public int CtaOrigenId { get; set; }
    public string CtaOrigenCodigo { get; set; } = string.Empty;
    public string CtaOrigenNombre { get; set; } = string.Empty;
    public string CtaOrigenArea { get; set; } = string.Empty;

    // CTA Destino
    public int CtaDestinoId { get; set; }
    public string CtaDestinoCodigo { get; set; } = string.Empty;
    public string CtaDestinoNombre { get; set; } = string.Empty;
    public string CtaDestinoArea { get; set; } = string.Empty;

    public string Estado { get; set; } = string.Empty;
    public string TipoTransporte { get; set; } = string.Empty;
    public bool EsUrgente { get; set; }
    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaSalida { get; set; }
    public DateTime? FechaLlegada { get; set; }
}
