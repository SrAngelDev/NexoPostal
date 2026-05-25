using Nexopostal.Shared.Errors;

namespace NexoPostal.Auth.Errors;

/// <summary>
/// Factory de errores de dominio específicos del módulo Auth.
/// Centraliza los códigos para que tanto controladores como tests usen valores estables.
/// </summary>
public static class AuthError
{
    public static UnauthorizedError InvalidCredentials() =>
        new("INVALID_CREDENTIALS", "Credenciales incorrectas");

    public static ForbiddenError UserBlocked() =>
        new("USER_BLOCKED", "Tu cuenta esta bloqueada. Contacta con soporte para mas informacion.");

    public static NotFoundError UserNotFound(string id = "") =>
        string.IsNullOrEmpty(id)
            ? new("USER_NOT_FOUND", "Usuario no encontrado")
            : new("USER_NOT_FOUND", $"Usuario '{id}' no encontrado");

    public static ConflictError EmailAlreadyExists(string email) =>
        new("EMAIL_IN_USE", $"El email '{email}' ya está en uso por otro usuario");

    public static UnauthorizedError InvalidRefreshToken() =>
        new("INVALID_REFRESH_TOKEN", "Refresh token inválido o expirado");

    public static ValidationError WeakPassword(IEnumerable<string> errors)
    {
        var dict = new Dictionary<string, string[]>
        {
            ["password"] = errors.ToArray()
        };
        return new("PASSWORD_INVALID", "La contraseña no cumple los requisitos", dict);
    }

    public static ValidationError IdentityErrors(IEnumerable<string> errors)
    {
        var arr = errors.ToArray();
        var dict = new Dictionary<string, string[]> { ["identity"] = arr };
        return new("IDENTITY_ERROR", arr.FirstOrDefault() ?? "Error de identidad", dict);
    }

    public static BusinessRuleError ResetPasswordFailed(string detail) =>
        new("RESET_PASSWORD_FAILED", $"El enlace de recuperación no es válido o ha expirado. {detail}".Trim());

    public static BusinessRuleError CannotModifySelf(string action) =>
        new("CANNOT_MODIFY_SELF", $"No puedes {action} a ti mismo.");

    public static BusinessRuleError UserDeleted() =>
        new("USER_DELETED", "El usuario está eliminado. Restáuralo antes de modificarlo.");

    public static BusinessRuleError CannotDowngradeToClient() =>
        new("CANNOT_DOWNGRADE_TO_CLIENT", "No se puede degradar a Cliente desde la administración interna.");

    public static BusinessRuleError CannotCreateClientAsEmployee() =>
        new("CANNOT_CREATE_CLIENT_AS_EMPLOYEE", "No se puede crear un empleado con rol Cliente.");
}
