using System.Text;
using AspNetCore.ApiGateway;
using AspNetCore.ApiGateway.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Nexopostal.Gateway.Services;

namespace Nexopostal.Gateway.Extensions;

/// <summary>
/// Métodos de extensión para registrar los servicios del Gateway
/// en el contenedor de inyección de dependencias.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configura CORS con los orígenes permitidos definidos en appsettings.
    /// </summary>
    public static IServiceCollection AddGatewayCors(
        this IServiceCollection services, IConfiguration config)
    {
        var allowedOrigins = config.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy("NexoPostalPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    /// <summary>
    /// Configura la autenticación JWT Bearer.
    /// El Gateway NO genera tokens; solo los valida.
    /// La clave debe ser idéntica a la de NexoPostal.Auth/TokenService.cs.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration config)
    {
        var jwtSettings = config.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey no configurada");
        var key = Encoding.UTF8.GetBytes(secretKey);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }

    /// <summary>
    /// Registra IGatewayAuthorization + AddApiGateway + AddControllers.
    /// IGatewayAuthorization se registra ANTES de AddApiGateway (requisito de la librería).
    /// </summary>
    public static IServiceCollection AddGatewayServices(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IGatewayAuthorization, GatewayAuthorizationService>();
        services.AddTransient<Middleware.ErrorPropagationHandler>();
        services.AddApiGateway();
        services.AddControllers();

        // Registrar el DelegatingHandler en TODOS los HttpClient del contenedor.
        // Esto intercepta las llamadas HTTP de la librería ApiGateway antes de que
        // esta invoque EnsureSuccessStatusCode(), permitiendo capturar el body y
        // el status code real del microservicio.
        services.ConfigureHttpClientDefaults(builder =>
        {
            builder.AddHttpMessageHandler<Middleware.ErrorPropagationHandler>();
        });

        return services;
    }
}
