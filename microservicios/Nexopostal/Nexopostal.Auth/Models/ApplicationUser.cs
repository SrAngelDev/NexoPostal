using Microsoft.AspNetCore.Identity;

namespace NexoPostal.Auth.Models;

public class ApplicationUser : IdentityUser
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? CodigoEmpleado { get; set; }
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;
    public Rol Rol { get; set; } = Rol.Cliente;
}

