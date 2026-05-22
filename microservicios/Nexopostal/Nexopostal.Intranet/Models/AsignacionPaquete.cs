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
    /// Nullable: si la tarea va a un OperarioOficina, este campo será null
    /// y se usará <see cref="OperarioOficinaAsignadoId"/>.
    /// </summary>
    public int? OperarioAsignadoId { get; set; }
    public OperarioCta? OperarioAsignado { get; set; }

    /// <summary>
    /// OperarioLogistico que creó esta asignación.
    /// Nullable: cuando la asignación es generada automáticamente (alta presencial,
    /// orquestación), no hay logístico de origen.
    /// </summary>
    public int? AsignadoPorId { get; set; }
    public OperarioCta? AsignadoPor { get; set; }

    /// <summary>
    /// CTA donde se realiza la tarea.
    /// Nullable: tareas de oficina (SalidaOficinaACta, EntregaAlClienteEnOficina)
    /// no se ejecutan dentro de un CTA.
    /// </summary>
    public int? CtaId { get; set; }
    public CentroTratamiento? Cta { get; set; }

    // ===== Asignación a OperarioOficina (tareas de oficina postal) =====

    /// <summary>
    /// Operario de oficina que debe ejecutar la tarea (si aplica).
    /// Exclusivo con <see cref="OperarioAsignadoId"/>.
    /// </summary>
    public int? OperarioOficinaAsignadoId { get; set; }
    public OperarioOficina? OperarioOficinaAsignado { get; set; }

    /// <summary>
    /// Id de la oficina postal (OficinaJsonId) donde se realiza la tarea.
    /// </summary>
    public int? OficinaJsonId { get; set; }

    /// <summary>
    /// Nombre desnormalizado de la oficina (para listados sin lookup).
    /// </summary>
    [MaxLength(200)]
    public string? OficinaNombre { get; set; }

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
