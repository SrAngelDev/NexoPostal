using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.DTOs;

// DTOs para registrar y seguir incidencias detectadas en la operativa del CTA.

/// <summary>
/// DTO para que el OperarioJefe reporte una nueva incidencia.
/// </summary>
public class CrearIncidenciaDto
{
    /// <summary>Número de expedición interno del paquete afectado (NXI-...)</summary>
    [Required]
    [MaxLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>
    /// Tipo: "PaqueteDanado", "PaqueteExtraviado", "DireccionIncorrecta",
    ///       "PaqueteRetenido", "ErrorClasificacion", "Otra"
    /// </summary>
    [Required]
    public string Tipo { get; set; } = string.Empty;

    /// <summary>Descripción detallada del problema</summary>
    [Required]
    [MaxLength(2000)]
    public string Descripcion { get; set; } = string.Empty;
}

/// <summary>
/// DTO para que un operario reporte un escaneo de un paquete que no está en sus tareas.
/// Genera siempre una incidencia tipo PaqueteFueraDeTareas.
/// </summary>
public class ReportarFueraTareasDto
{
    [Required]
    [MaxLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Motivo { get; set; } = string.Empty;
}

/// <summary>
/// DTO para actualizar el estado de una incidencia.
/// </summary>
public class ActualizarIncidenciaDto
{
    /// <summary>
    /// Nuevo estado: "Abierta", "EnRevision", "Resuelta", "Cerrada"
    /// </summary>
    [Required]
    public string Estado { get; set; } = string.Empty;

    /// <summary>Descripción de la resolución (obligatorio si se resuelve)</summary>
    [MaxLength(2000)]
    public string? Resolucion { get; set; }
}

/// <summary>
/// Resumen de una incidencia para listados.
/// </summary>
public class IncidenciaResumenDto
{
    public int Id { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string ReportadaPor { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaResolucion { get; set; }
    // Opcional: contexto de CTA (sólo se rellena en vistas globales)
    public int? CtaId { get; set; }
    public string? CtaCodigo { get; set; }
    public string? CtaNombre { get; set; }
    public string? Descripcion { get; set; }
}

/// <summary>
/// Detalle completo de una incidencia.
/// </summary>
public class IncidenciaDetalleDto
{
    public int Id { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Resolucion { get; set; }

    // CTA
    public int CtaId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;
    public string CtaNombre { get; set; } = string.Empty;

    // Reportada por
    public int ReportadaPorId { get; set; }
    public string ReportadaPorNombre { get; set; } = string.Empty;
    public string ReportadaPorCodigo { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaResolucion { get; set; }
}
