namespace Nexopostal.Intranet.DTOs;

// ============================================================
//  DTOs para Oficinas (datos del JSON estático Data/oficinas.json)
// ============================================================

/// <summary>
/// DTO que representa una oficina cargada desde el JSON estático.
/// Misma estructura que el DTO de Ciudadano para coherencia inter-microservicios.
/// </summary>
public class OficinaJsonDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string? Horario { get; set; }
    public string? Servicios { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
}

/// <summary>
/// DTO para la respuesta de resolución automática de oficina + CTA.
/// Dado un código postal, resuelve:
///   1. La oficina más cercana (del JSON)
///   2. El CTA que gestiona esa zona (del BD)
/// </summary>
public class ResolverOficinaCtaResponseDto
{
    // Oficina resuelta (del JSON)
    public int OficinaId { get; set; }
    public string OficinaNombre { get; set; } = string.Empty;
    public string OficinaCodigoPostal { get; set; } = string.Empty;
    public string OficinaCiudad { get; set; } = string.Empty;
    public string OficinaDireccion { get; set; } = string.Empty;

    // CTA asociado (de la BD)
    public int CtaId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;
    public string CtaNombre { get; set; } = string.Empty;
    public string AreaZonal { get; set; } = string.Empty;
}

/// <summary>
/// Resumen de un operario asignado a una oficina.
/// </summary>
public class OperarioOficinaResumenDto
{
    public int Id { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int OficinaJsonId { get; set; }
    public string OficinaNombre { get; set; } = string.Empty;
}

/// <summary>
/// DTO para asignar un operario a una oficina del JSON.
/// </summary>
public class AsignarOperarioOficinaDto
{
    public string IdentityUserId { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public int OficinaJsonId { get; set; }
}
