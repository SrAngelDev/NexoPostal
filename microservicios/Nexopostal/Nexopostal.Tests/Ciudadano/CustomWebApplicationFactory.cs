using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexopostal.Ciudadano.Data;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Testcontainers.PostgreSql;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

/// <summary>
/// Mock de autenticación para pruebas de integración de Ciudadano.
/// Solo autentica si la petición incluye el header Authorization.
/// </summary>
public class CiudadanoTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public CiudadanoTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id-123"),
            new Claim("sub", "test-user-id-123"),
            new Claim(ClaimTypes.Name, "test@nexopostal.com"),
            new Claim(ClaimTypes.Role, "Cliente")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// WebApplicationFactory para tests de integración de Ciudadano.
/// Usa un contenedor PostgreSQL real (TestContainers) en lugar de EF InMemory.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Nexopostal.Ciudadano.Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _pgContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .Build();

    public async Task InitializeAsync() => await _pgContainer.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            var npgsqlDescriptors = services
                .Where(d =>
                    (d.ServiceType?.FullName?.Contains("Npgsql") == true) ||
                    (d.ImplementationType?.FullName?.Contains("Npgsql") == true) ||
                    (d.ImplementationFactory?.Method.DeclaringType?.FullName?.Contains("Npgsql") == true))
                .ToList();
            foreach (var d in npgsqlDescriptors) services.Remove(d);

            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CiudadanoDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            var optionsConfigDescriptors = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                    d.ServiceType.GenericTypeArguments.Length == 1 &&
                    d.ServiceType.GenericTypeArguments[0] == typeof(CiudadanoDbContext))
                .ToList();
            foreach (var d in optionsConfigDescriptors) services.Remove(d);

            services.AddDbContext<CiudadanoDbContext>(options =>
                options.UseNpgsql(_pgContainer.GetConnectionString()));

            services.AddAuthentication(defaultScheme: "Test")
                .AddScheme<AuthenticationSchemeOptions, CiudadanoTestAuthHandler>("Test", _ => { });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CiudadanoDbContext>();
        db.Database.EnsureCreated();
        return host;
    }

    public new async Task DisposeAsync() => await _pgContainer.DisposeAsync();
}
