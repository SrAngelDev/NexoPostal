using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nexopostal.Gateway.Services;
using Xunit;

namespace Nexopostal.Tests.Gateway;

/// <summary>Program.cs real con JWT y tráfico YARP a un upstream HTTP real, sin Docker.</summary>
public sealed class YarpFixture : IAsyncLifetime
{
    private const string Secret = "gateway-tests-only-secret-at-least-32-bytes-long";
    private WebApplication _upstream = null!;
    private WebApplicationFactory<GatewayAuthorizationService> _factory = null!;
    public HttpClient Client { get; private set; } = null!;
    public string Token { get; } = CreateToken("active");

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        _upstream = builder.Build();
        _upstream.Run(async context =>
        {
            if (context.Request.Path.StartsWithSegments("/api/internal/auth/session"))
            {
                await context.Response.WriteAsJsonAsync(new { activo = !context.Request.Path.Value!.Contains("blocked") });
                return;
            }
            if (context.Request.Query.TryGetValue("status", out var status))
            {
                context.Response.StatusCode = int.Parse(status.ToString());
                context.Response.Headers["X-Upstream"] = "preserved";
                context.Response.ContentType = "application/problem+json";
                if (context.Response.StatusCode != 204) await context.Response.WriteAsync("{\"code\":\"ORIGINAL_ERROR\"}");
                return;
            }
            if (context.Request.Query.ContainsKey("binary"))
            {
                context.Response.ContentType = "application/pdf";
                context.Response.Headers.ContentDisposition = "attachment; filename=etiqueta.pdf";
                await context.Response.Body.WriteAsync(new byte[] { 37, 80, 68, 70, 0, 255, 128, 13, 10 });
                return;
            }
            var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
            await context.Response.WriteAsJsonAsync(new Echo(context.Request.Method, context.Request.Path.Value!,
                context.Request.QueryString.Value ?? "", body, context.Request.Headers.Authorization.ToString(),
                context.Request.Headers["Stripe-Signature"].ToString()));
        });
        await _upstream.StartAsync();
        var config = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = Secret, ["JwtSettings:Issuer"] = "tests", ["JwtSettings:Audience"] = "tests",
            ["Cors:AllowedOrigins:0"] = "https://client.test", ["InterServiceSettings:ServiceKey"] = "test-internal-key"
        };
        foreach (var cluster in new[] { "Auth", "Ciudadano", "Logistica", "Reparto", "Nominatim" })
            config[$"Microservices:{cluster}"] = _upstream.Urls.Single();
        _factory = new WebApplicationFactory<GatewayAuthorizationService>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment("Testing");
            foreach (var (key, value) in config) host.UseSetting(key, value);
            host.ConfigureLogging(logging => logging.ClearProviders());
        });
        Client = _factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
    }

    public static string CreateToken(string user) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        "tests", "tests", [new Claim(ClaimTypes.NameIdentifier, user), new Claim(ClaimTypes.Role, "Repartidor")],
        expires: DateTime.UtcNow.AddMinutes(10),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)), SecurityAlgorithms.HmacSha256)));

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory != null) await _factory.DisposeAsync();
        if (_upstream != null) await _upstream.DisposeAsync();
    }

    public sealed record Echo(string Method, string Path, string Query, string Body, string Authorization, string StripeSignature);
}

