using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nexopostal.Gateway.Middleware;
using Xunit;

namespace Nexopostal.Tests.Gateway;

public class UrlRewriteMiddlewareTests
{
    private static async Task<HttpContext> RunAsync(string method, string path, string? query = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Method = method;
        ctx.Request.Path = path;
        if (query != null) ctx.Request.QueryString = new QueryString(query);
        var mw = new UrlRewriteMiddleware(_ => Task.CompletedTask);
        await mw.InvokeAsync(ctx);
        return ctx;
    }

    [Fact]
    public async Task RutaDirecta_NoSeReescribe()
    {
        var ctx = await RunAsync("GET", "/api/asignaciones/buscar", "?codigo=NXI-1");
        ctx.Request.Path.Value.Should().Be("/api/asignaciones/buscar");
        ctx.Request.QueryString.Value.Should().Be("?codigo=NXI-1");
    }

    [Fact]
    public async Task RutaDirecta_AdminUsuarios_NoSeReescribe()
    {
        var ctx = await RunAsync("GET", "/api/nexopostal/admin-usuarios/abc-123");
        ctx.Request.Path.Value.Should().Be("/api/nexopostal/admin-usuarios/abc-123");
    }

    [Fact]
    public async Task RutaGatewayPrefix_NoSeReescribe()
    {
        var ctx = await RunAsync("GET", "/api/Gateway/auth/login");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/auth/login");
    }

    [Fact]
    public async Task RutaNoApi_NoCambia()
    {
        var ctx = await RunAsync("GET", "/swagger/index.html");
        ctx.Request.Path.Value.Should().Be("/swagger/index.html");
    }

    [Fact]
    public async Task ReescribePrefijoNexopostal()
    {
        var ctx = await RunAsync("POST", "/api/nexopostal/envios/cotizar");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/envios/cotizar");
    }

    [Fact]
    public async Task RutaApiAuth_Reescribe()
    {
        var ctx = await RunAsync("POST", "/api/auth/login");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/auth/login");
    }

    [Fact]
    public async Task ApiKeyUnico_GET_AplicaDefaultRouteKey()
    {
        var ctx = await RunAsync("GET", "/api/ctas");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/ctas/listar-ctas");
    }

    [Fact]
    public async Task ApiKeyUnico_GET_Oficinas_Default()
    {
        var ctx = await RunAsync("GET", "/api/oficinas");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/oficinas/listar");
    }

    [Fact]
    public async Task ApiKeyUnico_POST_Asignaciones_AliasCompuesto()
    {
        var ctx = await RunAsync("POST", "/api/asignaciones");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/asignaciones/asignaciones-crear");
    }

    [Fact]
    public async Task ApiKeyUnico_SinDefault_NoCambia()
    {
        var ctx = await RunAsync("GET", "/api/envios");
        ctx.Request.Path.Value.Should().Be("/api/envios");
    }

    [Fact]
    public async Task NumericId_GET_RouteKeyDetalle()
    {
        var ctx = await RunAsync("GET", "/api/asignaciones/123");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/asignaciones/detalle");
        ctx.Request.QueryString.Value.Should().Contain("parameters=123");
    }

    [Fact]
    public async Task NumericId_PUT_RouteKeyActualizar()
    {
        var ctx = await RunAsync("PUT", "/api/incidencias/55");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/incidencias/actualizar");
        ctx.Request.QueryString.Value.Should().Contain("parameters=55");
    }

    [Fact]
    public async Task NumericId_PATCH_RouteKeyActualizar()
    {
        var ctx = await RunAsync("PATCH", "/api/incidencias/55");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/incidencias/actualizar");
    }

    [Fact]
    public async Task NumericId_DELETE_RouteKeyEliminar()
    {
        var ctx = await RunAsync("DELETE", "/api/asignaciones/9");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/asignaciones/eliminar");
    }

    [Fact]
    public async Task NumericIdConAction_AliasResuelto()
    {
        var ctx = await RunAsync("GET", "/api/ctas/1/dashboard");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/ctas/ctas-dashboard");
        ctx.Request.QueryString.Value.Should().Contain("parameters=1").And.Contain("dashboard");
    }

    [Fact]
    public async Task SubresourceCompuesta_AliasDirecto()
    {
        var ctx = await RunAsync("GET", "/api/reparto/entregas/pendientes-asignacion");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/reparto/entregas-pendientes-asignacion");
        ctx.Request.QueryString.HasValue.Should().BeFalse();
    }

    [Fact]
    public async Task SubresourceConIdYAction()
    {
        var ctx = await RunAsync("PATCH", "/api/reparto/entregas/45/reasignar");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/reparto/entregas-reasignar");
        ctx.Request.QueryString.Value.Should().Contain("parameters=45").And.Contain("reasignar");
    }

    [Fact]
    public async Task RutaConSegmentosExtra_JoinearComoParametros()
    {
        var ctx = await RunAsync("GET", "/api/envios/track/NXP-001");
        ctx.Request.Path.Value.Should().Be("/api/Gateway/envios/track");
        ctx.Request.QueryString.Value.Should().Contain("parameters=NXP-001");
    }

    [Fact]
    public async Task PreservaQueryStringExistente()
    {
        var ctx = await RunAsync("GET", "/api/envios/track/NXP-001", "?foo=bar");
        ctx.Request.QueryString.Value.Should().StartWith("?foo=bar");
        ctx.Request.QueryString.Value.Should().Contain("parameters=NXP-001");
    }

    [Fact]
    public async Task RutaSoloPrefix_NoCrashea()
    {
        var ctx = await RunAsync("GET", "/api/");
        ctx.Request.Path.Value.Should().Be("/api/");
    }

    [Fact]
    public async Task ExtensionMetodo_RegistraMiddleware()
    {
        // Sanity test del helper UseUrlRewrite
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var builder = new Microsoft.AspNetCore.Builder.ApplicationBuilder(sp);
        var result = builder.UseUrlRewrite();
        result.Should().BeSameAs(builder);
    }
}
