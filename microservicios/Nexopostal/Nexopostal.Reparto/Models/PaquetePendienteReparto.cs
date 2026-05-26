namespace Nexopostal.Reparto.Models;

/// <summary>
/// Bandeja del JefeReparto. Cada fila representa un paquete escaneado como
/// "DisponibleParaReparto" en el CTA destino y que aún no ha sido añadido a
/// ninguna ruta de reparto.
///
/// Cuando el JefeReparto crea o edita una ruta y añade el paquete, esta entrada
/// se marca como AsignadoARuta y se materializa una <see cref="EntregaPaquete"/>
/// dentro de la ruta correspondiente.
/// </summary>
public class PaquetePendienteReparto
{
    public int Id { get; set; }

    /// <summary>Número interno de expedición (NXI-...).</summary>
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>Número de seguimiento público (NXP-...).</summary>
    public string NumeroSeguimiento { get; set; } = string.Empty;

    /// <summary>CTA destino donde se escaneó el DisponibleParaReparto.</summary>
    public int CtaId { get; set; }

    /// <summary>Código humano del CTA (p.ej. CTA-BCN).</summary>
    public string CtaCodigo { get; set; } = string.Empty;

    public string NombreDestinatario { get; set; } = string.Empty;
    public string? TelefonoDestinatario { get; set; }
    public string DireccionEntrega { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;
    public string? CiudadDestino { get; set; }

    public bool EsUrgente { get; set; }
    public string? Observaciones { get; set; }

    /// <summary>Fecha de entrada en la bandeja.</summary>
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cuando el JefeReparto añade el paquete a una ruta, se rellena con el id
    /// de la ruta y la fecha de asignación; la fila permanece como histórico.
    /// </summary>
    public int? AsignadoARutaId { get; set; }
    public int? EntregaPaqueteId { get; set; }
    public DateTime? FechaAsignacion { get; set; }
    public string? AsignadoPorIdentityUserId { get; set; }
}
