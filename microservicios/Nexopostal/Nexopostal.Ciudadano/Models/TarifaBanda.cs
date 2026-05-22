using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Nexopostal.Ciudadano.Models;

/// <summary>
/// Serie de tarifas dentro del modelo de precios:
///  - LocalEstandar / LocalPremium: misma provincia
///  - PeninsulaEstandar / PeninsulaPremium: resto de la península; Baleares/Ceuta/Canarias se calculan como
///    PenínsulaXxx × multiplicador (constante en código por ser parámetro logístico).
/// </summary>
public enum TarifaSerie
{
    LocalEstandar = 0,
    LocalPremium = 1,
    PeninsulaEstandar = 2,
    PeninsulaPremium = 3
}

/// <summary>
/// Banda de peso editable por el Admin. Cada serie tiene 6 bandas (0..5)
/// correspondientes a los pesos: 1, 2, 5, 10, 20 y 30 kg.
/// </summary>
public class TarifaBanda
{
    public int Id { get; set; }

    public TarifaSerie Serie { get; set; }

    /// <summary>Posición de la banda dentro de la serie (0..5).</summary>
    public int OrdenBanda { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PesoHastaKg { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal PrecioBase { get; set; }

    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? ModificadoPorUserId { get; set; }
}
