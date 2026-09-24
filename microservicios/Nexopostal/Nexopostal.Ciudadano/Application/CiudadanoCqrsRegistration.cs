using Microsoft.Extensions.DependencyInjection;
using Nexopostal.Ciudadano.Services;
using Nexopostal.Shared.Cqrs;

namespace Nexopostal.Ciudadano.Application;
public static class CiudadanoCqrsRegistration
{
    public static IServiceCollection AddCiudadanoCqrs(this IServiceCollection services)
    {
        services.AddApplicationCqrs(typeof(CiudadanoCqrsRegistration).Assembly);
        services.AddScoped<Nexopostal.Ciudadano.Application.AdminEnvios.IAdminEnviosCommands>(provider => provider.GetRequiredService<IAdminEnviosService>());
        services.AddScoped<Nexopostal.Ciudadano.Application.AdminEnvios.IAdminEnviosQueries>(provider => provider.GetRequiredService<IAdminEnviosService>());
        return services;
    }
}
