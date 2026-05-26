using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NexoPostal.Auth.Data;
using System.Linq;

namespace Nexopostal.Tests.Auth;

/// <summary>
/// Factoría de servidor web en memoria personalizada para pruebas de integración de Auth.
/// </summary>
public class CustomAuthWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    private static readonly IServiceProvider _efInMemoryProvider =
        new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // 1. Quitar la configuración real de DbContext con PostgreSQL
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AuthDbContext>));

            if (descriptor != null)
                services.Remove(descriptor);

            // 2. Agregar DbContext usando base de datos en memoria con proveedor aislado
            services.AddDbContext<AuthDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryAuthDbForTesting");
                options.UseInternalServiceProvider(_efInMemoryProvider);
            });
        });
    }
}
