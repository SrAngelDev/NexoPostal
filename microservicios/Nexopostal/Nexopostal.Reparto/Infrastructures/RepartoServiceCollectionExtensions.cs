using System.Reflection;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Shared.Infrastructures;
using Microsoft.EntityFrameworkCore;

namespace Nexopostal.Reparto.Infrastructures;

/// <summary>
/// Extensiones de wiring para Nexopostal.Reparto.
/// </summary>
public static class RepartoServiceCollectionExtensions
{
    public static IServiceCollection AddRepartoDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = JwtAuthenticationExtensions.ResolveConfigValue(configuration.GetConnectionString("DefaultConnection"));
        services.AddDbContext<RepartoDbContext>(options => options.UseNpgsql(conn));
        return services;
    }

    public static IServiceCollection AddRepartoRepositories(this IServiceCollection services)
    {
        services.AddScoped<IRepartidorRepository, RepartidorRepository>();
        services.AddScoped<IRutaRepartoRepository, RutaRepartoRepository>();
        services.AddScoped<IEntregaPaqueteRepository, EntregaPaqueteRepository>();
        services.AddScoped<IUbicacionRepartidorRepository, UbicacionRepartidorRepository>();
        services.AddScoped<IVehiculoRepository, VehiculoRepository>();
        return services;
    }

    public static IServiceCollection AddRepartoValidation(this IServiceCollection services) =>
        services.AddNexopostalFluentValidation(Assembly.GetExecutingAssembly());
}
