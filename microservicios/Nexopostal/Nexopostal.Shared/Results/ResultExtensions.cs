using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Shared.Errors;

namespace Nexopostal.Shared.Results;

/// <summary>
/// Extensiones para mapear <see cref="Result{T, DomainError}"/> y <see cref="UnitResult{DomainError}"/>
/// a respuestas HTTP <see cref="IActionResult"/> en los controladores (patrón ROP).
/// </summary>
public static class ResultExtensions
{
    /// <summary>Mapea un Result&lt;T, DomainError&gt; a IActionResult.</summary>
    public static IActionResult ToActionResult<T>(this Result<T, DomainError> result) =>
        result.IsSuccess ? new OkObjectResult(result.Value) : ToErrorResult(result.Error);

    /// <summary>Mapea un Result&lt;T, DomainError&gt; a IActionResult con un mapper de éxito personalizado.</summary>
    public static IActionResult ToActionResult<T>(
        this Result<T, DomainError> result,
        Func<T, IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : ToErrorResult(result.Error);

    /// <summary>Mapea un UnitResult&lt;DomainError&gt; a IActionResult (204 No Content en éxito).</summary>
    public static IActionResult ToActionResult(this UnitResult<DomainError> result) =>
        result.IsSuccess ? new NoContentResult() : ToErrorResult(result.Error);

    /// <summary>Mapea un UnitResult&lt;DomainError&gt; a IActionResult con un mapper de éxito personalizado.</summary>
    public static IActionResult ToActionResult(this UnitResult<DomainError> result, Func<IActionResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : ToErrorResult(result.Error);

    /// <summary>Convierte un DomainError en una respuesta HTTP con el código apropiado.</summary>
    public static IActionResult ToErrorResult(DomainError error)
    {
        var (status, payload) = MapError(error);
        return new ObjectResult(payload) { StatusCode = status };
    }

    private static (int Status, object Payload) MapError(DomainError error) => error switch
    {
        NotFoundError => (StatusCodes.Status404NotFound, BuildPayload(error, "NotFoundError")),
        ValidationError ve => (StatusCodes.Status400BadRequest, BuildPayload(error, "ValidationError", ve.ValidationErrors)),
        ConflictError => (StatusCodes.Status409Conflict, BuildPayload(error, "ConflictError")),
        BusinessRuleError => (StatusCodes.Status400BadRequest, BuildPayload(error, "BusinessRuleError")),
        UnauthorizedError => (StatusCodes.Status401Unauthorized, BuildPayload(error, "UnauthorizedError")),
        ForbiddenError => (StatusCodes.Status403Forbidden, BuildPayload(error, "ForbiddenError")),
        InfrastructureError => (StatusCodes.Status502BadGateway, BuildPayload(error, "InfrastructureError")),
        _ => (StatusCodes.Status500InternalServerError, BuildPayload(error, "InternalError"))
    };

    private static object BuildPayload(DomainError error, string errorType, IReadOnlyDictionary<string, string[]>? errors = null) =>
        new
        {
            errorId = Guid.NewGuid().ToString("N")[..8],
            code = error.Code,
            message = error.Message,
            errorType,
            timestamp = DateTime.UtcNow.ToString("o"),
            errors
        };
}
