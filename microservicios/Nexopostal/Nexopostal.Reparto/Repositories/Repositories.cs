using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.Models;

namespace Nexopostal.Reparto.Repositories;

/// <summary>
/// Acceso a datos de repartidores y su relación con rutas activas.
/// </summary>
public class RepartidorRepository : IRepartidorRepository
{
    private readonly RepartoDbContext _context;
    public RepartidorRepository(RepartoDbContext context) => _context = context;

    /// <summary>Busca un repartidor por su identificador interno.</summary>
    public async Task<Repartidor?> GetByIdAsync(int id)
        => await _context.Repartidores.FindAsync(id);

    /// <summary>Recupera al repartidor vinculado a una cuenta de Identity junto con sus rutas.</summary>
    public async Task<Repartidor?> GetByIdentityUserIdAsync(string identityUserId)
        => await _context.Repartidores
            .Include(r => r.Rutas)
            .FirstOrDefaultAsync(r => r.IdentityUserId == identityUserId);

    /// <summary>Lista repartidores con filtros opcionales por oficina y estado activo.</summary>
    public async Task<List<Repartidor>> GetAllAsync(int? oficinaJsonId = null, bool incluirInactivos = false)
    {
        var query = _context.Repartidores.Include(r => r.Rutas).AsQueryable();
        if (!incluirInactivos)
            query = query.Where(r => r.Activo);
        if (oficinaJsonId.HasValue)
            query = query.Where(r => r.OficinaJsonId == oficinaJsonId.Value);
        return await query.OrderBy(r => r.NombreCompleto).ToListAsync();
    }

    /// <summary>Persiste un nuevo repartidor.</summary>
    public async Task<Repartidor> CreateAsync(Repartidor repartidor)
    {
        _context.Repartidores.Add(repartidor);
        await _context.SaveChangesAsync();
        return repartidor;
    }

    /// <summary>Guarda los cambios hechos sobre un repartidor existente.</summary>
    public async Task UpdateAsync(Repartidor repartidor)
    {
        _context.Repartidores.Update(repartidor);
        await _context.SaveChangesAsync();
    }

    /// <summary>Indica si el repartidor sigue teniendo rutas en curso o pendientes de salida.</summary>
    public async Task<bool> TieneRutasActivasAsync(int repartidorId)
    {
        return await _context.RutasReparto
            .AnyAsync(r => r.RepartidorId == repartidorId
                        && (r.Estado == EstadoRuta.Planificada || r.Estado == EstadoRuta.EnCurso));
    }
}

/// <summary>
/// Repositorio de rutas de reparto y sus entregas asociadas.
/// </summary>
public class RutaRepartoRepository : IRutaRepartoRepository
{
    private readonly RepartoDbContext _context;
    public RutaRepartoRepository(RepartoDbContext context) => _context = context;

    /// <summary>Obtiene una ruta por id con repartidor y entregas cargados.</summary>
    public async Task<RutaReparto?> GetByIdAsync(int id)
        => await _context.RutasReparto
            .Include(r => r.Repartidor)
            .Include(r => r.Entregas)
            .FirstOrDefaultAsync(r => r.Id == id);

    /// <summary>Busca una ruta por su código legible.</summary>
    public async Task<RutaReparto?> GetByCodigoAsync(string codigo)
        => await _context.RutasReparto
            .Include(r => r.Repartidor)
            .Include(r => r.Entregas)
            .FirstOrDefaultAsync(r => r.Codigo == codigo);

    /// <summary>Recupera una ruta con sus entregas cuando solo interesa el detalle operativo.</summary>
    public async Task<RutaReparto?> GetWithEntregasAsync(int id)
        => await _context.RutasReparto
            .Include(r => r.Entregas)
            .FirstOrDefaultAsync(r => r.Id == id);

    /// <summary>Lista rutas con filtros opcionales por fecha, repartidor y oficina.</summary>
    public async Task<List<RutaReparto>> GetAllAsync(DateOnly? fecha = null, int? repartidorId = null, int? oficinaJsonId = null)
    {
        var query = _context.RutasReparto
            .Include(r => r.Repartidor)
            .Include(r => r.Entregas)
            .AsQueryable();

        if (fecha.HasValue)
            query = query.Where(r => r.FechaReparto == fecha.Value);
        if (repartidorId.HasValue)
            query = query.Where(r => r.RepartidorId == repartidorId.Value);
        if (oficinaJsonId.HasValue)
            query = query.Where(r => r.OficinaOrigenJsonId == oficinaJsonId.Value);

        return await query
            .OrderByDescending(r => r.FechaReparto)
            .ThenBy(r => r.Codigo)
            .ToListAsync();
    }

