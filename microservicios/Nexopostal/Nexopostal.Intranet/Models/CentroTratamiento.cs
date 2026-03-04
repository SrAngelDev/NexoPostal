using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.Models;

/// <summary>
/// Centro de Tratamiento Automatizado (CTA).
/// Nodo principal de clasificación de la red logística de NexoPostal.
/// España se divide en 7 Áreas Zonales con 17 CTAs distribuidos estratégicamente.
/// Cada CTA recibe, clasifica y expide paquetes usando cintas transportadoras
/// y arcos de lectura de código de barras automatizados.
/// </summary>
public class CentroTratamiento
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Código único del CTA (ej: "CTA-MAD", "CTA-BCN").
    /// Se imprime en las etiquetas internas de enrutamiento.
    /// </summary>
    [Required]
    [MaxLength(10)]
    public string Codigo { get; set; } = string.Empty;

    /// <summary>
    /// Nombre descriptivo del CTA (ej: "CTA Madrid - Barajas").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Nombre { get; set; } = string.Empty;

    /// <summary>
    /// Área Zonal a la que pertenece este CTA.
    /// Determina las rutas troncales de transporte.
    /// </summary>
    public AreaZonal Area { get; set; }

    [Required]
    [MaxLength(100)]
    public string Provincia { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Ciudad { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Direccion { get; set; } = string.Empty;

    [MaxLength(5)]
    public string CodigoPostal { get; set; } = string.Empty;

    /// <summary>
    /// Indica si el CTA tiene acceso a un aeropuerto para envíos aéreos.
    /// Los paquetes urgentes a larga distancia o destinos insulares
    /// se desvían a CTAs con nodo aéreo.
    /// </summary>
    public bool EsNodoAereo { get; set; } = false;

    /// <summary>
    /// Indica si el CTA tiene acceso a un puerto marítimo.
    /// Para envíos normales a Canarias, Baleares, Ceuta y Melilla.
    /// </summary>
    public bool EsNodoMaritimo { get; set; } = false;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    // ===== NAVEGACIÓN =====

    public ICollection<OperarioCta> Operarios { get; set; } = new List<OperarioCta>();
    public ICollection<RutaCta> RutasAsignadas { get; set; } = new List<RutaCta>();
    public ICollection<MovimientoPaquete> MovimientosOrigen { get; set; } = new List<MovimientoPaquete>();
    public ICollection<MovimientoPaquete> MovimientosDestino { get; set; } = new List<MovimientoPaquete>();
    public ICollection<AsignacionPaquete> Asignaciones { get; set; } = new List<AsignacionPaquete>();
    public ICollection<Incidencia> Incidencias { get; set; } = new List<Incidencia>();
}
