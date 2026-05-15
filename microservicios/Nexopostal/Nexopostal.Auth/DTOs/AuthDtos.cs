using System.ComponentModel.DataAnnotations;

namespace NexoPostal.Auth.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    public string Password { get; set; } = string.Empty;
}

public class RegisterDto
{
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres")]
    public string NombreCompleto { get; set; } = string.Empty;
}

public class TokenResponseDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime RefreshTokenExpiration { get; set; }
    public string User { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
}

public class RefreshTokenRequestDto
{
    [Required(ErrorMessage = "El refresh token es obligatorio")]
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>
/// Devuelve la información completa del usuario autenticado.
/// </summary>
public class UsuarioInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime FechaRegistro { get; set; }
    public string Rol { get; set; } = string.Empty;
}

/// <summary>
/// Datos editables del usuario (nombre, email, teléfono).
/// </summary>
public class ActualizarUsuarioDto
{
    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    public string NombreCompleto { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "El formato del teléfono no es válido")]
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Cambio de contraseña con verificación de la actual.
/// </summary>
public class CambiarPasswordDto
{
    [Required(ErrorMessage = "La contraseña actual es obligatoria")]
    public string PasswordActual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [MinLength(6, ErrorMessage = "La nueva contraseña debe tener al menos 6 caracteres")]
    public string NuevaPassword { get; set; } = string.Empty;
}

/// <summary>
/// Solicita el envío del email de recuperación de contraseña.
/// </summary>
public class SolicitarResetPasswordDto
{
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Restablece la contraseña con el token recibido por email.
/// </summary>
public class ResetPasswordDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "El token de recuperación es obligatorio")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
    public string NuevaPassword { get; set; } = string.Empty;
}