            /// <summary>Crea una nueva ruta de reparto.</summary>
    public async Task<RutaReparto> CreateAsync(RutaReparto ruta)
    {
        _context.RutasReparto.Add(ruta);
        await _context.SaveChangesAsync();
        return ruta;
    }

    /// <summary>Confirma en base de datos los cambios sobre una ruta existente.</summary>
    public async Task UpdateAsync(RutaReparto ruta)
    {
        await _context.SaveChangesAsync();
    }

    /// <summary>Cuenta cuántas rutas se han planificado para una fecha.</summary>
    public async Task<int> CountByFechaAsync(DateOnly fecha)
        => await _context.RutasReparto.CountAsync(r => r.FechaReparto == fecha);

    /// <summary>Devuelve las rutas de una fecha, opcionalmente limitadas a una oficina.</summary>
    public async Task<List<RutaReparto>> GetByFechaAsync(DateOnly fecha, int? oficinaJsonId = null)
    {
        var query = _context.RutasReparto.Where(r => r.FechaReparto == fecha);
        if (oficinaJsonId.HasValue)
            query = query.Where(r => r.OficinaOrigenJsonId == oficinaJsonId.Value);
        return await query.ToListAsync();
    }
}

/// <summary>
/// Acceso a datos de entregas individuales dentro de las rutas de reparto.
/// </summary>
public class EntregaPaqueteRepository : IEntregaPaqueteRepository
{
    private readonly RepartoDbContext _context;
    public EntregaPaqueteRepository(RepartoDbContext context) => _context = context;

    /// <summary>Busca una entrega concreta por id.</summary>
    public async Task<EntregaPaquete?> GetByIdAsync(int id)
        => await _context.EntregasPaquetes.FindAsync(id);

    /// <summary>Recupera una entrega junto con su ruta asociada.</summary>
    public async Task<EntregaPaquete?> GetWithRutaAsync(int id)
        => await _context.EntregasPaquetes
            .Include(e => e.RutaReparto)
            .FirstOrDefaultAsync(e => e.Id == id);

    /// <summary>Lista las entregas de una ruta en el orden en que deben visitarse.</summary>
    public async Task<List<EntregaPaquete>> GetByRutaAsync(int rutaId)
        => await _context.EntregasPaquetes
            .Where(e => e.RutaRepartoId == rutaId)
            .OrderBy(e => e.OrdenEnRuta)
            .ToListAsync();

    /// <summary>Busca todas las entregas históricas de un número de seguimiento.</summary>
    public async Task<List<EntregaPaquete>> GetBySeguimientoAsync(string numeroSeguimiento)
        => await _context.EntregasPaquetes
            .Include(e => e.RutaReparto)
            .Where(e => e.NumeroSeguimiento == numeroSeguimiento)
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync();

    /// <summary>Busca todas las entregas históricas de un número de expedición.</summary>
    public async Task<List<EntregaPaquete>> GetByExpedicionAsync(string numeroExpedicion)
        => await _context.EntregasPaquetes
            .Include(e => e.RutaReparto)
            .Where(e => e.NumeroExpedicion == numeroExpedicion)
            .OrderByDescending(e => e.FechaCreacion)
            .ToListAsync();

    /// <summary>Crea una nueva entrega dentro de una ruta.</summary>
    public async Task<EntregaPaquete> CreateAsync(EntregaPaquete entrega)
    {
        _context.EntregasPaquetes.Add(entrega);
        await _context.SaveChangesAsync();
        return entrega;
    }

    /// <summary>Confirma cambios sobre una entrega ya existente.</summary>
    public async Task UpdateAsync(EntregaPaquete entrega)
    {
        await _context.SaveChangesAsync();
    }

    /// <summary>Cuenta cuántas veces se ha gestionado una expedición en entregas.</summary>
    public async Task<int> CountByExpedicionAsync(string numeroExpedicion)
        => await _context.EntregasPaquetes.CountAsync(e => e.NumeroExpedicion == numeroExpedicion);

    /// <summary>Recupera entregas de varias rutas de una sola vez para operaciones masivas.</summary>
    public async Task<List<EntregaPaquete>> GetByRutaIdsAsync(List<int> rutaIds)
        => await _context.EntregasPaquetes
            .Where(e => rutaIds.Contains(e.RutaRepartoId))
            .ToListAsync();
}

