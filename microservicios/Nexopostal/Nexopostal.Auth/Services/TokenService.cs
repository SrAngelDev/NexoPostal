using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Services;

/// <summary>
/// Encapsula la generación y comprobación de tokens usados por el módulo de autenticación.
/// Mantiene en un único punto la lógica de JWT, refresh tokens y hashes seguros.
/// </summary>
public class TokenService
{
    private const int DefaultAccessTokenExpiryMinutes = 60;
    private const int DefaultRefreshTokenExpiryDays = 14;
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// Resuelve valores de configuración que llegan con placeholders del entorno,
    /// por ejemplo cuando Docker inyecta secretos a través de variables.
    /// </summary>
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

    /// <summary>
    /// Obtiene una clave válida para firmar JWT incluso cuando la semilla original
    /// es demasiado corta para HMAC SHA-256.
    /// </summary>
    private static byte[] GetJwtKeyBytes(string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        return keyBytes.Length >= 32 ? keyBytes : SHA256.HashData(keyBytes);
    }

    /// <summary>
    /// Genera el access token del usuario con las claims que necesita el resto del sistema.
    /// </summary>
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

    /// <summary>
    /// Lee la duración del access token desde configuración y aplica un valor por defecto
    /// si la configuración falta o es inválida.
    /// </summary>
    public int GetAccessTokenExpiryMinutes()
    {
        var raw = ResolveConfigValue(_config["JwtSettings:ExpiryMinutes"]);
        if (int.TryParse(raw, out var minutes) && minutes > 0)
        {
            return minutes;
        }

        return DefaultAccessTokenExpiryMinutes;
    }

    /// <summary>
    /// Devuelve la caducidad de los refresh tokens en días, con fallback seguro.
    /// </summary>
    public int GetRefreshTokenExpiryDays()
    {
        var raw = ResolveConfigValue(_config["JwtSettings:RefreshExpiryDays"]);
        if (int.TryParse(raw, out var days) && days > 0)
        {
            return days;
        }

        return DefaultRefreshTokenExpiryDays;
    }

    /// <summary>
    /// Genera un refresh token opaco y suficientemente aleatorio, prefijado con el id del usuario
    /// para facilitar su trazabilidad interna.
    /// </summary>
    public string GenerateRefreshToken(string userId)
    {
        var random = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return $"{userId}.{random}";
    }

    /// <summary>
    /// Intenta recuperar el identificador del usuario desde el refresh token sin validar todavía
    /// su firma ni su hash almacenado.
    /// </summary>
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

    /// <summary>
    /// Convierte el token en un hash SHA-256 para persistirlo sin guardar el valor en claro.
    /// </summary>
    public string HashToken(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// Compara dos cadenas con tiempo constante para evitar ataques de timing.
    /// </summary>
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

