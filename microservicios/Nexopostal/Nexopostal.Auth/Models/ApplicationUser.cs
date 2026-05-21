using Microsoft.AspNetCore.Identity;

namespace NexoPostal.Auth.Models;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? CodigoEmpleado { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public Rol Rol { get; set; } = Rol.Cliente;

    /// <summary>
    /// Borrado lógico. Cuando es true el usuario queda inhabilitado:
    /// no puede iniciar sesión, no aparece en listados por defecto y se le
    /// invalida el refresh token. Se conservan datos para integridad histórica.
    /// </summary>
    public bool Eliminado { get; set; }

    /// <summary>Fecha UTC en la que se aplicó el borrado lógico.</summary>
    public DateTime? EliminadoEnUtc { get; set; }

    /// <summary>Id del administrador que aplicó el borrado lógico.</summary>
    public string? EliminadoPorId { get; set; }
}

