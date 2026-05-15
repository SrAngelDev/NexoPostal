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
    private const int DefaultAccessTokenExpiryMinutes = 60;
    private const int DefaultRefreshTokenExpiryDays = 14;
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

    public (string Token, DateTime ExpirationUtc) GenerateAccessToken(ApplicationUser user)
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
        var expirationUtc = DateTime.UtcNow.AddMinutes(GetAccessTokenExpiryMinutes());

        var key = new SymmetricSecurityKey(GetJwtKeyBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expirationUtc,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expirationUtc);
    }

    public int GetAccessTokenExpiryMinutes()
    {
        var raw = ResolveConfigValue(_config["JwtSettings:ExpiryMinutes"]);
        if (int.TryParse(raw, out var minutes) && minutes > 0)
        {
            return minutes;
        }

        return DefaultAccessTokenExpiryMinutes;
    }

    public int GetRefreshTokenExpiryDays()
    {
        var raw = ResolveConfigValue(_config["JwtSettings:RefreshExpiryDays"]);
        if (int.TryParse(raw, out var days) && days > 0)
        {
            return days;
        }

        return DefaultRefreshTokenExpiryDays;
    }

    public string GenerateRefreshToken(string userId)
    {
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return $"{userId}.{random}";
    }

    public bool TryExtractUserId(string refreshToken, out string userId)
    {
        userId = string.Empty;
        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        var separator = refreshToken.IndexOf('.');
        if (separator <= 0 || separator >= refreshToken.Length - 1)
            return false;

        userId = refreshToken[..separator];
        return !string.IsNullOrWhiteSpace(userId);
    }

    public string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }

    public bool SecureEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left ?? string.Empty);
        var rightBytes = Encoding.UTF8.GetBytes(right ?? string.Empty);

        if (leftBytes.Length != rightBytes.Length)
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}

