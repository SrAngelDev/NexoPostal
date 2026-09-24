using Microsoft.Extensions.DependencyInjection;
using Nexopostal.Reparto.Services;
using Nexopostal.Shared.Cqrs;

namespace Nexopostal.Reparto.Application;
public static class RepartoCqrsRegistration
{
    public static IServiceCollection AddRepartoCqrs(this IServiceCollection services)
    {
        services.AddApplicationCqrs(typeof(RepartoCqrsRegistration).Assembly);
        services.AddScoped<Nexopostal.Reparto.Application.BandejaPendientes.IBandejaPendientesCommands>(provider => provider.GetRequiredService<IBandejaPendientesService>());
        services.AddScoped<Nexopostal.Reparto.Application.BandejaPendientes.IBandejaPendientesQueries>(provider => provider.GetRequiredService<IBandejaPendientesService>());
        services.AddScoped<Nexopostal.Reparto.Application.Reparto.IRepartoCommands>(provider => provider.GetRequiredService<IRepartoService>());
        services.AddScoped<Nexopostal.Reparto.Application.Reparto.IRepartoQueries>(provider => provider.GetRequiredService<IRepartoService>());
        services.AddScoped<Nexopostal.Reparto.Application.Vehiculos.IVehiculoCommands>(provider => provider.GetRequiredService<IVehiculoService>());
        services.AddScoped<Nexopostal.Reparto.Application.Vehiculos.IVehiculoQueries>(provider => provider.GetRequiredService<IVehiculoService>());
        return services;
    }
}
