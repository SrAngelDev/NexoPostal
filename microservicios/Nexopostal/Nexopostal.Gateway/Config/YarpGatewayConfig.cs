using System.Text.RegularExpressions;
using Nexopostal.Gateway.Services;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Nexopostal.Gateway.Extensions;

/// <summary>Routing REST por prefijo, con alias históricos independientes del transporte.</summary>
public static class YarpGatewayConfig
{
    private static readonly Dictionary<string, string> ApiClusters = new(StringComparer.OrdinalIgnoreCase)
    {
        ["auth"] = "Auth",
        ["envios"] = "Ciudadano", ["pagos"] = "Ciudadano", ["perfil"] = "Ciudadano",
        ["tarifas"] = "Ciudadano", ["oficinas"] = "Ciudadano",
        ["operativa"] = "Logistica", ["admision"] = "Logistica", ["scan"] = "Logistica",
        ["asignaciones"] = "Logistica", ["movimientos"] = "Logistica", ["incidencias"] = "Logistica",
        ["historial"] = "Logistica", ["ctas"] = "Logistica", ["operarios"] = "Logistica",
        ["oficinaspostales"] = "Logistica", ["reparto"] = "Reparto"
    };

    public static IReadOnlyList<ClusterConfig> CreateClusters(IConfiguration configuration)
    {
        var defaults = new Dictionary<string, string>
        {
            ["Auth"] = "http://modulo-seguridad:80", ["Ciudadano"] = "http://modulo-ciudadano:80",
            ["Logistica"] = "http://modulo-logistica:80", ["Reparto"] = "http://modulo-reparto:80",
            ["Nominatim"] = "https://nominatim.openstreetmap.org"
        };
        return defaults.Select(pair => new ClusterConfig
        {
            ClusterId = pair.Key,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                ["primary"] = new()
                {
                    Address = Regex.Replace(configuration[$"Microservices:{pair.Key}"] ?? pair.Value,
                        @"\$\{([^}]+)\}", match => Environment.GetEnvironmentVariable(match.Groups[1].Value) ?? match.Value)
                        .TrimEnd('/') + "/"
                }
            }
        }).ToArray();
    }

    public static IReadOnlyList<RouteConfig> CreateRoutes()
    {
        var routes = new List<RouteConfig>();
        foreach (var prefix in new[] { "/api", "/api/nexopostal" })
        {
            foreach (var (api, cluster) in ApiClusters)
                routes.Add(Route($"{prefix}/{api}/{{**remainder}}", cluster, null,
                    $"/api/{api}/{{**remainder}}", "Default", 100));

            foreach (var action in new[] { "search", "reverse" })
                routes.Add(Route($"{prefix}/nominatim/{action}", "Nominatim", "GET", $"/{action}", "Default", 100));

            // Excepciones públicas limitadas a su verbo y recurso. El resto exige JWT.
            foreach (var legacy in LegacyGatewayRoutes.All.Where(r => GatewayAuthorizationService.IsPublic(r.Api, r.Key)))
            {
                var path = legacy.Destination.TrimEnd('/');
                if (legacy.Destination.EndsWith('/')) path += "/{**remainder}";
                routes.Add(Route(prefix + path[4..], legacy.Cluster, legacy.Method, path, "Anonymous", 20));
            }

            // Un alias no modifica Request.Path ni convierte segmentos en query parameters.
            foreach (var legacy in LegacyGatewayRoutes.All.Where(r => r.Api != "nominatim"))
            {
                var keys = new[] { legacy.Key, legacy.Key.StartsWith(legacy.Api + "-", StringComparison.Ordinal)
                    ? legacy.Key[(legacy.Api.Length + 1)..] : legacy.Key }.Distinct();
                foreach (var key in keys)
                {
                    var source = $"/api/{legacy.Api}/{key}";
                    if (source.Equals(legacy.Destination.TrimEnd('/'), StringComparison.OrdinalIgnoreCase)) continue;
                    routes.Add(Route($"{prefix}/{legacy.Api}/{key}/{{**remainder}}", legacy.Cluster,
                        legacy.Method, legacy.Destination.TrimEnd('/') + "/{**remainder}",
                        GatewayAuthorizationService.IsPublic(legacy.Api, legacy.Key) ? "Anonymous" : "Default", 10));
                }
            }
        }

        // Compatibilidad con consumidores del contrato original /api/Gateway/...?...parameters=...
        foreach (var legacy in LegacyGatewayRoutes.All)
        {
            routes.Add(new RouteConfig
            {
                RouteId = $"legacy-{legacy.Api}-{legacy.Key}-{legacy.Method}", ClusterId = legacy.Cluster,
                Match = new RouteMatch { Path = $"/api/Gateway/{legacy.Api}/{legacy.Key}", Methods = [legacy.Method] },
                AuthorizationPolicy = GatewayAuthorizationService.IsPublic(legacy.Api, legacy.Key) ? "Anonymous" : "Default",
                Metadata = new Dictionary<string, string> { ["LegacyDestination"] = legacy.Destination },
                Order = 10
            });
        }
        return routes;
    }

    private static RouteConfig Route(string match, string cluster, string? method, string destination, string policy, int order) => new()
    {
        RouteId = $"{method ?? "ALL"}:{match}", ClusterId = cluster, Order = order,
        Match = new RouteMatch { Path = match, Methods = method is null ? null : [method] },
        AuthorizationPolicy = policy,
        Transforms = [new Dictionary<string, string> { ["PathPattern"] = destination }]
    };

    public static void ConfigureTransforms(TransformBuilderContext builder)
    {
        if (builder.Route.Metadata?.TryGetValue("LegacyDestination", out var destination) == true)
        {
            builder.AddRequestTransform(context =>
            {
                var suffix = context.HttpContext.Request.Query["parameters"].ToString();
                context.Path = destination.TrimEnd('/') + (string.IsNullOrEmpty(suffix) ? "" : "/" + suffix.TrimStart('/'));
                context.Query.Collection.Remove("parameters");
                return ValueTask.CompletedTask;
            });
        }
        if (builder.Route.ClusterId == "Nominatim")
            builder.AddRequestHeader("User-Agent", "NexoPostal/1.0", append: false);
    }
}
