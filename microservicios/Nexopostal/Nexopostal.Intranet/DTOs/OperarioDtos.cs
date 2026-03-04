using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.DTOs;

// ============================================================
//  DTOs para Operarios de CTA
// ============================================================

/// <summary>
/// Resumen de un operario para listados.
/// </summary>
public class OperarioResumenDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaAsignacion { get; set; }
}

/// <summary>
/// Detalle completo de un operario.
/// </summary>
public class OperarioDetalleDto
{
    public int Id { get; set; }
    public string IdentityUserId { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int CentroTratamientoId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;
    public string CtaNombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaAsignacion { get; set; }

    // Estadísticas
    public int TareasPendientes { get; set; }
    public int TareasEnProgreso { get; set; }
    public int TareasCompletadasHoy { get; set; }
}

/// <summary>
/// DTO para crear/asignar un operario a un CTA.
/// </summary>
public class CrearOperarioDto
{
    [Required]
    [MaxLength(450)]
    public string IdentityUserId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string CodigoEmpleado { get; set; } = string.Empty;

    /// <summary>Rol: "Operario", "OperarioLogistico", "OperarioJefe"</summary>
    [Required]
    public string Rol { get; set; } = string.Empty;

    [Required]
    public int CentroTratamientoId { get; set; }
}

/// <summary>
/// Información del CTA del operario autenticado.
/// </summary>
public class MiCtaInfoDto
{
    public int OperarioId { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int CtaId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;
    public string CtaNombre { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
}

/// <summary>
/// Información completa de un operario con todas sus asignaciones a CTAs.
/// Un operario puede estar asignado a uno o más CTAs.
/// </summary>
public class MisCtasInfoDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public List<CtaAsignacionDto> Ctas { get; set; } = new();
}

/// <summary>
/// Resumen de la asignación de un operario a un CTA específico.
/// </summary>
public class CtaAsignacionDto
{
    public int OperarioCtaId { get; set; }
    public int CtaId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;
    public string CtaNombre { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
}
