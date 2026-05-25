using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Mappers;

/// <summary>
/// Mapper estático de <see cref="ApplicationUser"/> a DTOs.
/// Patrón: extensiones manuales — sin AutoMapper para máxima trazabilidad.
/// </summary>
public static class UsuarioMapper
{
    public static UsuarioInfoDto ToInfoDto(this ApplicationUser user) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        NombreCompleto = user.NombreCompleto,
        PhoneNumber = user.PhoneNumber,
        FechaRegistro = user.FechaRegistro,
        Rol = user.Rol.ToString()
    };

    public static TokenResponseDto ToTokenResponseDto(
        this ApplicationUser user,
        string accessToken,
        DateTime accessExpiration,
        string refreshToken,
        DateTime refreshExpiration) => new()
    {
        Token = accessToken,
        Expiration = accessExpiration,
        RefreshToken = refreshToken,
        RefreshTokenExpiration = refreshExpiration,
        User = user.NombreCompleto,
        Rol = user.Rol.ToString()
    };
}
