using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.Models;

/// <summary>
/// Operario asignado a una Oficina Postal.
/// 
/// Las oficinas se cargan desde el JSON estático de oficinas reales (Data/oficinas.json),
/// NO son entidades EF. Este modelo almacena la relación operario → oficina JSON.
/// 
/// Roles en la oficina:
///   - Operario: valida entrada/salida de paquetes, escanea códigos de barras,
///     registra la recepción de paquetes entregados por clientes o repartidores.
///   - OperarioJefe: gestiona incidencias de la oficina y supervisa operaciones,
///     incluye gestión de paquetes no recogidos y coordinación con CTAs.
/// 
/// Un operario de oficina puede atender una o varias oficinas cercanas.
/// </summary>
public class OperarioOficina
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID del usuario en el servicio de autenticación (Identity).
    /// Referencia cruzada entre microservicios (sin FK a otra BD).
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string IdentityUserId { get; set; } = string.Empty;

    /// <summary>
    /// Nombre completo del operario (copia desnormalizada desde Auth).
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Código de empleado único (copia desnormalizada desde Auth).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string CodigoEmpleado { get; set; } = string.Empty;

    /// <summary>
    /// Rol del operario dentro de la oficina.
    /// Solo se permiten Operario y OperarioJefe en oficinas.
    /// </summary>
    public RolOperario Rol { get; set; }

    /// <summary>
    /// ID de la oficina en el JSON estático (Data/oficinas.json).
    /// No es una FK de EF, sino una referencia lógica al fichero JSON.
    /// Ejemplo: 1001 → "Oficina NexoPostal. Oficina Principal. MADRID"
    /// </summary>
    public int OficinaJsonId { get; set; }

    /// <summary>
    /// Nombre de la oficina (desnormalizado del JSON para consultas rápidas).
    /// </summary>
    [MaxLength(200)]
    public string OficinaNombre { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;
}
