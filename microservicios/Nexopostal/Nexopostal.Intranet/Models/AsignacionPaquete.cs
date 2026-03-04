using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.Models;

/// <summary>
/// Asignación de un paquete a un operario del CTA.
/// 
/// Flujo:
///   1. El OperarioLogistico escanea el paquete → crea la asignación
///   2. El Operario ve la tarea pendiente → la inicia (EnProgreso)
///   3. El Operario completa la tarea física → la marca como Completada
///   4. Los paquetes urgentes tienen prioridad (se procesan primero)
/// 
/// Cada asignación representa una tarea atómica dentro del CTA:
/// recepción, clasificación, carga, descarga o expedición.
/// </summary>
public class AsignacionPaquete
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Número de expedición interno del paquete (NXI-...).
    /// Referencia cruzada al modelo Envio del microservicio Ciudadano.
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>
    /// Operario que debe ejecutar la tarea física.
    /// </summary>
    public int OperarioAsignadoId { get; set; }
    public OperarioCta OperarioAsignado { get; set; } = null!;

    /// <summary>
    /// OperarioLogistico que creó esta asignación.
    /// </summary>
    public int AsignadoPorId { get; set; }
    public OperarioCta AsignadoPor { get; set; } = null!;

    /// <summary>
    /// CTA donde se realiza la tarea.
    /// </summary>
    public int CtaId { get; set; }
    public CentroTratamiento Cta { get; set; } = null!;

    /// <summary>
    /// Tipo de tarea a realizar (Recepcion, Clasificacion, Carga, Descarga, Expedicion).
    /// </summary>
    public TipoTarea TipoTarea { get; set; }

    /// <summary>
    /// Estado actual de la tarea.
    /// </summary>
    public EstadoTarea EstadoTarea { get; set; } = EstadoTarea.Pendiente;

    /// <summary>
    /// Los envíos urgentes tienen "pase VIP": se procesan con prioridad absoluta
    /// en los CTAs, saltando la cola FIFO de los envíos normales.
    /// </summary>
    public bool EsUrgente { get; set; } = false;

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    public DateTime FechaAsignacion { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha en que el operario inició la tarea</summary>
    public DateTime? FechaInicio { get; set; }

    /// <summary>Fecha en que el operario completó la tarea</summary>
    public DateTime? FechaCompletada { get; set; }
}
