using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Reparto.Models;

/// <summary>
/// Ruta de reparto diaria asignada a un repartidor.
/// 
/// Una ruta agrupa todas las entregas que un repartidor debe realizar
/// en un turno. Los paquetes se recogen de la oficina de referencia
/// y se reparten a los domicilios de la zona.
/// 
/// Flujo:
///   1. Operario de oficina crea la ruta con los paquetes pendientes
///   2. Repartidor recoge los paquetes y marca la ruta como "EnCurso"
///   3. Repartidor entrega/intenta cada paquete individualmente
///   4. Al finalizar, la ruta se marca como "Completada" o "CompletadaParcial"
///   5. Paquetes no entregados se devuelven a la oficina
/// </summary>
public class RutaReparto
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Código único de la ruta: REP-{fecha}-{secuencial}
    /// Ejemplo: REP-20250601-001
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Fecha del reparto (solo fecha, sin hora)</summary>
    public DateOnly FechaReparto { get; set; }

    /// <summary>Repartidor asignado a esta ruta</summary>
    public int RepartidorId { get; set; }
    public Repartidor Repartidor { get; set; } = null!;

    /// <summary>
    /// ID de la oficina de origen (JSON) donde se recogen los paquetes
    /// </summary>
    public int OficinaOrigenJsonId { get; set; }

    /// <summary>Nombre de la oficina (desnormalizado)</summary>
    [MaxLength(200)]
    public string OficinaOrigenNombre { get; set; } = string.Empty;

    /// <summary>Estado actual de la ruta</summary>
    public EstadoRuta Estado { get; set; } = EstadoRuta.Planificada;

    /// <summary>Hora en que el repartidor salió de la oficina</summary>
    public DateTime? HoraSalida { get; set; }

    /// <summary>Hora en que el repartidor regresó a la oficina</summary>
    public DateTime? HoraRegreso { get; set; }

    /// <summary>Observaciones generales de la ruta</summary>
    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // ===== NAVEGACIÓN =====
    public ICollection<EntregaPaquete> Entregas { get; set; } = new List<EntregaPaquete>();
}
