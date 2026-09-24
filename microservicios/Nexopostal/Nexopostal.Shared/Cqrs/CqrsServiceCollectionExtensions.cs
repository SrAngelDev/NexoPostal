using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Nexopostal.Shared.Cqrs;

public static class CqrsServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationCqrs(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddMediatR(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(assemblies);
            configuration.AddOpenBehavior(typeof(CqrsExecutionBehavior<,>));
        });
        return services;
    }
}
