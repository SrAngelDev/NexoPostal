using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Nexopostal.Shared.Infrastructures;

/// <summary>
/// Helpers para registrar FluentValidation con auto-validación en cada microservicio.
/// </summary>
public static class FluentValidationExtensions
{
    /// <summary>
    /// Registra FluentValidation con auto-validación y client adapters,
    /// escaneando los validators de los assemblies indicados.
    /// </summary>
    public static IServiceCollection AddNexopostalFluentValidation(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();

        if (assemblies.Length == 0)
            assemblies = [Assembly.GetCallingAssembly()];

        services.AddValidatorsFromAssemblies(assemblies);
        return services;
    }
}
