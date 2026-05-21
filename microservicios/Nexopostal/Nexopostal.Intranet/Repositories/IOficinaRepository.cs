using Nexopostal.Intranet.Models;

namespace Nexopostal.Intranet.Repositories;

/// <summary>
/// Repositorio CRUD de oficinas postales.
/// </summary>
public interface IOficinaRepository
{
    Task<List<OficinaPostal>> GetAllAsync(bool incluirInactivas = false);
    Task<OficinaPostal?> GetByIdAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> NextIdAsync();
    Task<OficinaPostal> CreateAsync(OficinaPostal oficina);
    Task UpdateAsync(OficinaPostal oficina);
    Task<int> CountOperariosActivosAsync(int oficinaJsonId);
}
