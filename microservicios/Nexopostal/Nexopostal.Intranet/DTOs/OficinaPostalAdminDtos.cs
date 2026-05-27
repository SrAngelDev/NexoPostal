using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Intranet.DTOs;

/// <summary>
/// DTO administrativo completo de oficina postal (incluye campos privados/admin).
/// </summary>
public class OficinaPostalAdminDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string? Provincia { get; set; }
    public string? Telefono { get; set; }
    public string? Horario { get; set; }
    public string? Servicios { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaAlta { get; set; }
    public DateTime FechaModificacion { get; set; }
    public int OperariosActivos { get; set; }
}

/// <summary>
/// Datos necesarios para dar de alta una oficina postal desde administración.
/// </summary>
public class CrearOficinaPostalDto
{
    [Required, MaxLength(250)]
    public string Nombre { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Direccion { get; set; } = string.Empty;

    [Required, MaxLength(10)]
    public string CodigoPostal { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Ciudad { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Provincia { get; set; }

    [MaxLength(20)]
    public string? Telefono { get; set; }

    [MaxLength(200)]
    public string? Horario { get; set; }

    [MaxLength(500)]
    public string? Servicios { get; set; }

    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}

/// <summary>
/// Mismo payload que la creación, reutilizado para actualizar una oficina existente.
/// </summary>
public class ActualizarOficinaPostalDto : CrearOficinaPostalDto
{
}