public class YarpProxyTests(YarpFixture fixture) : IClassFixture<YarpFixture>
{
    [Theory]
    [InlineData("GET", "/api/reparto/rutas", "/api/reparto/rutas")]
    [InlineData("POST", "/api/reparto/rutas", "/api/reparto/rutas")]
    [InlineData("POST", "/api/nexopostal/reparto/crear-ruta", "/api/reparto/rutas")]
    [InlineData("GET", "/api/nexopostal/reparto/rutas/12", "/api/reparto/rutas/12")]
    [InlineData("POST", "/api/nexopostal/reparto/rutas/12/iniciar", "/api/reparto/rutas/12/iniciar")]
    [InlineData("POST", "/api/reparto/rutas/12/finalizar", "/api/reparto/rutas/12/finalizar")]
    [InlineData("POST", "/api/reparto/rutas/12/cancelar", "/api/reparto/rutas/12/cancelar")]
    [InlineData("POST", "/api/reparto/rutas/12/reactivar", "/api/reparto/rutas/12/reactivar")]
    [InlineData("PATCH", "/api/nexopostal/reparto/entregas/45/reasignar", "/api/reparto/entregas/45/reasignar")]
    [InlineData("POST", "/api/reparto/confirmar", "/api/reparto/confirmar")]
    [InlineData("PUT", "/api/asignaciones/12/completar", "/api/asignaciones/12/completar")]
    [InlineData("GET", "/api/ctas/12/dashboard", "/api/ctas/12/dashboard")]
    [InlineData("GET", "/api/nexopostal/perfil/get", "/api/perfil")]
    [InlineData("POST", "/api/nexopostal/perfil/guardar", "/api/perfil")]
    [InlineData("POST", "/api/nexopostal/perfil/agregar-direccion", "/api/perfil/direcciones")]
    [InlineData("PUT", "/api/nexopostal/perfil/editar-direccion/abc", "/api/perfil/direcciones/abc")]
    [InlineData("DELETE", "/api/nexopostal/perfil/eliminar-direccion/abc", "/api/perfil/direcciones/abc")]
    [InlineData("PUT", "/api/nexopostal/envios/interno-estado/EXP-1/estado", "/api/envios/interno/EXP-1/estado")]
    [InlineData("GET", "/api/oficinasPostales/buscar", "/api/oficinaspostales/buscar")]
    [InlineData("POST", "/api/admision/oficina/alta", "/api/admision/oficina/alta")]
    [InlineData("POST", "/api/movimientos", "/api/movimientos")]
    [InlineData("POST", "/api/incidencias/crear", "/api/incidencias")]
    public async Task RoutesPreserveMethodBodyQueryAndJwt(string method, string path, string destination)
    {
        const string query = "?entregaId=42&filtro=a%26b&tag=uno&tag=dos";
        using var request = new HttpRequestMessage(new HttpMethod(method), path + query);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fixture.Token);
        if (method != "GET") request.Content = new StringContent("{\"texto\":\"España\"}", Encoding.UTF8, "application/json");
        using var response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echo = (await response.Content.ReadFromJsonAsync<YarpFixture.Echo>())!;
        Assert.Equal(destination, echo.Path.TrimEnd('/'));
        Assert.Equal(method, echo.Method);
        Assert.Equal(query, echo.Query);
        Assert.Equal("Bearer " + fixture.Token, echo.Authorization);
        if (method != "GET") Assert.Equal("{\"texto\":\"España\"}", echo.Body);
    }

    [Theory]
    [InlineData("POST", "/api/auth/login")]
    [InlineData("POST", "/api/nexopostal/envios/cotizar")]
    [InlineData("GET", "/api/nexopostal/envios/track/NXP-1")]
    [InlineData("GET", "/api/oficinas")]
    [InlineData("POST", "/api/pagos/webhook")]
    public async Task PublicRoutesRemainPublic(string method, string path)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);
        request.Headers.Add("Stripe-Signature", "t=1,v1=signature");
        request.Content = new StringContent("{ \"raw\": true }\n", Encoding.UTF8, "application/json");
        using var response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echo = (await response.Content.ReadFromJsonAsync<YarpFixture.Echo>())!;
        Assert.Equal("{ \"raw\": true }\n", echo.Body);
        Assert.Equal("t=1,v1=signature", echo.StripeSignature);
    }

    [Theory]
    [InlineData(null, "UNAUTHORIZED")]
    [InlineData("invalid", "UNAUTHORIZED")]
    [InlineData("blocked", "USER_BLOCKED")]
    public async Task ProtectedRoutesRejectMissingInvalidAndBlockedUsers(string? user, string code)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/reparto/rutas");
        if (user != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer",
            user == "invalid" ? "bad-token" : YarpFixture.CreateToken(user));
        using var response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains(code, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(201)] [InlineData(204)] [InlineData(400)] [InlineData(401)] [InlineData(403)]
    [InlineData(404)] [InlineData(409)] [InlineData(422)] [InlineData(500)]
    public async Task UpstreamStatusAndBodyAreNotRewritten(int status)
    {
        using var response = await fixture.Client.GetAsync($"/api/envios/track/test?status={status}");
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("preserved", response.Headers.GetValues("X-Upstream").Single());
        if (status != 204)
        {
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
            Assert.Equal("{\"code\":\"ORIGINAL_ERROR\"}", await response.Content.ReadAsStringAsync());
        }
    }

    [Fact]
    public async Task BinaryContentAndDownloadHeadersArePreserved()
    {
        using var response = await fixture.Client.GetAsync("/api/envios/etiqueta/test?binary=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new byte[] { 37, 80, 68, 70, 0, 255, 128, 13, 10 }, await response.Content.ReadAsByteArrayAsync());
        Assert.Equal("application/pdf", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("etiqueta.pdf", response.Content.Headers.ContentDisposition!.FileName);
    }

    [Fact]
    public async Task LegacyGatewayParametersAreTranslatedOnlyForLegacyContract()
    {
        using var response = await fixture.Client.GetAsync("/api/Gateway/envios/track?parameters=NXP-123&lang=es");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echo = (await response.Content.ReadFromJsonAsync<YarpFixture.Echo>())!;
        Assert.Equal("/api/envios/track/NXP-123", echo.Path);
        Assert.Equal("?lang=es", echo.Query);
    }

    [Fact]
    public async Task CorsPreflightDoesNotRequireJwt()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/reparto/rutas");
        request.Headers.Add("Origin", "https://client.test");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        using var response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("https://client.test", response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task AdministrativeControllerKeepsRoleRestriction()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/nexopostal/admin-usuarios");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fixture.Token);
        using var response = await fixture.Client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
