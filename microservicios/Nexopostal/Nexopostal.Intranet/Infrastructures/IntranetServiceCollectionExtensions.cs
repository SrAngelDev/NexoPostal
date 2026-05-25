using System.Reflection;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Shared.Infrastructures;
using Microsoft.EntityFrameworkCore;

namespace Nexopostal.Intranet.Infrastructures;

/// <summary>
/// Extensiones de wiring para Nexopostal.Intranet.
/// </summary>
public static class IntranetServiceCollectionExtensions
{
    public static IServiceCollection AddIntranetDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var conn = JwtAuthenticationExtensions.ResolveConfigValue(configuration.GetConnectionString("DefaultConnection"));
        services.AddDbContext<IntranetDbContext>(options => options.UseNpgsql(conn));
        return services;
    }

    public static IServiceCollection AddIntranetRepositories(this IServiceCollection services)
    {
        services.AddScoped<ICentroTratamientoRepository, CentroTratamientoRepository>();
        services.AddScoped<IRutaCtaRepository, RutaCtaRepository>();
        services.AddScoped<IOperarioCtaRepository, OperarioCtaRepository>();
        services.AddScoped<IOperarioOficinaRepository, OperarioOficinaRepository>();
        services.AddScoped<IAsignacionPaqueteRepository, AsignacionPaqueteRepository>();
        services.AddScoped<IMovimientoPaqueteRepository, MovimientoPaqueteRepository>();
        services.AddScoped<IIncidenciaRepository, IncidenciaRepository>();
        services.AddScoped<IHistorialEstadoRepository, HistorialEstadoRepository>();
        services.AddScoped<IOficinaRepository, OficinaRepository>();
        return services;
    }

    public static IServiceCollection AddIntranetValidation(this IServiceCollection services) =>
        services.AddNexopostalFluentValidation(Assembly.GetExecutingAssembly());
}
