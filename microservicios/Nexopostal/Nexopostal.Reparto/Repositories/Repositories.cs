using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.Models;

namespace Nexopostal.Reparto.Repositories;

public class RepartidorRepository : IRepartidorRepository
{
    private readonly RepartoDbContext _context;
    public RepartidorRepository(RepartoDbContext context) => _context = context;

    public async Task<Repartidor?> GetByIdAsync(int id)
        => await _context.Repartidores.FindAsync(id);

    public async Task<Repartidor?> GetByIdentityUserIdAsync(string identityUserId)
        => await _context.Repartidores
            .Include(r => r.Rutas)
            .FirstOrDefaultAsync(r => r.IdentityUserId == identityUserId);

    public async Task<List<Repartidor>> GetAllAsync(int? oficinaJsonId = null)
    {
        var query = _context.Repartidores.Include(r => r.Rutas).AsQueryable();
        if (oficinaJsonId.HasValue)
            query = query.Where(r => r.OficinaJsonId == oficinaJsonId.Value);
        return await query.OrderBy(r => r.NombreCompleto).ToListAsync();
    }

    public async Task<Repartidor> CreateAsync(Repartidor repartidor)
    {
        _context.Repartidores.Add(repartidor);
        await _context.SaveChangesAsync();
        return repartidor;
    }
}

public class RutaRepartoRepository : IRutaRepartoRepository
{
    private readonly RepartoDbContext _context;
    public RutaRepartoRepository(RepartoDbContext context) => _context = context;

    public async Task<RutaReparto?> GetByIdAsync(int id)
        => await _context.RutasReparto
            .Include(r => r.Repartidor)
            .Include(r => r.Entregas)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<RutaReparto?> GetByCodigoAsync(string codigo)
        => await _context.RutasReparto
            .Include(r => r.Repartidor)
            .Include(r => r.Entregas)
            .FirstOrDefaultAsync(r => r.Codigo == codigo);

    public async Task<RutaReparto?> GetWithEntregasAsync(int id)
        => await _context.RutasReparto
            .Include(r => r.Entregas)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<List<RutaReparto>> GetAllAsync(DateOnly? fecha = null, int? repartidorId = null)
    {
        var query = _context.RutasReparto
            .Include(r => r.Repartidor)
            .Include(r => r.Entregas)
            .AsQueryable();

        if (fecha.HasValue)
            query = query.Where(r => r.FechaReparto == fecha.Value);
        if (repartidorId.HasValue)
            query = query.Where(r => r.RepartidorId == repartidorId.Value);

        return await query
            .OrderByDescending(r => r.FechaReparto)
            .ThenBy(r => r.Codigo)
            .ToListAsync();
    }

    public async Task<RutaReparto> CreateAsync(RutaReparto ruta)
    {
        _context.RutasReparto.Add(ruta);
        await _context.SaveChangesAsync();
        return ruta;
    }

    public async Task UpdateAsync(RutaReparto ruta)
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountByFechaAsync(DateOnly fecha)
        => await _context.RutasReparto.CountAsync(r => r.FechaReparto == fecha);

    public async Task<List<RutaReparto>> GetByFechaAsync(DateOnly fecha, int? oficinaJsonId = null)
    {
        var query = _context.RutasReparto.Where(r => r.FechaReparto == fecha);
        if (oficinaJsonId.HasValue)
            query = query.Where(r => r.OficinaOrigenJsonId == oficinaJsonId.Value);
        return await query.ToListAsync();
    }
}

public class EntregaPaqueteRepository : IEntregaPaqueteRepository
{
    private readonly RepartoDbContext _context;
    public EntregaPaqueteRepository(RepartoDbContext context) => _context = context;

    public async Task<EntregaPaquete?> GetByIdAsync(int id)
        => await _context.EntregasPaquetes.FindAsync(id);

    public async Task<EntregaPaquete?> GetWithRutaAsync(int id)
        => await _context.EntregasPaquetes
            .Include(e => e.RutaReparto)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<List<EntregaPaquete>> GetByRutaAsync(int rutaId)
        => await _context.EntregasPaquetes
            .Where(e => e.RutaRepartoId == rutaId)
            .OrderBy(e => e.OrdenEnRuta)
            .ToListAsync();

    public async Task<List<EntregaPaquete>> GetBySeguimientoAsync(string numeroSeguimiento)
        => await _context.EntregasPaquetes
            .Where(e => e.NumeroSeguimiento == numeroSeguimiento)
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync();

    public async Task<EntregaPaquete> CreateAsync(EntregaPaquete entrega)
    {
        _context.EntregasPaquetes.Add(entrega);
        await _context.SaveChangesAsync();
        return entrega;
    }

    public async Task UpdateAsync(EntregaPaquete entrega)
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountByExpedicionAsync(string numeroExpedicion)
        => await _context.EntregasPaquetes.CountAsync(e => e.NumeroExpedicion == numeroExpedicion);

    public async Task<List<EntregaPaquete>> GetByRutaIdsAsync(List<int> rutaIds)
        => await _context.EntregasPaquetes
            .Where(e => rutaIds.Contains(e.RutaRepartoId))
            .ToListAsync();
}