/// <summary>
/// Repositorio de la última ubicación conocida de cada repartidor.
/// </summary>
public class UbicacionRepartidorRepository : IUbicacionRepartidorRepository
{
    private readonly RepartoDbContext _context;
    public UbicacionRepartidorRepository(RepartoDbContext context) => _context = context;

    /// <summary>Inserta o actualiza la última posición reportada por un repartidor.</summary>
    public async Task UpsertAsync(int repartidorId, double latitud, double longitud, int? rutaActivaId)
    {
        var existente = await _context.UbicacionesRepartidores
            .FirstOrDefaultAsync(u => u.RepartidorId == repartidorId);

        if (existente == null)
        {
            _context.UbicacionesRepartidores.Add(new UbicacionRepartidor
            {
                RepartidorId = repartidorId,
                Latitud = latitud,
                Longitud = longitud,
                RutaActivaId = rutaActivaId,
                ActualizadoEn = DateTime.UtcNow
            });
        }
        else
        {
            existente.Latitud = latitud;
            existente.Longitud = longitud;
            existente.RutaActivaId = rutaActivaId;
            existente.ActualizadoEn = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>Devuelve las ubicaciones todavía vigentes dentro de la ventana de actividad indicada.</summary>
    public async Task<List<UbicacionRepartidor>> GetActivasAsync(TimeSpan ventana, int? oficinaJsonId = null)
    {
        var umbral = DateTime.UtcNow - ventana;
        var query = _context.UbicacionesRepartidores
            .Include(u => u.Repartidor)
            .Where(u => u.ActualizadoEn >= umbral);

        if (oficinaJsonId.HasValue)
            query = query.Where(u => u.Repartidor.OficinaJsonId == oficinaJsonId.Value);

        return await query
            .OrderByDescending(u => u.ActualizadoEn)
            .ToListAsync();
    }
}

/// <summary>
/// Acceso a la flota de vehículos usada por el módulo de reparto.
/// </summary>
public class VehiculoRepository : IVehiculoRepository
{
    private readonly RepartoDbContext _context;
    public VehiculoRepository(RepartoDbContext context) => _context = context;

    /// <summary>Lista vehículos con filtros opcionales por estado, oficina y repartidor.</summary>
    public async Task<List<Vehiculo>> GetAllAsync(bool incluirInactivos = false, int? oficinaJsonId = null, int? repartidorId = null)
    {
        var q = _context.Vehiculos.AsQueryable();
        if (!incluirInactivos) q = q.Where(v => v.Activo);
        if (oficinaJsonId.HasValue) q = q.Where(v => v.OficinaJsonId == oficinaJsonId.Value);
        if (repartidorId.HasValue) q = q.Where(v => v.RepartidorAsignadoId == repartidorId.Value);
        return await q.OrderBy(v => v.Matricula).ToListAsync();
    }

    /// <summary>Busca un vehículo por su id.</summary>
    public Task<Vehiculo?> GetByIdAsync(int id) =>
        _context.Vehiculos.FirstOrDefaultAsync(v => v.Id == id);

    /// <summary>Localiza un vehículo por matrícula.</summary>
    public Task<Vehiculo?> GetByMatriculaAsync(string matricula) =>
        _context.Vehiculos.FirstOrDefaultAsync(v => v.Matricula == matricula);

    /// <summary>Recupera el vehículo activo asignado a un repartidor, si existe.</summary>
    public Task<Vehiculo?> GetByRepartidorAsync(int repartidorId) =>
        _context.Vehiculos.FirstOrDefaultAsync(v => v.RepartidorAsignadoId == repartidorId && v.Activo);

    /// <summary>Comprueba si la matrícula ya está ocupada por otro vehículo.</summary>
    public Task<bool> MatriculaExistsAsync(string matricula, int? excluyendoId = null) =>
        _context.Vehiculos.AnyAsync(v => v.Matricula == matricula && (!excluyendoId.HasValue || v.Id != excluyendoId.Value));

    /// <summary>Da de alta un vehículo nuevo en la flota.</summary>
    public async Task<Vehiculo> CreateAsync(Vehiculo vehiculo)
    {
        _context.Vehiculos.Add(vehiculo);
        await _context.SaveChangesAsync();
        return vehiculo;
    }

    /// <summary>Guarda cambios sobre un vehículo existente.</summary>
    public async Task UpdateAsync(Vehiculo vehiculo)
    {
        _context.Vehiculos.Update(vehiculo);
        await _context.SaveChangesAsync();
    }
}


