using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexoPostal.Auth.Data;
using Testcontainers.PostgreSql;
using Xunit;

namespace Nexopostal.Tests.Auth;

/// <summary>
/// WebApplicationFactory para tests de integración de Auth.
/// Usa un contenedor PostgreSQL real (TestContainers) en lugar de EF InMemory.
/// </summary>
public class CustomAuthWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>, IAsyncLifetime where TProgram : class
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
            // Quitar todas las configuraciones de DbContext con Npgsql
            var npgsqlDescriptors = services
                .Where(d =>
                    (d.ServiceType?.FullName?.Contains("Npgsql") == true) ||
                    (d.ImplementationType?.FullName?.Contains("Npgsql") == true) ||
                    (d.ImplementationFactory?.Method.DeclaringType?.FullName?.Contains("Npgsql") == true))
                .ToList();
            foreach (var d in npgsqlDescriptors) services.Remove(d);

            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            var optionsConfigDescriptors = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                    d.ServiceType.GenericTypeArguments.Length == 1 &&
                    d.ServiceType.GenericTypeArguments[0] == typeof(AuthDbContext))
                .ToList();
            foreach (var d in optionsConfigDescriptors) services.Remove(d);

            // Usar el contenedor PostgreSQL de pruebas
            services.AddDbContext<AuthDbContext>(options =>
                options.UseNpgsql(_pgContainer.GetConnectionString()));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        db.Database.EnsureCreated();
        return host;
    }

    public new async Task DisposeAsync() => await _pgContainer.DisposeAsync();
}
