using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Nexopostal.Shared.Cqrs;

/// <summary>
/// Instrumenta el despacho sin registrar contraseñas, tokens, direcciones ni payloads.
/// No añade transacciones externas, paralelismo ni reintentos a operaciones existentes.
/// </summary>
public sealed class CqrsExecutionBehavior<TRequest, TResponse>(ILogger<CqrsExecutionBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var kind = request is ICommand ? "Command" : "Query";
        var started = Stopwatch.GetTimestamp();
        try
        {
            var response = await next();
            logger.LogInformation("CQRS {Kind} {RequestType} completado en {ElapsedMs} ms",
                kind, typeof(TRequest).Name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            return response;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogDebug("CQRS {Kind} {RequestType} cancelado", kind, typeof(TRequest).Name);
            throw;
        }
        catch
        {
            logger.LogWarning("CQRS {Kind} {RequestType} falló tras {ElapsedMs} ms",
                kind, typeof(TRequest).Name, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
            throw;
        }
    }
}
