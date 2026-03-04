using Microsoft.EntityFrameworkCore;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.Repositories;

/// <summary>
/// Implementación del repositorio de envíos.
/// Toda la lógica de acceso a datos de Envios queda centralizada aquí.
/// </summary>
public class EnvioRepository : IEnvioRepository
{
    private readonly CiudadanoDbContext _context;

    public EnvioRepository(CiudadanoDbContext context)
    {
        _context = context;
    }

    public async Task<Envio?> GetByTrackingAsync(string numeroSeguimiento)
    {
        return await _context.Envios.FirstOrDefaultAsync(e => e.NumeroSeguimiento == numeroSeguimiento);
    }

    public async Task<Envio?> GetByExpedicionAsync(string numeroExpedicion)
    {
        return await _context.Envios.FirstOrDefaultAsync(e => e.NumeroExpedicion == numeroExpedicion);
    }

    public async Task<Envio?> GetByTrackingAndUserAsync(string numeroSeguimiento, string userId)
    {
        return await _context.Envios
            .FirstOrDefaultAsync(e => e.NumeroSeguimiento == numeroSeguimiento && e.IdentityUserId == userId);
    }

    public async Task<Envio?> GetByStripeSessionAsync(string stripeSessionId)
    {
        return await _context.Envios
            .FirstOrDefaultAsync(e => e.StripeSessionId == stripeSessionId);
    }

    public async Task<List<Envio>> GetByUserAsync(string userId)
    {
        return await _context.Envios
            .Where(e => e.IdentityUserId == userId && e.EstadoActual != EstadoEnvio.PendientePago)
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync();
    }

    public async Task<List<Envio>> GetByEstadoInternoAsync(EstadoInterno? estadoInterno, string? codigoPostal)
    {
        var query = _context.Envios
            .Where(e => e.EstadoActual != EstadoEnvio.PendientePago)
            .AsQueryable();

        if (estadoInterno.HasValue)
            query = query.Where(e => e.EstadoInternoActual == estadoInterno.Value);

        if (!string.IsNullOrEmpty(codigoPostal))
            query = query.Where(e => e.CodigoPostalDestino == codigoPostal);

        return await query
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync();
    }

    public async Task<Envio> CreateAsync(Envio envio)
    {
        _context.Envios.Add(envio);
        await _context.SaveChangesAsync();
        return envio;
    }

    public async Task UpdateAsync(Envio envio)
    {
        _context.Envios.Update(envio);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(string numeroSeguimiento)
    {
        return await _context.Envios.AnyAsync(e => e.NumeroSeguimiento == numeroSeguimiento);
    }
}
