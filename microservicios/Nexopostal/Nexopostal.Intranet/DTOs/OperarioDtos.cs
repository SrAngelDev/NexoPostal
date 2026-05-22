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

/// <summary>
/// Asignación operativa de un usuario interno a un CTA (vista administración).
/// </summary>
public class AdminOperarioCtaAsignacionDto
{
    public int OperarioCtaId { get; set; }
    public int CtaId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;
    public string CtaNombre { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string RolOperativo { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaAsignacion { get; set; }
    public int TareasPendientes { get; set; }
    public int TareasEnProgreso { get; set; }
    public int TareasCompletadasHoy { get; set; }
}

/// <summary>
/// Detalle operativo por IdentityUserId para administración.
/// </summary>
public class AdminOperarioDetalleDto
{
    public string IdentityUserId { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public List<AdminOperarioCtaAsignacionDto> AsignacionesCta { get; set; } = new();
}

/// <summary>
/// Solicitud para mover la asignación de CTA de un trabajador.
/// Si el usuario aún no tiene ninguna asignación, los tres campos opcionales
/// (NombreCompleto, CodigoEmpleado, Rol) son obligatorios para crear la primera.
/// </summary>
public class AdminActualizarCtaDto
{
    /// <summary>
    /// OperarioCtaId a mover. Opcional si el usuario solo tiene una asignación activa
    /// o si todavía no tiene ninguna (primera asignación).
    /// </summary>
    public int? OperarioCtaId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int NuevoCtaId { get; set; }

    /// <summary>Nombre completo. Solo se usa cuando se crea la primera asignación.</summary>
    public string? NombreCompleto { get; set; }

    /// <summary>Código de empleado. Solo se usa cuando se crea la primera asignación.</summary>
    public string? CodigoEmpleado { get; set; }

    /// <summary>Rol operativo (OperarioOficina, OperarioCTA, Supervisor). Solo cuando es la primera asignación.</summary>
    public string? Rol { get; set; }
}
