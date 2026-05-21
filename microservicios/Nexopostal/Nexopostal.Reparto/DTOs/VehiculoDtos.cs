using System.ComponentModel.DataAnnotations;
using Nexopostal.Reparto.Models;

namespace Nexopostal.Reparto.DTOs;

public class VehiculoDto
{
    public int Id { get; set; }
    public string Matricula { get; set; } = string.Empty;
    public TipoVehiculo Tipo { get; set; }
    public string TipoNombre => Tipo.ToString();
    public string? Marca { get; set; }
    public string? Modelo { get; set; }
    public string? Color { get; set; }
    public int? AnioFabricacion { get; set; }
    public int? RepartidorAsignadoId { get; set; }
    public string? RepartidorAsignadoNombre { get; set; }
    public int? OficinaJsonId { get; set; }
    public string? Notas { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime FechaModificacion { get; set; }
}

public class CrearVehiculoDto
{
    [Required, MaxLength(20)]
    public string Matricula { get; set; } = string.Empty;

    [Required]
    public TipoVehiculo Tipo { get; set; }

    [MaxLength(60)]
    public string? Marca { get; set; }

    [MaxLength(60)]
    public string? Modelo { get; set; }

    [MaxLength(40)]
    public string? Color { get; set; }

    public int? AnioFabricacion { get; set; }

    public int? OficinaJsonId { get; set; }

    [MaxLength(500)]
    public string? Notas { get; set; }
}

public class ActualizarVehiculoDto : CrearVehiculoDto
{
}

public class AsignarVehiculoDto
{
    /// <summary>Id del repartidor al que se asigna el vehículo. Null para desasignar.</summary>
    public int? RepartidorId { get; set; }
}

public class ImportarDesdeRepartidoresResultDto
{
    public int Importados { get; set; }
    public int Omitidos { get; set; }
    public List<string> MatriculasImportadas { get; set; } = new();
    public List<string> Mensajes { get; set; } = new();
}
