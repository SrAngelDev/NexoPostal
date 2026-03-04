namespace Nexopostal.Intranet.DTOs;

// ============================================================
//  DTOs para el Historial de Estados (trazabilidad completa)
// ============================================================

/// <summary>
/// Evento de trazabilidad visible para el cliente (tracking público).
/// Se envía también vía SignalR al TrackingHub del microservicio Ciudadano.
/// </summary>
public class HistorialEventoDto
{
    /// <summary>Estado del paquete tras este evento</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Descripción legible para el cliente</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Nombre de la ubicación (ej: "CTA Madrid - Barajas")</summary>
    public string? Ubicacion { get; set; }

    /// <summary>Código de la ubicación (ej: "CTA-MAD")</summary>
    public string? UbicacionCodigo { get; set; }

    /// <summary>Tipo de ubicación (Oficina, Cta, EnReparto, Domicilio)</summary>
    public string TipoUbicacion { get; set; } = string.Empty;

    /// <summary>Fecha y hora UTC del evento</summary>
    public DateTime FechaEvento { get; set; }
}

/// <summary>
/// Evento de trazabilidad completo (vista interna para operarios).
/// Incluye datos de auditoría no visibles para el cliente.
/// </summary>
public class HistorialEventoInternoDto
{
    public int Id { get; set; }

    public string NumeroExpedicion { get; set; } = string.Empty;

    public string? NumeroSeguimiento { get; set; }

    public string Estado { get; set; } = string.Empty;

    public string? EstadoPrevio { get; set; }

    public string TipoUbicacion { get; set; } = string.Empty;

    public int? UbicacionId { get; set; }

    public string? UbicacionNombre { get; set; }

    public string? UbicacionCodigo { get; set; }

    public int? OperarioId { get; set; }

    public string? OperarioNombre { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public string? Observaciones { get; set; }

    public bool VisibleParaCliente { get; set; }

    public DateTime FechaEvento { get; set; }
}

/// <summary>
/// DTO para registrar un nuevo evento en el historial de estados.
/// Usado por servicios internos al cambiar el estado de un paquete.
/// </summary>
public class CrearHistorialEventoDto
{
    /// <summary>Número de expedición interno (NXI-...)</summary>
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>Número de seguimiento público (NX...ES), opcional</summary>
    public string? NumeroSeguimiento { get; set; }

    /// <summary>Nuevo estado del paquete</summary>
    public string Estado { get; set; } = string.Empty;

    /// <summary>Estado anterior (calculado automáticamente si no se proporciona)</summary>
    public string? EstadoPrevio { get; set; }

    /// <summary>Tipo de ubicación donde ocurre el evento</summary>
    public string TipoUbicacion { get; set; } = "Sistema";

    /// <summary>ID de la ubicación (CTA u Oficina)</summary>
    public int? UbicacionId { get; set; }

    /// <summary>Nombre descriptivo de la ubicación</summary>
    public string? UbicacionNombre { get; set; }

    /// <summary>Código de la ubicación</summary>
    public string? UbicacionCodigo { get; set; }

    /// <summary>ID del operario que genera el evento</summary>
    public int? OperarioId { get; set; }

    /// <summary>Nombre del operario</summary>
    public string? OperarioNombre { get; set; }

    /// <summary>Descripción legible para el tracking público</summary>
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Observaciones internas</summary>
    public string? Observaciones { get; set; }

    /// <summary>Si el evento es visible para el cliente</summary>
    public bool VisibleParaCliente { get; set; } = true;
}
