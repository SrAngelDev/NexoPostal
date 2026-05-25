using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Nexopostal.Shared.Exceptions;

namespace Nexopostal.Shared.Middleware;

/// <summary>
/// Manejador global de excepciones. Genera respuestas HTTP consistentes y trazables.
/// Convierte excepciones de dominio en códigos HTTP apropiados y un payload JSON uniforme.
/// </summary>
public class GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N")[..8];
            logger.LogError(ex, "Excepción no manejada. ErrorId: {ErrorId}", errorId);
            await HandleExceptionAsync(context, ex, errorId);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, string errorId)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, errors, errorType) = exception switch
        {
            NotFoundException notFound => (StatusCodes.Status404NotFound, notFound.Message,
                (IDictionary<string, string[]>?)null, "NotFoundError"),
            ValidationException validation => (StatusCodes.Status400BadRequest, validation.Message,
                validation.Errors.Count > 0 ? validation.Errors : null, "ValidationError"),
            BusinessException business => (StatusCodes.Status400BadRequest, business.Message,
                (IDictionary<string, string[]>?)null, "BusinessRuleError"),
            ConflictException conflict => (StatusCodes.Status409Conflict, conflict.Message,
                (IDictionary<string, string[]>?)null, "ConflictError"),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "No autorizado",
                (IDictionary<string, string[]>?)null, "UnauthorizedError"),
            ArgumentException arg => (StatusCodes.Status400BadRequest, arg.Message,
                (IDictionary<string, string[]>?)null, "ValidationError"),
            TimeoutException => (StatusCodes.Status408RequestTimeout, "Tiempo de espera agotado",
                (IDictionary<string, string[]>?)null, "InternalError"),
            _ => (StatusCodes.Status500InternalServerError, "Ha ocurrido un error interno",
                (IDictionary<string, string[]>?)null, "InternalError")
        };

        context.Response.StatusCode = statusCode;

        var response = new
        {
            errorId,
            message,
            errorType,
            timestamp = DateTime.UtcNow.ToString("o"),
            path = context.Request.Path.Value,
            method = context.Request.Method,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, JsonOptions));
    }
}

public static class GlobalExceptionHandlerExtensions
{
    /// <summary>Registra el middleware global de manejo de excepciones.</summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app) =>
        app.UseMiddleware<GlobalExceptionHandler>();
}
