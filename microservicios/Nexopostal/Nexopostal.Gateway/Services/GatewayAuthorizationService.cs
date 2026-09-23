namespace Nexopostal.Gateway.Services;

/// <summary>Contratos públicos históricos y respuesta de autenticación compartida con JWT/YARP.</summary>
public sealed class GatewayAuthorizationService
{
    /// <summary>
    /// Rutas que NO requieren token JWT.
    /// Formato: (apiKey, routeKey) en minúsculas.
    /// </summary>
    private static readonly HashSet<(string ApiKey, string RouteKey)> PublicRoutes =
    [
        // Auth: login, registro, refresh y recuperación de contraseña son públicos por definición
        ("auth", "login"),
        ("auth", "register"),
        ("auth", "refresh"),
        ("auth", "solicitar-reset"),
        ("auth", "reset-password"),

        // Envíos: cotización y tracking son de consulta pública
        ("envios", "cotizar"),
        ("envios", "track"),
        ("envios", "etiqueta"),

        // Pagos: webhook es llamado por Stripe directamente
        ("pagos", "webhook"),

        // Tarifas: consulta pública
        ("tarifas", "consultar"),
        ("tarifas", "calcular"),

        // Oficinas: consulta pública
        ("oficinas", "buscar"),
        ("oficinas", "listar"),
    ];

    public static bool IsPublic(string api, string route) =>
        PublicRoutes.Contains((api.ToLowerInvariant(), route.ToLowerInvariant()));

    public static object UnauthorizedResponse(HttpContext context)
    {
        var blocked = string.Equals(context.Items["GatewayAuthErrorCode"] as string, "USER_BLOCKED", StringComparison.OrdinalIgnoreCase);
        return new
        {
            error = blocked ? "Cuenta bloqueada" : "Acceso denegado",
            code = blocked ? "USER_BLOCKED" : "UNAUTHORIZED",
            message = blocked ? "Tu cuenta ha sido bloqueada por un administrador."
                : "Token JWT requerido o invalido para acceder a este recurso.",
            timestamp = DateTime.UtcNow
        };
    }

}
