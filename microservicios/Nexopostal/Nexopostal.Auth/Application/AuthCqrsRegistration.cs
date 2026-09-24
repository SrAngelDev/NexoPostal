using Microsoft.Extensions.DependencyInjection;
using NexoPostal.Auth.Services;
using Nexopostal.Shared.Cqrs;

namespace NexoPostal.Auth.Application;
public static class AuthCqrsRegistration
{
    public static IServiceCollection AddAuthCqrs(this IServiceCollection services)
    {
        services.AddApplicationCqrs(typeof(AuthCqrsRegistration).Assembly);
        services.AddScoped<NexoPostal.Auth.Application.AdminUser.IAdminUserCommands>(provider => provider.GetRequiredService<IAdminUserService>());
        services.AddScoped<NexoPostal.Auth.Application.AdminUser.IAdminUserQueries>(provider => provider.GetRequiredService<IAdminUserService>());
        services.AddScoped<NexoPostal.Auth.Application.Auth.IAuthCommands>(provider => provider.GetRequiredService<IAuthService>());
        services.AddScoped<NexoPostal.Auth.Application.Auth.IAuthQueries>(provider => provider.GetRequiredService<IAuthService>());
        return services;
    }
}
