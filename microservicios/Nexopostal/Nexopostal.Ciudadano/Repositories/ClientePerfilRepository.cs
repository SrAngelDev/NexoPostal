using Microsoft.EntityFrameworkCore;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.Repositories;

/// <summary>
/// Implementación del repositorio de perfiles de cliente y direcciones.
/// </summary>
public class ClientePerfilRepository : IClientePerfilRepository
{
    private readonly CiudadanoDbContext _context;

    public ClientePerfilRepository(CiudadanoDbContext context)
    {
        _context = context;
    }

    public async Task<ClientePerfil?> GetByUserIdAsync(string userId)
    {
        return await _context.ClientePerfiles
            .Include(p => p.Agenda)
            .FirstOrDefaultAsync(p => p.IdentityUserId == userId);
    }

    public async Task<ClientePerfil> CreateOrUpdateAsync(ClientePerfil perfil)
    {
        var existing = await _context.ClientePerfiles
            .FirstOrDefaultAsync(p => p.IdentityUserId == perfil.IdentityUserId);

        if (existing == null)
        {
            _context.ClientePerfiles.Add(perfil);
        }
        else
        {
            existing.DNI = perfil.DNI;
            existing.Telefono = perfil.Telefono;
            existing.DireccionPredeterminada = perfil.DireccionPredeterminada;
        }

        await _context.SaveChangesAsync();
        return existing ?? perfil;
    }

    public async Task<List<DireccionFavorita>> GetDireccionesAsync(int clientePerfilId)
    {
        return await _context.DireccionesFavoritas
            .Where(d => d.ClientePerfilId == clientePerfilId)
            .OrderBy(d => d.Alias)
            .ToListAsync();
    }

    public async Task<DireccionFavorita?> GetDireccionByIdAsync(int id, int clientePerfilId)
    {
        return await _context.DireccionesFavoritas
            .FirstOrDefaultAsync(d => d.Id == id && d.ClientePerfilId == clientePerfilId);
    }

    public async Task<DireccionFavorita> AddDireccionAsync(DireccionFavorita direccion)
    {
        _context.DireccionesFavoritas.Add(direccion);
        await _context.SaveChangesAsync();
        return direccion;
    }

    public async Task UpdateDireccionAsync(DireccionFavorita direccion)
    {
        _context.DireccionesFavoritas.Update(direccion);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> DeleteDireccionAsync(int id, int clientePerfilId)
    {
        var direccion = await GetDireccionByIdAsync(id, clientePerfilId);
        if (direccion == null) return false;

        _context.DireccionesFavoritas.Remove(direccion);
        await _context.SaveChangesAsync();
        return true;
    }
}
