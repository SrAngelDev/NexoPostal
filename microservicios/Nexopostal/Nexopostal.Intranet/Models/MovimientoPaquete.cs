using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.Models;

/// <summary>
/// Movimiento de un paquete entre CTAs (ruta troncal).
/// 
/// Representa el transporte de larga distancia entre las Áreas Zonales.
/// Los camiones viajan mayoritariamente de noche por rutas fijas.
/// Para destinos insulares se usa transporte aéreo o marítimo.
/// 
/// Flujo:
///   1. Programado → el paquete se clasifica y agrupa para expedición
///   2. EnTransito → el camión/avión/barco sale del CTA de origen
///   3. Recibido   → el paquete llega al CTA de destino
/// 
/// Los paquetes urgentes tienen espacio asegurado en el primer
/// transporte que salga, sin esperar a llenar el camión.
/// </summary>
public class MovimientoPaquete
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Número de expedición interno del paquete (NXI-...).
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>CTA desde el que sale el paquete</summary>
    public int CtaOrigenId { get; set; }
    public CentroTratamiento CtaOrigen { get; set; } = null!;

    /// <summary>CTA al que llega el paquete</summary>
    public int CtaDestinoId { get; set; }
    public CentroTratamiento CtaDestino { get; set; } = null!;

    /// <summary>Estado actual del movimiento</summary>
    public EstadoMovimiento Estado { get; set; } = EstadoMovimiento.Programado;

    /// <summary>
    /// Tipo de transporte:
    ///   - Terrestre: camiones nocturnos (peninsulares)
    ///   - Aéreo: avión (insular, urgentes larga distancia)
    ///   - Marítimo: barco (insular, paquetes normales)
    /// </summary>
    public TipoTransporte TipoTransporte { get; set; } = TipoTransporte.Terrestre;

    /// <summary>
    /// Si el paquete es urgente. Los urgentes tienen prioridad 
    /// de despacho y espacio garantizado en el transporte.
    /// </summary>
    public bool EsUrgente { get; set; } = false;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    /// <summary>Fecha/hora real de salida del CTA de origen</summary>
    public DateTime? FechaSalida { get; set; }

    /// <summary>Fecha/hora real de llegada al CTA de destino</summary>
    public DateTime? FechaLlegada { get; set; }

    [MaxLength(500)]
    public string? Observaciones { get; set; }
}
