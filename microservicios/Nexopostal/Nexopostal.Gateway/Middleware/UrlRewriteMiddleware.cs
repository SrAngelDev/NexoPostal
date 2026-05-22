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

    // Rutas que se sirven directamente desde proxy controllers específicos
    // y NO deben reescribirse al formato /api/Gateway/{api}/{route}
    private static readonly string[] DirectProxyPaths =
    [
        "/api/nexopostal/envios/etiqueta/",
        "/api/nexopostal/envios/factura/",
        "/api/nexopostal/oficinas/",
        "/api/nexopostal/admin-usuarios",  // IDs son GUIDs (strings), no enteros
        "/api/nexopostal/admin-repartidores",  // rutas mixtas con identity/{guid} y {id}/reactivar
        "/api/nexopostal/admin-ctas",      // CRUD admin de CTAs con {id}/reactivar
        "/api/nexopostal/admin-tarifas",   // CRUD admin de tarifas (bandas de precio)
        "/api/nexopostal/admin-oficinas",  // CRUD admin de oficinas postales
        "/api/nexopostal/admin-vehiculos", // CRUD admin de vehículos (flota)
        "/api/nexopostal/admin-envios",    // Panel global de envíos (admin)
        "/api/nexopostal/admin-clientes",  // Vista 360 de clientes (admin)
        "/api/nexopostal/notificaciones",  // Broadcast notifications (admin)
        "/api/nexopostal/tarifas"          // El gateway pierde los query params en GET
    ];

    // Compatibilidad para endpoints raíz /api/{apiKey} sin routeKey explícito.
    private static readonly IReadOnlyDictionary<string, string> DefaultGetRouteKeys =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ctas"] = "listar-ctas",
            ["oficinas"] = "listar",
            ["oficinaspostales"] = "listar"
        };

    private static readonly IReadOnlyDictionary<string, string> DefaultPostRouteKeys =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["asignaciones"] = "crear",
            ["movimientos"] = "crear",
            ["incidencias"] = "crear",
            ["operarios"] = "operario-crear",
            ["historial"] = "registrar"
        };

    // Aliases por API para mantener URLs estables mientras las route keys internas son globalmente únicas.
    private static readonly IReadOnlyDictionary<string, string> RouteKeyAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["admision|interno"] = "admision-interno",
            ["asignaciones|crear"] = "asignaciones-crear",
            ["movimientos|crear"] = "movimientos-crear",
            ["incidencias|crear"] = "incidencias-crear",
            ["movimientos|cta"] = "movimientos-cta",
            ["incidencias|cta"] = "incidencias-cta",
            ["movimientos|global"] = "movimientos-global",
            ["incidencias|global"] = "incidencias-global",
            ["operarios|cta"] = "operarios-cta",
            ["movimientos|detalle"] = "movimientos-detalle",
            ["incidencias|detalle"] = "incidencias-detalle",
            ["operarios|detalle"] = "operarios-detalle",
            ["ctas|detalle"] = "ctas-detalle",
            ["oficinaspostales|detalle"] = "oficinaspostales-detalle",
            ["ctas|dashboard"] = "ctas-dashboard",
            ["movimientos|cancelar"] = "movimientos-cancelar",
            ["movimientos|paquete"] = "movimientos-paquete",
            ["incidencias|paquete"] = "incidencias-paquete",
            ["historial|interno"] = "historial-interno",
            ["oficinaspostales|listar"] = "oficinaspostales-listar",
            ["oficinaspostales|buscar"] = "oficinaspostales-buscar",
            ["oficinaspostales|resolver"] = "oficinaspostales-resolver",
            ["reparto|entregas/pendientes-asignacion"] = "entregas-pendientes-asignacion"
        };

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

        if (segments.Length == 0) return;

        if (segments.Length == 1)
        {
            var apiKeyOnly = segments[0];
            string? defaultRoute = null;

            if (string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
            {
                DefaultGetRouteKeys.TryGetValue(apiKeyOnly, out defaultRoute);
            }
            else if (string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase))
            {
                DefaultPostRouteKeys.TryGetValue(apiKeyOnly, out defaultRoute);
            }

            if (!string.IsNullOrWhiteSpace(defaultRoute))
            {
                var resolvedDefaultRoute = ResolveRouteKeyAlias(apiKeyOnly, defaultRoute);
                context.Request.Path = $"{GatewayPrefix}{apiKeyOnly}/{resolvedDefaultRoute}";
            }

            return;
        }

        var apiKey = segments[0];
        string routeKey;
        string? extra;

        // Patrón /api/{apiKey}/{numericId}
        // ej: /api/asignaciones/123 (GET) -> routeKey="detalle", parameters="123"
        //     /api/incidencias/55 (PUT) -> routeKey="actualizar", parameters="55"
        if (segments.Length == 2 && int.TryParse(segments[1], out _))
        {
            routeKey = ResolveNumericIdRouteKey(context.Request.Method);
            extra = segments[1];
        }

        // Patrón /api/{apiKey}/{numericId}/{action}
        // ej: /api/ctas/1/dashboard → routeKey="dashboard", parameters="1/dashboard"
        // Esto permite que RouteInfo { Path = "api/ctas/" } + parameters "1/dashboard"
        // resulte en upstream: api/ctas/1/dashboard
        else if (segments.Length == 3 && int.TryParse(segments[1], out _))
        {
            routeKey = segments[2];
            extra    = $"{segments[1]}/{segments[2]}";
        }

        // Patrón /api/{apiKey}/{subresource}/{subaction} (no numéricos) con alias compuesto.
        // ej: /api/reparto/entregas/pendientes-asignacion → alias "entregas-pendientes-asignacion".
        // Solo se activa si existe el alias específico, así no rompe URLs existentes.
        else if (segments.Length == 3
                 && !int.TryParse(segments[2], out _)
                 && RouteKeyAliases.ContainsKey($"{apiKey}|{segments[1]}/{segments[2]}"))
        {
            routeKey = RouteKeyAliases[$"{apiKey}|{segments[1]}/{segments[2]}"];
            extra = null;
            context.Request.Path = $"{GatewayPrefix}{apiKey}/{routeKey}";
            return;
        }

        // Patrón /api/{apiKey}/{subresource}/{numericId}/{action}
        // ej: PATCH /api/reparto/entregas/45/reasignar → routeKey="entregas-reasignar",
        // parameters="45/reasignar". Combinado con Path="api/reparto/entregas/" produce
        // upstream: api/reparto/entregas/45/reasignar
        else if (segments.Length == 4 && int.TryParse(segments[2], out _))
        {
            routeKey = $"{segments[1]}-{segments[3]}";
            extra = $"{segments[2]}/{segments[3]}";
        }
        else
        {
            routeKey = segments[1];
            extra = segments.Length > 2 ? string.Join("/", segments.Skip(2)) : null;
        }

        routeKey = ResolveRouteKeyAlias(apiKey, routeKey);

        context.Request.Path = $"{GatewayPrefix}{apiKey}/{routeKey}";

        if (extra != null)
        {
            context.Request.QueryString = context.Request.QueryString.Add("parameters", extra);
        }
    }

    private static string ResolveRouteKeyAlias(string apiKey, string routeKey)
    {
        return RouteKeyAliases.TryGetValue($"{apiKey}|{routeKey}", out var alias)
            ? alias
            : routeKey;
    }

    private static string ResolveNumericIdRouteKey(string method)
    {
        if (string.Equals(method, "PUT", StringComparison.OrdinalIgnoreCase)
            || string.Equals(method, "PATCH", StringComparison.OrdinalIgnoreCase))
            return "actualizar";

        if (string.Equals(method, "DELETE", StringComparison.OrdinalIgnoreCase))
            return "eliminar";

        return "detalle";
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
