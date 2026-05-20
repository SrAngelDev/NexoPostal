using System.Security.Cryptography;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using AspNetCore.ApiGateway;
using AspNetCore.ApiGateway.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nexopostal.Gateway.Services;

namespace Nexopostal.Gateway.Extensions;

/// <summary>
/// Métodos de extensión para registrar los servicios del Gateway
/// en el contenedor de inyección de dependencias.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static string ResolveConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return Regex.Replace(value, @"\$\{([^}]+)\}", match =>
            Environment.GetEnvironmentVariable(match.Groups[1].Value) ?? match.Value);
    }

    private static byte[] GetJwtKeyBytes(string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        return keyBytes.Length >= 32 ? keyBytes : SHA256.HashData(keyBytes);
    }

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
        var secretKey = ResolveConfigValue(jwtSettings["SecretKey"]);
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("JWT SecretKey no configurada");
            
        var key = GetJwtKeyBytes(secretKey);
        var issuer = ResolveConfigValue(jwtSettings["Issuer"]);
        var audience = ResolveConfigValue(jwtSettings["Audience"]);
        var authBaseUrl = ResolveConfigValue(config["Microservices:Auth"]);
        if (string.IsNullOrWhiteSpace(authBaseUrl))
            authBaseUrl = "http://modulo-seguridad:80";

        services.AddHttpClient<IUserSessionValidationService, UserSessionValidationService>(client =>
        {
            client.BaseAddress = new Uri($"{authBaseUrl.TrimEnd('/')}/");
            client.Timeout = TimeSpan.FromSeconds(3);
        });

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
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                NameClaimType = "sub",
                RoleClaimType = "role"
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                 ?? context.Principal?.FindFirst("sub")?.Value;

                    if (string.IsNullOrWhiteSpace(userId))
                    {
                        context.HttpContext.Items["GatewayAuthErrorCode"] = "INVALID_TOKEN";
                        context.Fail("Token invalido: no incluye identificador de usuario.");
                        return;
                    }

                    var sessionValidator = context.HttpContext.RequestServices
                        .GetRequiredService<IUserSessionValidationService>();
                    var validationStatus = await sessionValidator.ValidateAsync(
                        userId,
                        context.HttpContext.RequestAborted);

                    if (validationStatus == SessionValidationStatus.Blocked)
                    {
                        context.HttpContext.Items["GatewayAuthErrorCode"] = "USER_BLOCKED";
                        context.Fail("La cuenta esta bloqueada.");
                    }
                }
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
