using System.Reflection;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using Nexopostal.Shared.Infrastructures;
using Microsoft.EntityFrameworkCore;

namespace Nexopostal.Ciudadano.Infrastructures;

/// <summary>
/// Extensiones de wiring para Nexopostal.Ciudadano. Permite simplificar Program.cs y
/// reutilizar registros en tests de integración.
/// </summary>
public static class CiudadanoServiceCollectionExtensions
{
    public static IServiceCollection AddCiudadanoDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = JwtAuthenticationExtensions.ResolveConfigValue(configuration.GetConnectionString("DefaultConnection"));
        services.AddDbContext<CiudadanoDbContext>(options => options.UseNpgsql(conn));
        return services;
    }

    public static IServiceCollection AddCiudadanoRepositories(this IServiceCollection services)
    {
        services.AddScoped<IEnvioRepository, EnvioRepository>();
        services.AddScoped<IClientePerfilRepository, ClientePerfilRepository>();
        return services;
    }

    public static IServiceCollection AddCiudadanoValidation(this IServiceCollection services) =>
        services.AddNexopostalFluentValidation(Assembly.GetExecutingAssembly());
}
