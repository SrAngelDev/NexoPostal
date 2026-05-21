using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Reparto.Models;

/// <summary>
/// Vehículo perteneciente a la flota de NexoPostal.
///
/// Antes la información del vehículo vivía embebida en <see cref="Repartidor.TipoVehiculo"/>
/// y <see cref="Repartidor.MatriculaVehiculo"/>. Se promueve a entidad propia para permitir:
///   - Inventario centralizado de la flota.
///   - Reasignación de vehículos entre repartidores.
///   - Datos extra (marca, modelo, color, año, notas).
///
/// La asignación a un repartidor se mantiene en sincronía con los campos embebidos del
/// repartidor (compatibilidad con el código existente).
/// </summary>
public class Vehiculo
{
    [Key]
    public int Id { get; set; }

    /// <summary>Matrícula o identificador único del vehículo.</summary>
    [Required]
    [MaxLength(20)]
    public string Matricula { get; set; } = string.Empty;

    public TipoVehiculo Tipo { get; set; } = TipoVehiculo.Furgoneta;

    [MaxLength(60)]
    public string? Marca { get; set; }

    [MaxLength(60)]
    public string? Modelo { get; set; }

    [MaxLength(40)]
    public string? Color { get; set; }

    public int? AnioFabricacion { get; set; }

    /// <summary>
    /// Repartidor al que está actualmente asignado el vehículo.
    /// Null si está libre.
    /// </summary>
    public int? RepartidorAsignadoId { get; set; }

    /// <summary>Nombre del repartidor asignado (desnormalizado para listados).</summary>
    [MaxLength(200)]
    public string? RepartidorAsignadoNombre { get; set; }

    /// <summary>
    /// Oficina lógica donde está estacionado/asignado el vehículo (referencia al JSON de oficinas).
    /// Null si no está asignado a oficina.
    /// </summary>
    public int? OficinaJsonId { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime FechaModificacion { get; set; } = DateTime.UtcNow;

    [MaxLength(450)]
    public string? ModificadoPorUserId { get; set; }
}
