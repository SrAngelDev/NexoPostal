using System.ComponentModel.DataAnnotations;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.DTOs.Admin;

public class AdminCambiarRolDto
{
    [Required]
    public Rol NuevoRol { get; set; }
}

public class AdminResetPasswordDto
{
    [Required, MinLength(6)]
    public string NuevaPassword { get; set; } = string.Empty;
}

public class AdminCrearEmpleadoDto
{
    [Required]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? CodigoEmpleado { get; set; }

    [Required]
    public Rol Rol { get; set; }

    [Required, MinLength(6)]
    public string Password { get; set; } = string.Empty;
}
