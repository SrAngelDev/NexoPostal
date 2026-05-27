using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Nexopostal.Shared.Exceptions;
using Nexopostal.Shared.Middleware;
using Xunit;

namespace Nexopostal.Tests.Shared;

public class GlobalExceptionHandlerTests
{
    private static async Task<(int Status, JsonElement Body)> Invoke(Exception toThrow)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = "/api/x";
        ctx.Request.Method = "POST";
        var ms = new MemoryStream();
        ctx.Response.Body = ms;

        var handler = new GlobalExceptionHandler(_ => throw toThrow, NullLogger<GlobalExceptionHandler>.Instance);
        await handler.InvokeAsync(ctx);

        ms.Position = 0;
        var doc = await JsonDocument.ParseAsync(ms);
        return (ctx.Response.StatusCode, doc.RootElement.Clone());
    }

    [Fact]
    public async Task Success_NoThrow_NoModificaResponse()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(_ => Task.CompletedTask, NullLogger<GlobalExceptionHandler>.Instance);
        await handler.InvokeAsync(ctx);
        ctx.Response.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task NotFoundException_Devuelve404()
    {
        var (status, body) = await Invoke(new NotFoundException("no existe"));
        status.Should().Be(404);
        body.GetProperty("errorType").GetString().Should().Be("NotFoundError");
        body.GetProperty("message").GetString().Should().Be("no existe");
    }

    [Fact]
    public async Task ValidationException_Devuelve400ConErrores()
    {
        var errores = new Dictionary<string, string[]> { ["Nombre"] = new[] { "req" } };
        var (status, body) = await Invoke(new ValidationException("inválido", errores));
        status.Should().Be(400);
        body.GetProperty("errorType").GetString().Should().Be("ValidationError");
        body.GetProperty("errors").GetProperty("Nombre")[0].GetString().Should().Be("req");
    }

    [Fact]
    public async Task ValidationException_SinErrores_SerializaErrorsNullOmitido()
    {
        var (status, body) = await Invoke(new ValidationException("inv"));
        status.Should().Be(400);
        body.TryGetProperty("errors", out _).Should().BeFalse();
    }

    [Fact]
    public async Task BusinessException_Devuelve400()
    {
        var (status, body) = await Invoke(new BusinessException("regla"));
        status.Should().Be(400);
        body.GetProperty("errorType").GetString().Should().Be("BusinessRuleError");
    }

    [Fact]
    public async Task ConflictException_Devuelve409()
    {
        var (status, _) = await Invoke(new ConflictException("dup"));
        status.Should().Be(409);
    }

    [Fact]
    public async Task UnauthorizedAccess_Devuelve401()
    {
        var (status, body) = await Invoke(new UnauthorizedAccessException("nope"));
        status.Should().Be(401);
        body.GetProperty("message").GetString().Should().Be("No autorizado");
    }

    [Fact]
    public async Task ArgumentException_Devuelve400()
    {
        var (status, body) = await Invoke(new ArgumentException("arg malo"));
        status.Should().Be(400);
        body.GetProperty("errorType").GetString().Should().Be("ValidationError");
    }

    [Fact]
    public async Task TimeoutException_Devuelve408()
    {
        var (status, _) = await Invoke(new TimeoutException());
        status.Should().Be(408);
    }

    [Fact]
    public async Task ExcepcionGenerica_Devuelve500()
    {
        var (status, body) = await Invoke(new InvalidOperationException("boom"));
        status.Should().Be(500);
        body.GetProperty("errorType").GetString().Should().Be("InternalError");
    }

    [Fact]
    public async Task RespuestaIncluyeErrorIdYMetadata()
    {
        var (_, body) = await Invoke(new NotFoundException("x"));
        body.GetProperty("errorId").GetString()!.Length.Should().Be(8);
        body.GetProperty("path").GetString().Should().Be("/api/x");
        body.GetProperty("method").GetString().Should().Be("POST");
    }
}
