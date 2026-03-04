using System.Net;
using System.Text.Json;
using Nexopostal.Gateway.Models;

namespace Nexopostal.Gateway.Middleware;

/// <summary>
/// Middleware de manejo de errores del Gateway.
///
/// Utiliza response buffering: reemplaza el stream de respuesta por un MemoryStream
/// para que el body no se envíe inmediatamente al cliente. Tras ejecutar el pipeline,
/// comprueba si ErrorPropagationHandler marcó un error en HttpContext.Items.
///
/// Si hay un error marcado, descarta la respuesta 200 "falsa" del Gateway y escribe
/// la respuesta real del microservicio con su código de estado original.
///
/// Esto soluciona el problema de que la librería AspNetCore.ApiGateway convierte
/// todos los errores backend en 500 Internal Server Error mediante su ExceptionFilter
/// interno (GatewayAsyncExceptionFilterAttribute).
/// </summary>
public class GatewayErrorMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayErrorMiddleware> _logger;

    public GatewayErrorMiddleware(RequestDelegate next, ILogger<GatewayErrorMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Reemplazar el body de la respuesta con un buffer en memoria
        var originalBody = context.Response.Body;
        using var bufferedBody = new MemoryStream();
        context.Response.Body = bufferedBody;

        try
        {
            await _next(context);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Error inesperado — restaurar stream y devolver 500 real
            _logger.LogError(ex, "Error inesperado en {Method} {Path}",
                context.Request.Method, context.Request.Path);

            context.Response.Body = originalBody;
            if (context.Response.HasStarted) throw;

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Error interno del servidor",
                status = 500,
                message = "Se produjo un error inesperado. Inténtalo de nuevo más tarde.",
                timestamp = DateTime.UtcNow
            });
            return;
        }

        // Comprobar si el ErrorPropagationHandler marcó un error del microservicio
        if (context.Items.TryGetValue("GatewayRealStatus", out var statusObj) && statusObj is int realStatus)
        {
            var realBody = context.Items["GatewayRealBody"] as string ?? "";
            var realContentType = context.Items["GatewayRealContentType"] as string ?? "application/json";

            _logger.LogWarning(
                "Propagando error real del microservicio: {StatusCode} para {Method} {Path}",
                realStatus, context.Request.Method, context.Request.Path);

            // Descartar la respuesta 200 buffereada y escribir la real
            context.Response.Body = originalBody;
            context.Response.StatusCode = realStatus;
            context.Response.ContentType = realContentType;
            context.Response.ContentLength = null;

            if (!string.IsNullOrWhiteSpace(realBody))
            {
                await context.Response.WriteAsync(realBody);
            }
            return;
        }

        // Respuesta normal — copiar el buffer al stream real
        bufferedBody.Seek(0, SeekOrigin.Begin);
        await bufferedBody.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }
}

public static class GatewayErrorMiddlewareExtensions
{
    public static IApplicationBuilder UseGatewayErrorHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GatewayErrorMiddleware>();
    }
}
