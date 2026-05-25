namespace Nexopostal.Shared.Exceptions;

/// <summary>Excepción para representar errores de dominio cuando no se puede usar Result.</summary>
public class NotFoundException(string message) : Exception(message);

/// <summary>Excepción de validación con detalles por campo.</summary>
public class ValidationException(string message, IDictionary<string, string[]>? errors = null) : Exception(message)
{
    public IDictionary<string, string[]> Errors { get; } = errors ?? new Dictionary<string, string[]>();
}

/// <summary>Excepción de regla de negocio violada.</summary>
public class BusinessException(string message) : Exception(message);

/// <summary>Excepción de conflicto (duplicado, estado inconsistente).</summary>
public class ConflictException(string message) : Exception(message);
