using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.Models;

/// <summary>
/// Registro histórico de cada cambio de estado y ubicación de un paquete.
/// 
/// Esta tabla es la "caja negra" del envío: registra cada evento que ocurre
/// a lo largo del flujo logístico completo:
///   Oficina origen → CTA origen → CTA destino → Oficina destino → Domicilio
/// 
/// Cada fila representa un evento atómico (escaneo, cambio de estado,
/// cambio de ubicación) con timestamp UTC y el operario responsable.
/// 
/// Se usa para:
///   - Tracking público del cliente (barra de progreso)
///   - Auditoría interna (qué operario hizo qué y cuándo)
///   - Dashboard del OperarioJefe (seguimiento de incidencias)
///   - Cálculo de tiempos medios de tránsito
/// </summary>
public class HistorialEstado
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
    /// Número de seguimiento público del paquete (NX...ES).
    /// Se almacena para poder notificar al cliente vía SignalR sin llamar a Ciudadano.
    /// </summary>
    [MaxLength(20)]
    public string? NumeroSeguimiento { get; set; }

    /// <summary>
    /// Estado interno del paquete DESPUÉS de este evento.
    /// Coincide con el enum EstadoInterno del microservicio Ciudadano.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Estado { get; set; } = string.Empty;

    /// <summary>
    /// Estado interno del paquete ANTES de este evento.
    /// Permite reconstruir la secuencia completa de transiciones.
    /// </summary>
    [MaxLength(50)]
    public string? EstadoPrevio { get; set; }

    /// <summary>
    /// Tipo de ubicación donde ocurrió el evento.
    /// </summary>
    public TipoUbicacion TipoUbicacion { get; set; }

    /// <summary>
    /// ID de la ubicación (CTA u Oficina) donde ocurrió el evento.
    /// Referencia a CentroTratamiento.Id o OficinaPostal.Id según TipoUbicacion.
    /// </summary>
    public int? UbicacionId { get; set; }

    /// <summary>
    /// Nombre descriptivo de la ubicación para mostrar en tracking.
    /// Se desnormaliza para evitar JOINs en consultas de tracking frecuentes.
    /// Ejemplo: "CTA Madrid - Barajas", "Oficina Correos 28919 Leganés"
    /// </summary>
    [MaxLength(200)]
    public string? UbicacionNombre { get; set; }

    /// <summary>
    /// Código de la ubicación (ej: "CTA-MAD", "OFC-28919-01").
    /// </summary>
    [MaxLength(20)]
    public string? UbicacionCodigo { get; set; }

    /// <summary>
    /// ID del operario que registró el evento (OperarioCta.Id).
    /// Null si el evento fue generado automáticamente por el sistema.
    /// </summary>
    public int? OperarioId { get; set; }
    public OperarioCta? Operario { get; set; }

    /// <summary>
    /// Nombre del operario para auditoría (desnormalizado).
    /// </summary>
    [MaxLength(200)]
    public string? OperarioNombre { get; set; }

    /// <summary>
    /// Descripción legible del evento para mostrar en el tracking público.
    /// Ejemplo: "Tu paquete ha llegado al centro de clasificación de Madrid"
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>
    /// Observaciones internas (no visibles para el cliente).
    /// </summary>
    [MaxLength(1000)]
    public string? Observaciones { get; set; }

    /// <summary>
    /// Si este evento es visible en el tracking público del cliente.
    /// Algunos eventos internos (clasificación, carga, etc.) no se muestran.
    /// </summary>
    public bool VisibleParaCliente { get; set; } = true;

    /// <summary>
    /// Fecha y hora UTC exacta del evento.
    /// </summary>
    public DateTime FechaEvento { get; set; } = DateTime.UtcNow;
}
