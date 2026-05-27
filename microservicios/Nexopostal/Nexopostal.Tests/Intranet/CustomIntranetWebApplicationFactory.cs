using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.Hubs;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Testcontainers.PostgreSql;
using Xunit;

namespace Nexopostal.Tests.Intranet;

/// <summary>
/// Mock de autenticación para tests de integración de Intranet.
/// Permite configurar el rol en tiempo de construcción.
/// </summary>
public class IntranetTestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public static string DefaultRole { get; set; } = "OperarioCTA";
    public static int DefaultOperarioId { get; set; } = 1;
    public static int DefaultCtaId { get; set; } = 1;

    public IntranetTestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Solo autenticar si hay header Authorization en la petición
        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-intranet-user-id"),
            new Claim("sub", "test-intranet-user-id"),
            new Claim(ClaimTypes.Name, "test-intranet@nexopostal.com"),
            new Claim(ClaimTypes.Role, DefaultRole),
            new Claim("OperarioId", DefaultOperarioId.ToString()),
            new Claim("CtaId", DefaultCtaId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// WebApplicationFactory para tests de integración de Intranet.
/// Usa un contenedor PostgreSQL real (TestContainers), mockea IHubContext&lt;IntranetHub&gt;.
/// </summary>
public class CustomIntranetWebApplicationFactory : WebApplicationFactory<Nexopostal.Intranet.Data.IntranetDbContext>, IAsyncLifetime
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
                d => d.ServiceType == typeof(DbContextOptions<IntranetDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            var optionsConfigDescriptors = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                    d.ServiceType.GenericTypeArguments.Length == 1 &&
                    d.ServiceType.GenericTypeArguments[0] == typeof(IntranetDbContext))
                .ToList();
            foreach (var d in optionsConfigDescriptors) services.Remove(d);

            services.AddDbContext<IntranetDbContext>(options =>
                options.UseNpgsql(_pgContainer.GetConnectionString()));

            // Mockear IHubContext<IntranetHub> para evitar dependencias de SignalR real
            var mockHub = new Mock<IHubContext<IntranetHub>>();
            var mockClients = new Mock<IHubClients>();
            var mockClientProxy = new Mock<IClientProxy>();

            mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

            services.AddSingleton(mockHub.Object);

            services.AddAuthentication(defaultScheme: "Test")
                .AddScheme<AuthenticationSchemeOptions, IntranetTestAuthHandler>("Test", _ => { });
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntranetDbContext>();
        db.Database.EnsureCreated();
        return host;
    }

    public new async Task DisposeAsync() => await _pgContainer.DisposeAsync();
}
