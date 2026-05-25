using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Mappers;

/// <summary>
/// Mapper de <see cref="ApplicationUser"/> a <see cref="AdminUsuarioListItemDto"/>.
/// </summary>
public static class AdminUsuarioMapper
{
    public static AdminUsuarioListItemDto ToListItemDto(this ApplicationUser u, DateTimeOffset ahora) => new()
    {
        Id             = u.Id,
        NombreCompleto = u.NombreCompleto,
        Email          = u.Email ?? string.Empty,
        CodigoEmpleado = u.CodigoEmpleado,
        PhoneNumber    = u.PhoneNumber,
        Rol            = u.Rol.ToString(),
        FechaRegistro  = u.FechaRegistro,
        Bloqueado      = u.LockoutEnd != null && u.LockoutEnd > ahora,
        Eliminado      = u.Eliminado,
        EliminadoEnUtc = u.EliminadoEnUtc
    };

    public static List<AdminUsuarioListItemDto> ToListItemDtos(this IEnumerable<ApplicationUser> users, DateTimeOffset ahora) =>
        users.Select(u => u.ToListItemDto(ahora)).ToList();
}
