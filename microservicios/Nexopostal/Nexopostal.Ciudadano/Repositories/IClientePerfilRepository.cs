using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.Repositories;

/// <summary>
/// Repositorio para la gestión de perfiles de cliente y direcciones favoritas.
/// </summary>
public interface IClientePerfilRepository
{
    Task<ClientePerfil?> GetByUserIdAsync(string userId);
    Task<ClientePerfil> CreateOrUpdateAsync(ClientePerfil perfil);
    Task<List<DireccionFavorita>> GetDireccionesAsync(int clientePerfilId);
    Task<DireccionFavorita?> GetDireccionByIdAsync(int id, int clientePerfilId);
    Task<DireccionFavorita> AddDireccionAsync(DireccionFavorita direccion);
    Task UpdateDireccionAsync(DireccionFavorita direccion);
    Task<bool> DeleteDireccionAsync(int id, int clientePerfilId);
}
