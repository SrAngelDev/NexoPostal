using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Nexopostal.Ciudadano.Data;
using System.Linq;

namespace Nexopostal.Tests.Ciudadano;

/// <summary>
/// Factoría de servidor web en memoria personalizada para pruebas de integración de Ciudadano.
/// </summary>
public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Forzar el entorno a Testing
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // 1. Quitar la configuración real de DbContext con PostgreSQL
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CiudadanoDbContext>));
            if (descriptor != null)
                services.Remove(descriptor);

            // 2. Quitar los descriptores de EF relacionados con Npgsql
            var npgsqlDescriptors = services
                .Where(d =>
                    (d.ServiceType?.FullName?.Contains("Npgsql") == true) ||
                    (d.ImplementationType?.FullName?.Contains("Npgsql") == true) ||
                    (d.ImplementationFactory?.Method.DeclaringType?.FullName?.Contains("Npgsql") == true))
                .ToList();
            foreach (var d in npgsqlDescriptors) services.Remove(d);

            // Quitar IDbContextOptionsConfiguration<CiudadanoDbContext> (lambda UseNpgsql en EF Core 10)
            var optionsConfigDescriptors = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                    d.ServiceType.GenericTypeArguments.Length == 1 &&
                    d.ServiceType.GenericTypeArguments[0] == typeof(CiudadanoDbContext))
                .ToList();
            foreach (var d in optionsConfigDescriptors) services.Remove(d);

            // 3. Agregar DbContext con InMemory (sin UseInternalServiceProvider para evitar conflictos)
            // IMPORTANTE: el nombre debe generarse FUERA de la lambda para que
            // todos los scopes del mismo factory usen la misma BD InMemory.
            var dbName = "InMemoryCiudadanoForTesting_" + Guid.NewGuid();
            services.AddDbContext<CiudadanoDbContext>(options =>
            {
                options.UseInMemoryDatabase(dbName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }
}
