using Microsoft.EntityFrameworkCore;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.Models;

namespace Nexopostal.Intranet.Repositories;

public class OficinaRepository : IOficinaRepository
{
    private readonly IntranetDbContext _db;

    public OficinaRepository(IntranetDbContext db)
    {
        _db = db;
    }

    public async Task<List<OficinaPostal>> GetAllAsync(bool incluirInactivas = false)
    {
        IQueryable<OficinaPostal> q = _db.OficinasPostales.AsNoTracking();
        if (!incluirInactivas) q = q.Where(o => o.Activo);
        return await q.OrderBy(o => o.Id).ToListAsync();
    }

    public Task<OficinaPostal?> GetByIdAsync(int id) =>
        _db.OficinasPostales.FirstOrDefaultAsync(o => o.Id == id);

    public Task<bool> ExistsAsync(int id) =>
        _db.OficinasPostales.AnyAsync(o => o.Id == id);

    public async Task<int> NextIdAsync()
    {
        var max = await _db.OficinasPostales.AsNoTracking().MaxAsync(o => (int?)o.Id) ?? 1000;
        return max + 1;
    }

    public async Task<OficinaPostal> CreateAsync(OficinaPostal oficina)
    {
        _db.OficinasPostales.Add(oficina);
        await _db.SaveChangesAsync();
        return oficina;
    }

    public async Task UpdateAsync(OficinaPostal oficina)
    {
        _db.OficinasPostales.Update(oficina);
        await _db.SaveChangesAsync();
    }

    public Task<int> CountOperariosActivosAsync(int oficinaJsonId) =>
        _db.OperariosOficina.CountAsync(o => o.OficinaJsonId == oficinaJsonId && o.Activo);
}
