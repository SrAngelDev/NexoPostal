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
        ConfigureNominatim(orchestrator);
        ConfigureOperativa(orchestrator, microservices);
        ConfigureAdmision(orchestrator, microservices);
        ConfigureScan(orchestrator, microservices);
        ConfigureReparto(orchestrator, microservices);
        ConfigureAsignaciones(orchestrator, microservices);
        ConfigureMovimientos(orchestrator, microservices);
        ConfigureIncidencias(orchestrator, microservices);
        ConfigureHistorial(orchestrator, microservices);
        ConfigureCtas(orchestrator, microservices);
        ConfigureOperarios(orchestrator, microservices);
        ConfigureOficinasPostales(orchestrator, microservices);
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
            .AddRoute("cambiar-password", GatewayVerb.POST, new RouteInfo { Path = "api/auth/cambiar-password" })
            .AddRoute("solicitar-reset", GatewayVerb.POST, new RouteInfo { Path = "api/auth/solicitar-reset" })
            .AddRoute("reset-password", GatewayVerb.POST, new RouteInfo { Path = "api/auth/reset-password" });
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

    // NOMINATIM (Geocodificación pública para clientes-app)
    private static void ConfigureNominatim(IApiOrchestrator orchestrator)
    {
        orchestrator.AddApi("nominatim", "https://nominatim.openstreetmap.org/")
            .AddRoute("search", GatewayVerb.GET, new RouteInfo { Path = "search" })
            .AddRoute("reverse", GatewayVerb.GET, new RouteInfo { Path = "reverse" });
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

    // ADMISIÓN (Intranet / Logística)
    private static void ConfigureAdmision(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("admision", url + "/")
            .AddRoute("paquete", GatewayVerb.POST, new RouteInfo { Path = "api/admision/paquete" })
            .AddRoute("admision-interno", GatewayVerb.POST, new RouteInfo { Path = "api/admision/interno/" })
            // Alta presencial por OperarioOficina (formulario intranet-app/alta-en-oficina)
            .AddRoute("admision-oficina-alta", GatewayVerb.POST, new RouteInfo { Path = "api/admision/oficina/alta" });
    }

    // SCAN (Escaneo operativo en Intranet)
    private static void ConfigureScan(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("scan", url + "/")
            .AddRoute("modos", GatewayVerb.GET, new RouteInfo { Path = "api/scan/modos" })
            .AddRoute("procesar", GatewayVerb.POST, new RouteInfo { Path = "api/scan/procesar" })
            .AddRoute("procesar-lote", GatewayVerb.POST, new RouteInfo { Path = "api/scan/procesar-lote" });
    }

    // REPARTO (Movilidad / Conductores)
    private static void ConfigureReparto(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Reparto"] ?? "http://modulo-reparto:80";

        orchestrator.AddApi("reparto", url + "/")
            .AddRoute("mi-perfil", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/mi-perfil" })
            .AddRoute("ruta", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/ruta" })
            .AddRoute("rutas", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/rutas" })
            // La lib AspNetCore.ApiGateway no permite mismo routeKey con verbos distintos,
            // por eso el POST usa routeKey "crear-ruta" (frontend llama a /api/nexopostal/reparto/crear-ruta).
            .AddRoute("crear-ruta", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/rutas" })
            // El UrlRewriteMiddleware compone routeKey como `{segments[1]}-{segments[3]}`
            // ⇒ para /api/reparto/rutas/{id}/iniciar genera "rutas-iniciar" (plural).
            .AddRoute("rutas-iniciar", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/rutas/" })
            .AddRoute("rutas-finalizar", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/rutas/" })
            .AddRoute("rutas-detalle", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/rutas/" })
            .AddRoute("rutas-cancelar", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/rutas/" })
            .AddRoute("rutas-reactivar", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/rutas/" })
            .AddRoute("confirmar", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/confirmar" })
            .AddRoute("ubicacion", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/ubicacion" })
            .AddRoute("entregas", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/entregas" })
            .AddRoute("dashboard", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/dashboard" })
            .AddRoute("ubicaciones-activas", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/ubicaciones-activas" })
            .AddRoute("entregas-pendientes-asignacion", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/entregas/pendientes-asignacion" })
            .AddRoute("entregas-reasignar", GatewayVerb.PATCH, new RouteInfo { Path = "api/reparto/entregas/" })
            .AddRoute("repartidores", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/repartidores" })
            .AddRoute("bandeja", GatewayVerb.GET, new RouteInfo { Path = "api/reparto/bandeja" })
            // POST /api/reparto/bandeja/{id}/asignar-a-ruta — UrlRewriteMiddleware genera
            // routeKey "bandeja-asignar-a-ruta" + parameters "{id}/asignar-a-ruta".
            .AddRoute("bandeja-asignar-a-ruta", GatewayVerb.POST, new RouteInfo { Path = "api/reparto/bandeja/" });
    }

    // ASIGNACIONES (Intranet / Logistica)
    private static void ConfigureAsignaciones(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("asignaciones", url + "/")
            .AddRoute("asignaciones-crear", GatewayVerb.POST, new RouteInfo { Path = "api/asignaciones/crear" })
            .AddRoute("mis-pendientes", GatewayVerb.GET, new RouteInfo { Path = "api/asignaciones/mis-pendientes" })
            .AddRoute("mis-en-progreso", GatewayVerb.GET, new RouteInfo { Path = "api/asignaciones/mis-en-progreso" })
            .AddRoute("mis-completadas", GatewayVerb.GET, new RouteInfo { Path = "api/asignaciones/mis-completadas" })
            // Busca una tarea del operario autenticado por código de expedición (?codigo=)
            .AddRoute("asignaciones-buscar", GatewayVerb.GET, new RouteInfo { Path = "api/asignaciones/buscar" })
            // El UrlRewriteMiddleware transforma /api/asignaciones/cta/{id} → routeKey "cta-detalle"
            .AddRoute("cta-detalle", GatewayVerb.GET, new RouteInfo { Path = "api/asignaciones/cta/" })
            .AddRoute("detalle", GatewayVerb.GET, new RouteInfo { Path = "api/asignaciones/" })
            .AddRoute("iniciar", GatewayVerb.PUT, new RouteInfo { Path = "api/asignaciones/" })
            .AddRoute("completar", GatewayVerb.PUT, new RouteInfo { Path = "api/asignaciones/" })
            .AddRoute("cancelar", GatewayVerb.PUT, new RouteInfo { Path = "api/asignaciones/" })
            .AddRoute("reasignar", GatewayVerb.PUT, new RouteInfo { Path = "api/asignaciones/" });
    }

    // MOVIMIENTOS (Troncales entre CTAs)
    private static void ConfigureMovimientos(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("movimientos", url + "/")
            .AddRoute("movimientos-crear", GatewayVerb.POST, new RouteInfo { Path = "api/movimientos" })
            .AddRoute("movimientos-cta", GatewayVerb.GET, new RouteInfo { Path = "api/movimientos/cta/" })
            .AddRoute("movimientos-global", GatewayVerb.GET, new RouteInfo { Path = "api/movimientos/global" })
            .AddRoute("movimientos-detalle", GatewayVerb.GET, new RouteInfo { Path = "api/movimientos/" })
            .AddRoute("movimientos-paquete", GatewayVerb.GET, new RouteInfo { Path = "api/movimientos/paquete/" })
            .AddRoute("despachar", GatewayVerb.PUT, new RouteInfo { Path = "api/movimientos/" })
            .AddRoute("recibir", GatewayVerb.PUT, new RouteInfo { Path = "api/movimientos/" })
            .AddRoute("movimientos-cancelar", GatewayVerb.PUT, new RouteInfo { Path = "api/movimientos/" });
    }

    // INCIDENCIAS (Gestión de incidencias en CTA)
    private static void ConfigureIncidencias(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("incidencias", url + "/")
            .AddRoute("incidencias-crear", GatewayVerb.POST, new RouteInfo { Path = "api/incidencias" })
            // Incidencia automática "PaqueteFueraDeTareas" desde el modal de escaneo
            .AddRoute("incidencias-reportar-fuera-tareas", GatewayVerb.POST, new RouteInfo { Path = "api/incidencias/reportar-fuera-tareas" })
            .AddRoute("incidencias-cta", GatewayVerb.GET, new RouteInfo { Path = "api/incidencias/cta/" })
            .AddRoute("incidencias-global", GatewayVerb.GET, new RouteInfo { Path = "api/incidencias/global" })
            .AddRoute("incidencias-detalle", GatewayVerb.GET, new RouteInfo { Path = "api/incidencias/" })
            .AddRoute("incidencias-paquete", GatewayVerb.GET, new RouteInfo { Path = "api/incidencias/paquete/" })
            .AddRoute("actualizar", GatewayVerb.PUT, new RouteInfo { Path = "api/incidencias/" });
    }

    // HISTORIAL (Trazabilidad pública e interna)
    private static void ConfigureHistorial(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("historial", url + "/")
            .AddRoute("tracking", GatewayVerb.GET, new RouteInfo { Path = "api/historial/tracking/" })
            .AddRoute("historial-interno", GatewayVerb.GET, new RouteInfo { Path = "api/historial/interno/" })
            .AddRoute("ultimo", GatewayVerb.GET, new RouteInfo { Path = "api/historial/ultimo/" })
            .AddRoute("registrar", GatewayVerb.POST, new RouteInfo { Path = "api/historial" });
    }

    // OPERARIOS (Gestión de operarios de CTA — Intranet)
    private static void ConfigureOperarios(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("operarios", url + "/")
            .AddRoute("mi-cta", GatewayVerb.GET, new RouteInfo { Path = "api/operarios/mi-cta" })
            .AddRoute("mis-ctas", GatewayVerb.GET, new RouteInfo { Path = "api/operarios/mis-ctas" })
            .AddRoute("mi-oficina", GatewayVerb.GET, new RouteInfo { Path = "api/operarios/mi-oficina" })
            .AddRoute("operarios-cta", GatewayVerb.GET, new RouteInfo { Path = "api/operarios/cta/" })
            .AddRoute("operarios-detalle", GatewayVerb.GET, new RouteInfo { Path = "api/operarios/" })
            .AddRoute("operario-detalle", GatewayVerb.GET, new RouteInfo { Path = "api/operarios/" })
            .AddRoute("operario-crear", GatewayVerb.POST, new RouteInfo { Path = "api/operarios" })
            .AddRoute("eliminar", GatewayVerb.DELETE, new RouteInfo { Path = "api/operarios/" })
            .AddRoute("desactivar", GatewayVerb.DELETE, new RouteInfo { Path = "api/operarios/" });
    }

    // CTAs (Centros de Tratamiento Automatizado — Intranet / Admin)
    private static void ConfigureCtas(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("ctas", url + "/")
            .AddRoute("listar-ctas", GatewayVerb.GET, new RouteInfo { Path = "api/ctas" })
            .AddRoute("ctas-detalle", GatewayVerb.GET, new RouteInfo { Path = "api/ctas/" })
            .AddRoute("resolver", GatewayVerb.GET, new RouteInfo { Path = "api/ctas/resolver/" })
            .AddRoute("ctas-dashboard", GatewayVerb.GET, new RouteInfo { Path = "api/ctas/" })
            .AddRoute("dashboard-global", GatewayVerb.GET, new RouteInfo { Path = "api/ctas/dashboard-global" });
    }

    // OFICINAS POSTALES (JSON operativo para Intranet)
    private static void ConfigureOficinasPostales(IApiOrchestrator orchestrator, IConfigurationSection section)
    {
        var url = section["Logistica"] ?? "http://modulo-logistica:80";

        orchestrator.AddApi("oficinaspostales", url + "/")
            .AddRoute("oficinaspostales-listar", GatewayVerb.GET, new RouteInfo { Path = "api/oficinaspostales" })
            .AddRoute("oficinaspostales-buscar", GatewayVerb.GET, new RouteInfo { Path = "api/oficinaspostales/buscar" })
            .AddRoute("oficinaspostales-detalle", GatewayVerb.GET, new RouteInfo { Path = "api/oficinaspostales/" })
            .AddRoute("oficinaspostales-resolver", GatewayVerb.GET, new RouteInfo { Path = "api/oficinaspostales/resolver/" })
            .AddRoute("por-cta", GatewayVerb.GET, new RouteInfo { Path = "api/oficinaspostales/por-cta/" })
            .AddRoute("operarios", GatewayVerb.GET, new RouteInfo { Path = "api/oficinaspostales/" });
    }
}
