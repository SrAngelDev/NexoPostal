using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.Models;

/// <summary>
/// Operario asignado a un CTA.
/// Vincula un usuario del servicio de autenticación (Identity) con un
/// Centro de Tratamiento Automatizado y un rol operativo.
/// 
/// Roles:
///   - Operario: ejecuta tareas físicas (mover paquetes de A a B)
///   - OperarioLogistico: asigna paquetes a operarios y gestiona el flujo
///   - OperarioJefe: gestiona exclusivamente las incidencias del CTA
/// </summary>
public class OperarioCta
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
    /// Se almacena aquí para evitar llamadas constantes al servicio Auth.
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    /// <summary>
    /// Código de empleado único (copia desnormalizada desde Auth).
    /// Ejemplo: "EMP-001", "EMP-002".
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string CodigoEmpleado { get; set; } = string.Empty;

    /// <summary>
    /// Rol del operario dentro del CTA.
    /// </summary>
    public RolOperario Rol { get; set; }

    /// <summary>
    /// CTA al que está asignado este operario.
    /// </summary>
    public int CentroTratamientoId { get; set; }
    public CentroTratamiento CentroTratamiento { get; set; } = null!;

    public bool Activo { get; set; } = true;

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

    // ===== NAVEGACIÓN =====

    /// <summary>Tareas asignadas a este operario (como ejecutor)</summary>
    public ICollection<AsignacionPaquete> AsignacionesRecibidas { get; set; } = new List<AsignacionPaquete>();

    /// <summary>Tareas creadas por este operario logístico (como asignador)</summary>
    public ICollection<AsignacionPaquete> AsignacionesCreadas { get; set; } = new List<AsignacionPaquete>();

    /// <summary>Incidencias reportadas por este operario jefe</summary>
    public ICollection<Incidencia> IncidenciasReportadas { get; set; } = new List<Incidencia>();
}
