namespace Nexopostal.Gateway.Middleware;

/// <summary>
/// Middleware que reescribe las URLs de Angular al formato que espera
/// la librería AspNetCore.ApiGateway: /api/Gateway/{apiKey}/{routeKey}.
///
/// Transformaciones:
///   /api/auth/login                      → /api/Gateway/auth/login
///   /api/nexopostal/envios/cotizar       → /api/Gateway/envios/cotizar
///   /api/nexopostal/envios/track/NXP-xxx → /api/Gateway/envios/track?parameters=NXP-xxx
/// </summary>
public class UrlRewriteMiddleware
{
    private readonly RequestDelegate _next;

    private const string ApiPrefix = "/api/";
    private const string GatewayPrefix = "/api/Gateway/";
    private const string NexoPostalPrefix = "/api/nexopostal/";

    public UrlRewriteMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    // Rutas que se sirven directamente desde FileProxyController (contenido binario)
    // y NO deben reescribirse al formato /api/Gateway/{api}/{route}
    private static readonly string[] DirectProxyPaths =
    [
        "/api/nexopostal/envios/etiqueta/",
        "/api/nexopostal/envios/factura/"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (path != null
            && path.StartsWith(ApiPrefix, StringComparison.OrdinalIgnoreCase)
            && !path.StartsWith(GatewayPrefix, StringComparison.OrdinalIgnoreCase)
            && !IsDirectProxyPath(path))
        {
            RewritePath(context, path);
        }

        await _next(context);
    }

    private static bool IsDirectProxyPath(string path)
    {
        foreach (var prefix in DirectProxyPaths)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static void RewritePath(HttpContext context, string path)
    {
        var workingPath = path;

        // Eliminar prefijo /api/nexopostal/ que usa clientes-app.
        if (workingPath.StartsWith(NexoPostalPrefix, StringComparison.OrdinalIgnoreCase))
        {
            workingPath = ApiPrefix + workingPath[NexoPostalPrefix.Length..];
        }

        // Extraer segmentos: /api/{apiKey}/{routeKey}[/{parámetros...}]
        var relativePath = workingPath[ApiPrefix.Length..];
        var segments = relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length < 2) return;

        var apiKey = segments[0];
        var routeKey = segments[1];

        context.Request.Path = $"{GatewayPrefix}{apiKey}/{routeKey}";

        // Segmentos extra (ej: /track/NXP-xxx) → ?parameters=NXP-xxx
        if (segments.Length > 2)
        {
            var extra = string.Join("/", segments.Skip(2));
            context.Request.QueryString = context.Request.QueryString.Add("parameters", extra);
        }
    }
}

/// <summary>
/// Extensión para registrar el middleware en el pipeline.
/// </summary>
public static class UrlRewriteMiddlewareExtensions
{
    public static IApplicationBuilder UseUrlRewrite(this IApplicationBuilder app)
    {
        return app.UseMiddleware<UrlRewriteMiddleware>();
    }
}
