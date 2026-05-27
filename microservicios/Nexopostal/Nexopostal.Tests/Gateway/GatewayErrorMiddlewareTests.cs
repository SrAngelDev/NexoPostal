using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Nexopostal.Gateway.Middleware;
using Xunit;

namespace Nexopostal.Tests.Gateway;

public class GatewayErrorMiddlewareTests
{
    private static GatewayErrorMiddleware Crear(RequestDelegate next) =>
        new(next, NullLogger<GatewayErrorMiddleware>.Instance);

    private static DefaultHttpContext NewCtx()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task PropagaErrorMarcadoPorHandler()
    {
        var ctx = NewCtx();
        var mw = Crear(c =>
        {
            c.Items["GatewayRealStatus"] = 404;
            c.Items["GatewayRealBody"] = "{\"code\":\"NOT_FOUND\"}";
            c.Items["GatewayRealContentType"] = "application/problem+json";
            return Task.CompletedTask;
        });

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(404);
        ctx.Response.ContentType.Should().Be("application/problem+json");
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        new StreamReader(ctx.Response.Body).ReadToEnd().Should().Contain("NOT_FOUND");
    }

    [Fact]
    public async Task SinErrorMarcado_CopiaBuffer()
    {
        var ctx = NewCtx();
        var mw = Crear(async c =>
        {
            c.Response.StatusCode = 200;
            await c.Response.WriteAsync("OK");
        });

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(200);
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        new StreamReader(ctx.Response.Body).ReadToEnd().Should().Be("OK");
    }

    [Fact]
    public async Task ExcepcionInesperada_Devuelve500()
    {
        var ctx = NewCtx();
        var mw = Crear(_ => throw new InvalidOperationException("boom"));

        await mw.InvokeAsync(ctx);

        ctx.Response.StatusCode.Should().Be(500);
        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = new StreamReader(ctx.Response.Body).ReadToEnd();
        body.Should().Contain("INTERNAL_ERROR");
    }

    [Fact]
    public async Task OperationCanceledException_NoCapturada()
    {
        var ctx = NewCtx();
        var mw = Crear(_ => throw new OperationCanceledException());
        await FluentActions.Invoking(() => mw.InvokeAsync(ctx))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public void UseGatewayErrorHandling_DevuelveMismoBuilder()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddLogging();
        var sp = services.BuildServiceProvider();
        var builder = new Microsoft.AspNetCore.Builder.ApplicationBuilder(sp);
        builder.UseGatewayErrorHandling().Should().BeSameAs(builder);
    }
}
