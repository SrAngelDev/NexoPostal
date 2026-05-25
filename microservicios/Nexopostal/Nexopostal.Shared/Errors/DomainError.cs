namespace Nexopostal.Shared.Errors;

/// <summary>
/// Error de dominio base para el patrón Result (Railway Oriented Programming).
/// Los errores son valores, no excepciones. Cada error tiene un código estable y un mensaje.
/// </summary>
public abstract record DomainError(string Code, string Message);

/// <summary>Recurso no encontrado.</summary>
public sealed record NotFoundError(string Code, string Message) : DomainError(Code, Message)
{
    public static NotFoundError Of(string entity, object id) =>
        new($"{entity.ToUpperInvariant()}_NOT_FOUND", $"{entity} con id '{id}' no encontrado");
}

/// <summary>Error de validación con detalles por campo.</summary>
public sealed record ValidationError(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]> ValidationErrors
) : DomainError(Code, Message)
{
    public static ValidationError Of(string message, IReadOnlyDictionary<string, string[]> errors) =>
        new("VALIDATION_ERROR", message, errors);

    public static ValidationError Of(string field, string message) =>
        new("VALIDATION_ERROR", message, new Dictionary<string, string[]> { [field] = new[] { message } });

    public static ValidationError Of(string message) =>
        new("VALIDATION_ERROR", message, new Dictionary<string, string[]>());
}

/// <summary>Conflicto (recurso ya existe, estado inconsistente, concurrencia).</summary>
public sealed record ConflictError(string Code, string Message) : DomainError(Code, Message)
{
    public static ConflictError Of(string code, string message) => new(code, message);
}

/// <summary>Regla de negocio violada.</summary>
public sealed record BusinessRuleError(string Code, string Message) : DomainError(Code, Message)
{
    public static BusinessRuleError Of(string code, string message) => new(code, message);
}

/// <summary>No autorizado (credenciales / acceso).</summary>
public sealed record UnauthorizedError(string Code, string Message) : DomainError(Code, Message)
{
    public static readonly UnauthorizedError InvalidCredentials =
        new("INVALID_CREDENTIALS", "Credenciales incorrectas");

    public static readonly UnauthorizedError Unauthorized =
        new("UNAUTHORIZED", "No autorizado");
}

/// <summary>Acceso prohibido (autenticado pero sin permiso).</summary>
public sealed record ForbiddenError(string Code, string Message) : DomainError(Code, Message)
{
    public static ForbiddenError Of(string code, string message) => new(code, message);
}

/// <summary>Error de servicio externo / infraestructura.</summary>
public sealed record InfrastructureError(string Code, string Message) : DomainError(Code, Message)
{
    public static InfrastructureError Of(string code, string message) => new(code, message);
}
