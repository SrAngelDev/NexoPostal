using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.Models;

/// <summary>
/// Tabla de enrutamiento: relaciona prefijos de código postal con CTAs.
/// Los 2 primeros dígitos del código postal español determinan la provincia
/// y, por tanto, el CTA que debe procesar el envío en destino.
/// Ejemplo: CP "28001" → prefijo "28" → Madrid → CTA-MAD.
/// </summary>
public class RutaCta
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Primeros 2 dígitos del código postal (01-52).
    /// Identifica la provincia española de destino.
    /// </summary>
    [Required]
    [MaxLength(2)]
    public string PrefijoCp { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la provincia correspondiente al prefijo.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Provincia { get; set; } = string.Empty;

    /// <summary>
    /// CTA asignado para gestionar los envíos de esta provincia.
    /// </summary>
    public int CtaId { get; set; }
    public CentroTratamiento Cta { get; set; } = null!;
}
