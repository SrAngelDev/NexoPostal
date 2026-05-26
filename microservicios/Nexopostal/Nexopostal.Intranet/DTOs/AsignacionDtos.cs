using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.DTOs;

// ============================================================
//  DTOs para Asignaciones de paquetes a operarios
// ============================================================

/// <summary>
/// DTO para que el OperarioLogistico cree una asignación de tarea.
/// </summary>
public class CrearAsignacionDto
{
    /// <summary>Número de expedición interno del paquete (NXI-...)</summary>
    [Required]
    [MaxLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>ID del operario que ejecutará la tarea</summary>
    [Required]
    public int OperarioAsignadoId { get; set; }

    /// <summary>Tipo: "Recepcion", "Clasificacion", "CargaTransporte", "DescargaTransporte", "Expedicion"</summary>
    [Required]
    public string TipoTarea { get; set; } = string.Empty;

    /// <summary>Si el envío es urgente (prioridad)</summary>
    public bool EsUrgente { get; set; } = false;

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}

/// <summary>
/// DTO para reasignar una tarea pendiente o en progreso a otro OperarioCTA.
/// </summary>
public class ReasignarTareaDto
{
    /// <summary>Id del operario destino. Debe pertenecer al mismo CTA y tener rol OperarioCTA.</summary>
    [Required]
    public int NuevoOperarioId { get; set; }
}

/// <summary>
/// Resumen de una asignación para listados.
/// </summary>
public class AsignacionResumenDto
{
    public int Id { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string TipoTarea { get; set; } = string.Empty;
    public string EstadoTarea { get; set; } = string.Empty;
    public bool EsUrgente { get; set; }
    /// <summary>Id del operario al que está asignada (CTA u oficina, según corresponda).</summary>
    public int? OperarioAsignadoId { get; set; }
    public string OperarioAsignado { get; set; } = string.Empty;
    public string AsignadoPor { get; set; } = string.Empty;
    public DateTime FechaAsignacion { get; set; }
    public DateTime? FechaCompletada { get; set; }

    /// <summary>Modo de escaneo sugerido para esta tarea (derivado del TipoTarea).</summary>
    public string? ModoSugerido { get; set; }
}

/// <summary>
/// Detalle completo de una asignación.
/// </summary>
public class AsignacionDetalleDto
{
    public int Id { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string TipoTarea { get; set; } = string.Empty;
    public string EstadoTarea { get; set; } = string.Empty;
    public bool EsUrgente { get; set; }
    public string? Observaciones { get; set; }

    // Operario asignado (CTA)
    public int? OperarioAsignadoId { get; set; }
    public string OperarioAsignadoNombre { get; set; } = string.Empty;
    public string OperarioAsignadoCodigo { get; set; } = string.Empty;

    // Asignado por
    public int? AsignadoPorId { get; set; }
    public string AsignadoPorNombre { get; set; } = string.Empty;

    // CTA
    public int? CtaId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;

    // Oficina (tareas de OperarioOficina)
    public int? OperarioOficinaAsignadoId { get; set; }
    public int? OficinaJsonId { get; set; }
    public string? OficinaNombre { get; set; }

    // Fechas
    public DateTime FechaAsignacion { get; set; }
    public DateTime? FechaInicio { get; set; }
    public DateTime? FechaCompletada { get; set; }
}
