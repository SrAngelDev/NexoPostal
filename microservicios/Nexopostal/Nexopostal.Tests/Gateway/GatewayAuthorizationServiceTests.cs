using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Nexopostal.Gateway.Services;
using System.Security.Claims;
using Xunit;

namespace Nexopostal.Tests.Gateway;

/// <summary>
/// Tests unitarios para GatewayAuthorizationService.
/// Verifica que:
///   - Rutas públicas pasan sin autenticación (sin establecer Result = 401)
///   - Rutas protegidas sin usuario autenticado → Result = 401
///   - Rutas protegidas con usuario autenticado → pasan (Result sigue null)
/// </summary>
public class GatewayAuthorizationServiceTests
{
    private static AuthorizationFilterContext CrearContexto(bool autenticado = false, string? errorCode = null)
    {
        var identity = autenticado
            ? new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, "user-id-test") },
                authenticationType: "jwt")
            : new ClaimsIdentity();

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        if (errorCode != null)
            httpContext.Items["GatewayAuthErrorCode"] = errorCode;

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
    }

    private static GatewayAuthorizationService BuildService() => new GatewayAuthorizationService();

    // ═══════════════════════════════════════════
    //  Rutas públicas: pasan sin token
    // ═══════════════════════════════════════════

    [Theory]
    [InlineData("auth", "login")]
    [InlineData("auth", "register")]
    [InlineData("auth", "refresh")]
    [InlineData("envios", "cotizar")]
    [InlineData("envios", "track")]
    [InlineData("pagos", "webhook")]
    [InlineData("tarifas", "consultar")]
    [InlineData("oficinas", "listar")]
    public async Task RutaPublica_SinAutenticacion_DeberiaPermitirAcceso(string apiKey, string routeKey)
    {
        var context = CrearContexto(autenticado: false);
        var service = BuildService();

        await service.AuthorizeAsync(context, apiKey, routeKey, "GET");

        context.Result.Should().BeNull("las rutas públicas no deben bloquear la petición");
    }

    // ═══════════════════════════════════════════
    //  Rutas protegidas sin autenticación → 401
    // ═══════════════════════════════════════════

    [Theory]
    [InlineData("ciudadano", "perfil", "GET")]
    [InlineData("intranet", "dashboard", "GET")]
    [InlineData("reparto", "rutas", "GET")]
    [InlineData("admin", "usuarios", "POST")]
    public async Task RutaProtegida_SinAutenticacion_DeberiaRetornar401(string apiKey, string routeKey, string verb)
    {
        var context = CrearContexto(autenticado: false);
        var service = BuildService();

        await service.AuthorizeAsync(context, apiKey, routeKey, verb);

        context.Result.Should().NotBeNull();
        var jsonResult = context.Result.Should().BeOfType<JsonResult>().Subject;
        jsonResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    // ═══════════════════════════════════════════
    //  Rutas protegidas con autenticación → pasan
    // ═══════════════════════════════════════════

    [Theory]
    [InlineData("ciudadano", "perfil", "GET")]
    [InlineData("reparto", "rutas", "GET")]
    public async Task RutaProtegida_UsuarioAutenticado_DeberiaPermitirAcceso(string apiKey, string routeKey, string verb)
    {
        var context = CrearContexto(autenticado: true);
        var service = BuildService();

        await service.AuthorizeAsync(context, apiKey, routeKey, verb);

        context.Result.Should().BeNull("el usuario autenticado debe poder acceder a rutas protegidas");
    }

    // ═══════════════════════════════════════════
    //  Usuario bloqueado → 401 con código USER_BLOCKED
    // ═══════════════════════════════════════════

    [Fact]
    public async Task RutaProtegida_UsuarioBloqueado_DeberiaRetornar401ConCodigoUserBlocked()
    {
        var context = CrearContexto(autenticado: false, errorCode: "USER_BLOCKED");
        var service = BuildService();

        await service.AuthorizeAsync(context, "ciudadano", "perfil", "GET");

        context.Result.Should().NotBeNull();
        var jsonResult = context.Result.Should().BeOfType<JsonResult>().Subject;
        jsonResult.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);

        var codeProp = jsonResult.Value!.GetType().GetProperty("code");
        var codeValue = codeProp!.GetValue(jsonResult.Value) as string;
        codeValue.Should().Be("USER_BLOCKED");
    }
}
