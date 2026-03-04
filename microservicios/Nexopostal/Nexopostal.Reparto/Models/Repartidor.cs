using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Reparto.Models;

/// <summary>
/// Repartidor de última milla de NexoPostal.
/// 
/// Cada repartidor está asignado a una oficina postal de referencia
/// (por su ID del JSON estático) y opera en la zona de esa oficina.
/// 
/// El repartidor:
///   - Recoge paquetes de la oficina de destino
///   - Realiza la ruta de reparto a domicilio
///   - Registra entregas, ausencias, incidencias
///   - Devuelve paquetes no entregados a la oficina
/// </summary>
public class Repartidor
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID del usuario en el servicio de autenticación (Identity).
    /// Referencia cruzada entre microservicios.
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string IdentityUserId { get; set; } = string.Empty;

    /// <summary>Nombre completo (desnormalizado desde Auth)</summary>
    [Required]
    [MaxLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>Código de empleado único</summary>
    [Required]
    [MaxLength(20)]
    public string CodigoEmpleado { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto para coordinación</summary>
    [MaxLength(20)]
    public string? Telefono { get; set; }

    /// <summary>
    /// ID de la oficina de referencia en el JSON estático.
    /// El repartidor recoge paquetes de esta oficina.
    /// </summary>
    public int OficinaJsonId { get; set; }

    /// <summary>Nombre de la oficina (desnormalizado)</summary>
    [MaxLength(200)]
    public string OficinaNombre { get; set; } = string.Empty;

    /// <summary>Tipo de vehículo que utiliza</summary>
    public TipoVehiculo TipoVehiculo { get; set; } = TipoVehiculo.Furgoneta;

    /// <summary>Matrícula o identificador del vehículo</summary>
    [MaxLength(20)]
    public string? MatriculaVehiculo { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;

    // ===== NAVEGACIÓN =====
    public ICollection<RutaReparto> Rutas { get; set; } = new List<RutaReparto>();
}
