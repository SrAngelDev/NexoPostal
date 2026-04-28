using AspNetCore.ApiGateway;

namespace Nexopostal.Gateway.Extensions;

/// <summary>
/// Registra todas las rutas del API Gateway en el IApiOrchestrator.
/// </summary>
public static class ApiOrchestrationConfig
{
    public static void ConfigureRoutes(IApiOrchestrator orchestrator, IConfiguration config)
    {
        var microservices = config.GetSection("Microservices");

        ConfigureAuth(orchestrator, microservices);
        ConfigureEnvios(orchestrator, microservices);
        ConfigurePagos(orchestrator, microservices);
        ConfigureTarifas(orchestrator, microservices);
        ConfigureOficinas(orchestrator, microservices);
        ConfigurePerfil(orchestrator, microservices);
        ConfigureOperativa(orchestrator, microservices);
        ConfigureReparto(orchestrator, microservices);
        ConfigureCtas(orchestrator, microservices);
    }

    // AUTH (Autenticación con Identity)
    private static void ConfigureAuth(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Auth"] ?? "http://modulo-seguridad:80";

        orchestrator.AddApi("auth", url + "/")
            .AddRoute("login", GatewayVerb.POST, new RouteInfo { Path = "api/auth/login" })
            .AddRoute("register", GatewayVerb.POST, new RouteInfo { Path = "api/auth/register" })
            .AddRoute("refresh", GatewayVerb.POST, new RouteInfo { Path = "api/auth/refresh" })
            .AddRoute("me", GatewayVerb.GET, new RouteInfo { Path = "api/auth/me" })
            .AddRoute("actualizar-perfil", GatewayVerb.POST, new RouteInfo { Path = "api/auth/actualizar-perfil" })
            .AddRoute("cambiar-password", GatewayVerb.POST, new RouteInfo { Path = "api/auth/cambiar-password" });
    }

    // ENVÍOS
    private static void ConfigureEnvios(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Ciudadano"] ?? "http://modulo-ciudadano:80";

        orchestrator.AddApi("envios", url + "/")
            // Endpoints públicos / clientes
            .AddRoute("cotizar", GatewayVerb.POST, new RouteInfo { Path = "api/envios/cotizar" })
            .AddRoute("crear", GatewayVerb.POST, new RouteInfo { Path = "api/envios/crear" })
            .AddRoute("track", GatewayVerb.GET, new RouteInfo { Path = "api/envios/track/" })
            .AddRoute("mis-envios", GatewayVerb.GET, new RouteInfo { Path = "api/envios/mis-envios" })
            .AddRoute("etiqueta", GatewayVerb.GET, new RouteInfo { Path = "api/envios/etiqueta/" })
            .AddRoute("factura", GatewayVerb.GET, new RouteInfo { Path = "api/envios/factura/" })
            // Endpoints internos (intranet / driver-app) — acceso por NumeroExpedicion
            .AddRoute("interno", GatewayVerb.GET, new RouteInfo { Path = "api/envios/interno/" })
            // PUT usa routeKey distinto; el frontend llama a /envios/interno-estado/{exp}/estado
            .AddRoute("interno-estado", GatewayVerb.PUT, new RouteInfo { Path = "api/envios/interno/" });
    }

    // PAGOS
    private static void ConfigurePagos(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Ciudadano"] ?? "http://modulo-ciudadano:80";

        orchestrator.AddApi("pagos", url + "/")
            .AddRoute("crear-sesion", GatewayVerb.POST, new RouteInfo { Path = "api/pagos/crear-sesion" })
            .AddRoute("verificar", GatewayVerb.GET, new RouteInfo { Path = "api/pagos/verificar/" })
            .AddRoute("reintentar", GatewayVerb.POST, new RouteInfo { Path = "api/pagos/reintentar/" })
            .AddRoute("webhook", GatewayVerb.POST, new RouteInfo { Path = "api/pagos/webhook" });
    }

    // TARIFAS
    private static void ConfigureTarifas(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Ciudadano"] ?? "http://modulo-ciudadano:80";

        orchestrator.AddApi("tarifas", url + "/")
            .AddRoute("consultar", GatewayVerb.GET, new RouteInfo { Path = "api/tarifas/consultar" })
            .AddRoute("calcular", GatewayVerb.POST, new RouteInfo { Path = "api/tarifas/calcular" });
    }

    // OFICINAS
    private static void ConfigureOficinas(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Ciudadano"] ?? "http://modulo-ciudadano:80";

        orchestrator.AddApi("oficinas", url + "/")
            .AddRoute("buscar", GatewayVerb.GET, new RouteInfo { Path = "api/oficinas/buscar" })
            .AddRoute("listar", GatewayVerb.GET, new RouteInfo { Path = "api/oficinas" });
    }

    // PERFIL
    private static void ConfigurePerfil(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Ciudadano"] ?? "http://modulo-ciudadano:80";

        orchestrator.AddApi("perfil", url + "/")
            .AddRoute("get", GatewayVerb.GET, new RouteInfo { Path = "api/perfil" })
            .AddRoute("guardar", GatewayVerb.POST, new RouteInfo { Path = "api/perfil" })
            .AddRoute("direcciones", GatewayVerb.GET, new RouteInfo { Path = "api/perfil/direcciones" })
            .AddRoute("agregar-direccion", GatewayVerb.POST, new RouteInfo { Path = "api/perfil/direcciones" })
            .AddRoute("editar-direccion", GatewayVerb.PUT, new RouteInfo { Path = "api/perfil/direcciones/" })
            .AddRoute("eliminar-direccion", GatewayVerb.DELETE, new RouteInfo { Path = "api/perfil/direcciones/" });
    }

    // OPERATIVA (Intranet / Logística)
    private static void ConfigureOperativa(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("operativa", url + "/")
            .AddRoute("recepcion", GatewayVerb.POST, new RouteInfo { Path = "api/operativa/recepcion" })
            .AddRoute("clasificar", GatewayVerb.POST, new RouteInfo { Path = "api/operativa/clasificar" })
            .AddRoute("estado", GatewayVerb.GET, new RouteInfo { Path = "api/operativa/estado" })
            .AddRoute("inventario", GatewayVerb.GET, new RouteInfo { Path = "api/operativa/inventario" });
    }

    // REPARTO (Movilidad / Conductores)
    private static void ConfigureReparto(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Reparto"] ?? "http://modulo-reparto:80";

        orchestrator.AddApi("reparto", url + "/")
            .AddRoute("ruta", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/ruta" })
            .AddRoute("confirmar", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/confirmar" })
            .AddRoute("ubicacion", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/ubicacion" })
            .AddRoute("entregas", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/entregas" });
    }

    // CTAs (Centros de Tratamiento Automatizado — Intranet / Admin)
    private static void ConfigureCtas(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("ctas", url + "/")
            .AddRoute("listar-ctas", GatewayVerb.GET, new RouteInfo { Path = "api/ctas" })
            .AddRoute("detalle", GatewayVerb.GET, new RouteInfo { Path = "api/ctas/" })
            .AddRoute("dashboard", GatewayVerb.GET, new RouteInfo { Path = "api/ctas/" })
            .AddRoute("dashboard-global", GatewayVerb.GET, new RouteInfo { Path = "api/ctas/dashboard-global" });
    }
}
