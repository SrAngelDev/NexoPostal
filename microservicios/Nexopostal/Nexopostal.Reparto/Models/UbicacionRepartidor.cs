using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Reparto.Models;

/// <summary>
/// Última ubicación conocida de un repartidor.
/// Se actualiza (upsert) cada vez que el repartidor envía un POST /ubicacion
/// desde la driver-app durante una ruta en curso.
///
/// El JefeReparto consulta estas ubicaciones para ver el mapa en tiempo real
/// de su equipo. Solo hay un registro por repartidor (clave única).
/// </summary>
public class UbicacionRepartidor
{
    [Key]
    public int Id { get; set; }

    /// <summary>Repartidor al que pertenece la última ubicación (único)</summary>
    public int RepartidorId { get; set; }
    public Repartidor Repartidor { get; set; } = null!;

    /// <summary>Latitud GPS</summary>
    public double Latitud { get; set; }

    /// <summary>Longitud GPS</summary>
    public double Longitud { get; set; }

    /// <summary>Ruta activa en el momento del envío (si la hay)</summary>
    public int? RutaActivaId { get; set; }

    /// <summary>Marca temporal de la última actualización (UTC)</summary>
    public DateTime ActualizadoEn { get; set; } = DateTime.UtcNow;
}
