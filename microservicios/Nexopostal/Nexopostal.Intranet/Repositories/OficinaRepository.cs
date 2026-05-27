using Microsoft.EntityFrameworkCore;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.Models;

namespace Nexopostal.Intranet.Repositories;

/// <summary>
/// Repositorio de oficinas postales administradas desde Intranet.
/// Se apoya en la base de datos operativa para altas, cambios y métricas básicas.
/// </summary>
public class OficinaRepository : IOficinaRepository
{
    private readonly IntranetDbContext _db;

    public OficinaRepository(IntranetDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Devuelve el catálogo de oficinas, filtrando las inactivas cuando la pantalla no necesita ver histórico.
    /// </summary>
    public async Task<List<OficinaPostal>> GetAllAsync(bool incluirInactivas = false)
    {
        IQueryable<OficinaPostal> q = _db.OficinasPostales.AsNoTracking();
        if (!incluirInactivas) q = q.Where(o => o.Activo);
        return await q.OrderBy(o => o.Id).ToListAsync();
    }

    /// <summary>Busca una oficina concreta por su identificador interno.</summary>
    public Task<OficinaPostal?> GetByIdAsync(int id) =>
        _db.OficinasPostales.FirstOrDefaultAsync(o => o.Id == id);

    /// <summary>Indica si ya existe una oficina con ese identificador.</summary>
    public Task<bool> ExistsAsync(int id) =>
        _db.OficinasPostales.AnyAsync(o => o.Id == id);

    /// <summary>
    /// Calcula el siguiente identificador disponible cuando se crea una oficina nueva.
    /// </summary>
    public async Task<int> NextIdAsync()
    {
        var max = await _db.OficinasPostales.AsNoTracking().MaxAsync(o => (int?)o.Id) ?? 1000;
        return max + 1;
    }

    /// <summary>Persiste una nueva oficina y devuelve la entidad ya guardada.</summary>
    public async Task<OficinaPostal> CreateAsync(OficinaPostal oficina)
    {
        _db.OficinasPostales.Add(oficina);
        await _db.SaveChangesAsync();
        return oficina;
    }

    /// <summary>Actualiza los datos de una oficina existente.</summary>
    public async Task UpdateAsync(OficinaPostal oficina)
    {
        _db.OficinasPostales.Update(oficina);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Cuenta cuántos operarios activos siguen asignados a una oficina concreta.
    /// </summary>
    public Task<int> CountOperariosActivosAsync(int oficinaJsonId) =>
        _db.OperariosOficina.CountAsync(o => o.OficinaJsonId == oficinaJsonId && o.Activo);
}
