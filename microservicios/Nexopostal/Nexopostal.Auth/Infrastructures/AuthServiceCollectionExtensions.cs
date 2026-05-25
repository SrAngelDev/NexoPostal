using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nexopostal.Shared.Infrastructures;
using NexoPostal.Auth.Data;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Repositories;
using NexoPostal.Auth.Services;

namespace NexoPostal.Auth.Infrastructures;

/// <summary>
/// Extensiones de IServiceCollection para modularizar la configuración del microservicio Auth.
/// </summary>
public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddAuthDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = JwtAuthenticationExtensions.ResolveConfigValue(
            configuration.GetConnectionString("DefaultConnection"));

        services.AddDbContext<AuthDbContext>(options => options.UseNpgsql(connectionString));
        return services;
    }

    public static IServiceCollection AddAuthIdentity(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequiredLength = 6;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireLowercase = false;
        })
        .AddEntityFrameworkStores<AuthDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddAuthJwt(this IServiceCollection services, IConfiguration configuration) =>
        services.AddNexopostalJwtAuthentication(configuration);

    public static IServiceCollection AddAuthRepositories(this IServiceCollection services)
    {
        services.AddScoped<IUserRepository, UserRepository>();
        return services;
    }

    public static IServiceCollection AddAuthServices(this IServiceCollection services)
    {
        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddSingleton<IEmailService, SmtpEmailService>();
        return services;
    }

    public static IServiceCollection AddAuthValidation(this IServiceCollection services) =>
        services.AddNexopostalFluentValidation(Assembly.GetExecutingAssembly());
}
