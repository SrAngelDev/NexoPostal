using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Nexopostal.Shared.Infrastructures;

/// <summary>
/// Configuración de autenticación JWT Bearer común entre microservicios.
/// </summary>
public static class JwtAuthenticationExtensions
{
    /// <summary>Resuelve placeholders ${VAR} con variables de entorno.</summary>
    public static string ResolveConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return Regex.Replace(value, @"\$\{([^}]+)\}", match =>
        {
            var envVar = match.Groups[1].Value;
            return Environment.GetEnvironmentVariable(envVar) ?? match.Value;
        });
    }

    /// <summary>Convierte la secret en bytes asegurando al menos 32 bytes (SHA256 si es menor).</summary>
    public static byte[] GetJwtKeyBytes(string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        return keyBytes.Length >= 32 ? keyBytes : SHA256.HashData(keyBytes);
    }

    /// <summary>Configura autenticación JWT Bearer usando la sección "JwtSettings".</summary>
    public static IServiceCollection AddNexopostalJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection("JwtSettings");
        var secret = ResolveConfigValue(jwt["SecretKey"]);
        if (string.IsNullOrWhiteSpace(secret))
            throw new InvalidOperationException("JWT SecretKey no configurada");

        var key = GetJwtKeyBytes(secret);
        var issuer = ResolveConfigValue(jwt["Issuer"]);
        var audience = ResolveConfigValue(jwt["Audience"]);

        services
            .AddAuthentication(options =>
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
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();
        return services;
    }
}
