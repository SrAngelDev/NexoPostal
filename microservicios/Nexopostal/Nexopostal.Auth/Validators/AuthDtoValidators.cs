using FluentValidation;
using NexoPostal.Auth.DTOs;

namespace NexoPostal.Auth.Validators;

/// <summary>
/// Reglas mínimas para iniciar sesión sin dejar campos esenciales vacíos.
/// </summary>
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El formato del email no es válido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria");
    }
}

/// <summary>
/// Valida el alta pública de clientes antes de crear una nueva cuenta.
/// </summary>
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El formato del email no es válido");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres");

        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("El nombre completo es obligatorio")
            .MinimumLength(2).WithMessage("El nombre debe tener al menos 2 caracteres");
    }
}

/// <summary>
/// Revisa los datos que un usuario autenticado puede modificar en su perfil.
/// </summary>
public class ActualizarUsuarioDtoValidator : AbstractValidator<ActualizarUsuarioDto>
{
    public ActualizarUsuarioDtoValidator()
    {
        RuleFor(x => x.NombreCompleto).NotEmpty().WithMessage("El nombre completo es obligatorio");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El formato del email no es válido");
    }
}

/// <summary>
/// Controla el cambio de contraseña para que siempre llegue la clave actual y la nueva.
/// </summary>
public class CambiarPasswordDtoValidator : AbstractValidator<CambiarPasswordDto>
{
    public CambiarPasswordDtoValidator()
    {
        RuleFor(x => x.PasswordActual).NotEmpty().WithMessage("La contraseña actual es obligatoria");
        RuleFor(x => x.NuevaPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria")
            .MinimumLength(6).WithMessage("La nueva contraseña debe tener al menos 6 caracteres");
    }
}

/// <summary>
/// Garantiza que la petición de refresco incluya el token emitido previamente.
/// </summary>
public class RefreshTokenRequestDtoValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestDtoValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("El refresh token es obligatorio");
    }
}

/// <summary>
/// Comprueba que la solicitud de recuperación incluya un email válido.
/// </summary>
public class SolicitarResetPasswordDtoValidator : AbstractValidator<SolicitarResetPasswordDto>
{
    public SolicitarResetPasswordDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El email es obligatorio")
            .EmailAddress().WithMessage("El formato del email no es válido");
    }
}

/// <summary>
/// Verifica el payload usado para restablecer una contraseña desde el enlace del correo.
/// </summary>
public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty().WithMessage("El token de recuperación es obligatorio");
        RuleFor(x => x.NuevaPassword)
            .NotEmpty().WithMessage("La nueva contraseña es obligatoria")
            .MinimumLength(6).WithMessage("La contraseña debe tener al menos 6 caracteres");
    }
}
