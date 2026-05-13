using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Services;

public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    private static string ResolveConfigValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return Regex.Replace(value, @"\$\{([^}]+)\}", match =>
        {
            var envVar = match.Groups[1].Value;
            return Environment.GetEnvironmentVariable(envVar) ?? match.Value;
        });
    }

    private static byte[] GetJwtKeyBytes(string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        return keyBytes.Length >= 32 ? keyBytes : SHA256.HashData(keyBytes);
    }

    public string GenerateJwtToken(ApplicationUser user)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim("Nombre", user.NombreCompleto),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Rol.ToString())
        };

        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey = ResolveConfigValue(jwtSettings["SecretKey"]);
        if (string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("JWT SecretKey no configurada");

        var issuer = ResolveConfigValue(jwtSettings["Issuer"]);
        var audience = ResolveConfigValue(jwtSettings["Audience"]);

        var key = new SymmetricSecurityKey(GetJwtKeyBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

