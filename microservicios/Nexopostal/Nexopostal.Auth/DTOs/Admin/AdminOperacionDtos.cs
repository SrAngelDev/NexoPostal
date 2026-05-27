using System.ComponentModel.DataAnnotations;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.DTOs.Admin;

/// <summary>
/// Petición para reasignar el rol de una cuenta desde el panel de administración.
/// </summary>
public class AdminCambiarRolDto
{
    [Required]
    public Rol NuevoRol { get; set; }
}

/// <summary>
/// Petición de reseteo manual de contraseña para cuentas gestionadas por un admin.
/// </summary>
public class AdminResetPasswordDto
{
    [Required, MinLength(6)]
    public string NuevaPassword { get; set; } = string.Empty;
}

/// <summary>
/// Datos mínimos para dar de alta a un empleado interno desde la intranet de administración.
/// </summary>
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

/// <summary>
/// Permite a un administrador editar los datos operativos de un empleado existente.
/// La contraseña queda fuera porque tiene su propio flujo de reseteo.
/// </summary>
public class AdminEditarEmpleadoDto
{
    [Required]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public string? CodigoEmpleado { get; set; }

    public string? PhoneNumber { get; set; }

    [Required]
    public Rol Rol { get; set; }
}
