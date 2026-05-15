using AspNetCore.ApiGateway.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Nexopostal.Gateway.Services;

/// <summary>
/// Filtro de autorización del Gateway basado en IGatewayAuthorization
///
/// Se ejecuta ANTES de cada petición al GatewayController.
/// Recibe el apiKey y routeKey para decidir si la ruta es pública o protegida.
///
/// Flujo:
///   1. UseAuthentication() ya leyó el token JWT y pobló HttpContext.User
///   2. Este filtro se ejecuta (GatewayAuthorizeAttribute → IGatewayAuthorization)
///   3. Si la ruta es pública → deja pasar
///   4. Si la ruta es protegida y NO hay usuario autenticado → 401
///   5. Si la ruta es protegida y SÍ hay usuario → deja pasar al GatewayController
/// </summary>
public class GatewayAuthorizationService : IGatewayAuthorization
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

    public async Task AuthorizeAsync(
        AuthorizationFilterContext context,
        string apiKey,
        string routeKey,
        string verb)
    {
        // Ruta pública: no requiere autenticación
        if (PublicRoutes.Contains((apiKey.ToLower(), routeKey.ToLower())))
        {
            await Task.CompletedTask;
            return;
        }

        // Ruta protegida: verificar que el usuario está autenticado vía JWT
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new JsonResult(new
            {
                error = "Acceso denegado",
                message = "Token JWT requerido o inválido para acceder a este recurso.",
                timestamp = DateTime.UtcNow
            })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
        }

        await Task.CompletedTask;
    }
}
