using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>
/// Filtro usado por el buscador público de oficinas postales.
/// </summary>
public class BuscarOficinaDto
{
    /// <summary>
    /// Código postal para buscar oficinas cercanas
    /// </summary>
    [Required(ErrorMessage = "El código postal es requerido")]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "El código postal debe tener 5 dígitos")]
    public string CodigoPostal { get; set; } = string.Empty;

    /// <summary>
    /// Radio de búsqueda en kilómetros (opcional)
    /// </summary>
    [Range(1, 50, ErrorMessage = "El radio debe estar entre 1 y 50 km")]
    public int? RadioKm { get; set; }
}

/// <summary>
/// Información de una oficina que puede mostrarse en listados, mapas o selectores de envío.
/// </summary>
public class OficinaDto
{
    /// <summary>Identificador interno de la oficina.</summary>
    public int Id { get; set; }

    /// <summary>Nombre comercial o descriptivo de la oficina.</summary>
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Dirección postal completa.</summary>
    public string Direccion { get; set; } = string.Empty;

    /// <summary>Código postal de la oficina.</summary>
    public string CodigoPostal { get; set; } = string.Empty;

    /// <summary>Ciudad en la que presta servicio.</summary>
    public string Ciudad { get; set; } = string.Empty;

    /// <summary>Provincia asociada a la oficina.</summary>
    public string Provincia { get; set; } = string.Empty;

    /// <summary>Teléfono de contacto para incidencias o consultas.</summary>
    public string? Telefono { get; set; }

    /// <summary>Correo de contacto, si la oficina lo tiene publicado.</summary>
    public string? Email { get; set; }

    /// <summary>Franja horaria de apertura mostrada al cliente.</summary>
    public string Horario { get; set; } = string.Empty;

    /// <summary>Servicios destacados disponibles en esa oficina.</summary>
    public string Servicios { get; set; } = string.Empty;

    /// <summary>Indica si la oficina está operativa para admisión o recogida.</summary>
    public bool Activa { get; set; }

    /// <summary>Distancia aproximada respecto al punto buscado, cuando se calcula por proximidad.</summary>
    public double? DistanciaKm { get; set; }

    /// <summary>Latitud usada para mostrar la oficina en mapa.</summary>
    public double? Latitud { get; set; }

    /// <summary>Longitud usada para mostrar la oficina en mapa.</summary>
    public double? Longitud { get; set; }
}
