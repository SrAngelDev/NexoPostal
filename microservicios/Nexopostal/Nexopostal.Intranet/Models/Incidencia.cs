using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.Models;

/// <summary>
/// Incidencia reportada en un CTA, gestionada exclusivamente por el OperarioJefe.
/// 
/// El OperarioJefe es el responsable de:
///   - Detectar y registrar problemas (paquetes dañados, extraviados, etc.)
///   - Investigar la causa raíz
///   - Aplicar la resolución correspondiente
///   - Coordinar con otros CTAs si es necesario
/// 
/// Ciclo de vida: Abierta → EnRevision → Resuelta → Cerrada
/// </summary>
public class Incidencia
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Número de expedición interno del paquete afectado (NXI-...).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>CTA donde se detectó la incidencia</summary>
    public int CtaId { get; set; }
    public CentroTratamiento Cta { get; set; } = null!;

    /// <summary>OperarioJefe que reportó/gestiona la incidencia</summary>
    public int ReportadaPorId { get; set; }
    public OperarioCta ReportadaPor { get; set; } = null!;

    /// <summary>Categoría de la incidencia</summary>
    public TipoIncidencia Tipo { get; set; }

    /// <summary>Estado actual del ciclo de gestión</summary>
    public EstadoIncidencia Estado { get; set; } = EstadoIncidencia.Abierta;

    /// <summary>Descripción detallada del problema encontrado</summary>
    [Required]
    [MaxLength(2000)]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Descripción de la resolución aplicada (cuando se resuelve)</summary>
    [MaxLength(2000)]
    public string? Resolucion { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha en que se resolvió la incidencia</summary>
    public DateTime? FechaResolucion { get; set; }
}
