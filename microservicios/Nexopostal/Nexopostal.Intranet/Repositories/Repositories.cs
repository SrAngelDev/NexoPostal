using Microsoft.EntityFrameworkCore;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.Models;

namespace Nexopostal.Intranet.Repositories;

public class CentroTratamientoRepository : ICentroTratamientoRepository
{
    private readonly IntranetDbContext _context;
    public CentroTratamientoRepository(IntranetDbContext context) => _context = context;

    public async Task<CentroTratamiento?> GetByIdAsync(int id)
        => await _context.CentrosTratamiento.FindAsync(id);

    public async Task<CentroTratamiento?> GetByCodigoAsync(string codigo)
        => await _context.CentrosTratamiento.FirstOrDefaultAsync(c => c.Codigo == codigo);

    public async Task<List<CentroTratamiento>> GetAllAsync()
        => await _context.CentrosTratamiento.OrderBy(c => c.Area).ThenBy(c => c.Nombre).ToListAsync();

    public async Task<List<CentroTratamiento>> GetAllWithOperariosAsync()
        => await _context.CentrosTratamiento.Include(c => c.Operarios)
            .OrderBy(c => c.Area).ThenBy(c => c.Nombre).ToListAsync();

    public async Task<CentroTratamiento?> GetWithDetailAsync(int id)
        => await _context.CentrosTratamiento
            .Include(c => c.Operarios)
            .Include(c => c.RutasAsignadas)
            .FirstOrDefaultAsync(c => c.Id == id);
}

public class RutaCtaRepository : IRutaCtaRepository
{
    private readonly IntranetDbContext _context;
    public RutaCtaRepository(IntranetDbContext context) => _context = context;

    public async Task<RutaCta?> GetByPrefijoAsync(string prefijoCp)
        => await _context.RutasCta.Include(r => r.Cta).FirstOrDefaultAsync(r => r.PrefijoCp == prefijoCp);

    public async Task<List<RutaCta>> GetByCtaIdAsync(int ctaId)
        => await _context.RutasCta.Where(r => r.CtaId == ctaId).OrderBy(r => r.PrefijoCp).ToListAsync();
}

public class OperarioCtaRepository : IOperarioCtaRepository
{
    private readonly IntranetDbContext _context;
    public OperarioCtaRepository(IntranetDbContext context) => _context = context;

    public async Task<OperarioCta?> GetByIdAsync(int id)
        => await _context.OperariosCta.FindAsync(id);

    public async Task<OperarioCta?> GetByIdentityUserIdAsync(string identityUserId)
        => await _context.OperariosCta.Include(o => o.CentroTratamiento)
            .FirstOrDefaultAsync(o => o.IdentityUserId == identityUserId && o.Activo);

    public async Task<List<OperarioCta>> GetAllByIdentityUserIdAsync(string identityUserId)
        => await _context.OperariosCta.Include(o => o.CentroTratamiento)
            .Where(o => o.IdentityUserId == identityUserId && o.Activo)
            .OrderBy(o => o.CentroTratamiento.Codigo)
            .ToListAsync();

    public async Task<List<OperarioCta>> GetAllByIdentityUserIdIncludingInactiveAsync(string identityUserId)
        => await _context.OperariosCta.Include(o => o.CentroTratamiento)
            .Where(o => o.IdentityUserId == identityUserId)
            .OrderByDescending(o => o.Activo)
            .ThenBy(o => o.CentroTratamiento.Codigo)
            .ToListAsync();

    public async Task<OperarioCta?> GetWithCtaAsync(int id)
        => await _context.OperariosCta.Include(o => o.CentroTratamiento)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<List<OperarioCta>> GetByCtaIdAsync(int ctaId, bool? soloActivos = true)
    {
        var query = _context.OperariosCta.Where(o => o.CentroTratamientoId == ctaId);
        if (soloActivos == true)
            query = query.Where(o => o.Activo);
        return await query.ToListAsync();
    }

    public async Task<int> CountByCtaIdAsync(int ctaId, bool? soloActivos = null)
    {
        var query = _context.OperariosCta.Where(o => o.CentroTratamientoId == ctaId);
        if (soloActivos == true)
            query = query.Where(o => o.Activo);
        return await query.CountAsync();
    }

    public async Task<OperarioCta> CreateAsync(OperarioCta operario)
    {
        _context.OperariosCta.Add(operario);
        await _context.SaveChangesAsync();
        return operario;
    }

    public async Task UpdateAsync(OperarioCta operario)
    {
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByIdentityUserIdAndCtaAsync(string identityUserId, int ctaId)
        => await _context.OperariosCta.AnyAsync(o =>
            o.IdentityUserId == identityUserId && o.CentroTratamientoId == ctaId && o.Activo);
}

public class OperarioOficinaRepository : IOperarioOficinaRepository
{
    private readonly IntranetDbContext _context;
    public OperarioOficinaRepository(IntranetDbContext context) => _context = context;

    public async Task<OperarioOficina?> GetByIdAsync(int id)
        => await _context.OperariosOficina.FindAsync(id);

    public async Task<OperarioOficina?> GetByIdentityUserIdAsync(string identityUserId)
        => await _context.OperariosOficina.FirstOrDefaultAsync(o => o.IdentityUserId == identityUserId && o.Activo);

    public async Task<OperarioOficina?> GetByIdentityUserIdAnyAsync(string identityUserId)
        => await _context.OperariosOficina.FirstOrDefaultAsync(o => o.IdentityUserId == identityUserId);

    public async Task<List<OperarioOficina>> GetByOficinaAsync(int oficinaJsonId, bool soloActivos = true)
        => await _context.OperariosOficina
            .Where(o => o.OficinaJsonId == oficinaJsonId && (!soloActivos || o.Activo))
            .ToListAsync();

    public async Task<OperarioOficina> CreateAsync(OperarioOficina entity)
    {
        _context.OperariosOficina.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync(OperarioOficina entity)
    {
        _context.OperariosOficina.Update(entity);
        await _context.SaveChangesAsync();
    }
}

public class AsignacionPaqueteRepository : IAsignacionPaqueteRepository
{
    private readonly IntranetDbContext _context;
    public AsignacionPaqueteRepository(IntranetDbContext context) => _context = context;

    public async Task<AsignacionPaquete?> GetByIdAsync(int id)
        => await _context.AsignacionesPaquetes.FindAsync(id);

    public async Task<AsignacionPaquete?> GetDetailAsync(int id)
        => await _context.AsignacionesPaquetes
            .Include(a => a.OperarioAsignado)
            .Include(a => a.AsignadoPor)
            .Include(a => a.Cta)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<AsignacionPaquete>> GetByOperarioAsync(int operarioId, EstadoTarea estado)
        => await _context.AsignacionesPaquetes
            .Include(a => a.OperarioAsignado)
            .Include(a => a.AsignadoPor)
            .Where(a => a.OperarioAsignadoId == operarioId && a.EstadoTarea == estado)
            .OrderByDescending(a => a.EsUrgente)
            .ThenBy(a => estado == EstadoTarea.Pendiente ? a.FechaAsignacion : a.FechaInicio)
            .ToListAsync();

    public async Task<List<AsignacionPaquete>> GetByCtaAsync(int ctaId, EstadoTarea? filtroEstado = null)
    {
        var query = _context.AsignacionesPaquetes
            .Include(a => a.OperarioAsignado)
            .Include(a => a.AsignadoPor)
            .Where(a => a.CtaId == ctaId);

        if (filtroEstado.HasValue)
            query = query.Where(a => a.EstadoTarea == filtroEstado.Value);

        return await query
            .OrderByDescending(a => a.EsUrgente)
            .ThenByDescending(a => a.FechaAsignacion)
            .ToListAsync();
    }

    public async Task<AsignacionPaquete?> GetByExpedicionTipoCtaAsync(
        string numeroExpedicion,
        TipoTarea tipoTarea,
        int ctaId,
        bool incluirCanceladas = false)
    {
        var query = _context.AsignacionesPaquetes
            .Where(a =>
                a.NumeroExpedicion == numeroExpedicion &&
                a.TipoTarea == tipoTarea &&
                a.CtaId == ctaId);

        if (!incluirCanceladas)
        {
            query = query.Where(a => a.EstadoTarea != EstadoTarea.Cancelada);
        }

        return await query
            .OrderByDescending(a => a.FechaAsignacion)
            .FirstOrDefaultAsync();
    }

    public async Task<AsignacionPaquete> CreateAsync(AsignacionPaquete asignacion)
    {
        _context.AsignacionesPaquetes.Add(asignacion);
        await _context.SaveChangesAsync();
        return asignacion;
    }

    public async Task UpdateAsync(AsignacionPaquete asignacion)
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountByCtaAndEstadoAsync(int ctaId, EstadoTarea estado)
        => await _context.AsignacionesPaquetes.CountAsync(a => a.CtaId == ctaId && a.EstadoTarea == estado);

    public async Task<int> CountByCtaUrgentesAsync(int ctaId)
        => await _context.AsignacionesPaquetes.CountAsync(a =>
            a.CtaId == ctaId && a.EsUrgente &&
            (a.EstadoTarea == EstadoTarea.Pendiente || a.EstadoTarea == EstadoTarea.EnProgreso));

    public async Task<int> CountCompletadasHoyAsync(int ctaId)
    {
        var hoy = DateTime.UtcNow.Date;
        return await _context.AsignacionesPaquetes.CountAsync(a =>
            a.CtaId == ctaId && a.EstadoTarea == EstadoTarea.Completada &&
            a.FechaCompletada != null && a.FechaCompletada.Value.Date == hoy);
    }

    public async Task<int> CountByOperarioAndEstadoAsync(int operarioId, EstadoTarea estado)
        => await _context.AsignacionesPaquetes.CountAsync(a =>
            a.OperarioAsignadoId == operarioId && a.EstadoTarea == estado);

    public async Task<int> CountCompletadasHoyByOperarioAsync(int operarioId)
    {
        var hoy = DateTime.UtcNow.Date;
        return await _context.AsignacionesPaquetes.CountAsync(a =>
            a.OperarioAsignadoId == operarioId && a.EstadoTarea == EstadoTarea.Completada &&
            a.FechaCompletada != null && a.FechaCompletada.Value.Date == hoy);
    }
}

public class MovimientoPaqueteRepository : IMovimientoPaqueteRepository
{
    private readonly IntranetDbContext _context;
    public MovimientoPaqueteRepository(IntranetDbContext context) => _context = context;

    public async Task<MovimientoPaquete?> GetByIdAsync(int id)
        => await _context.MovimientosPaquetes.FindAsync(id);

    public async Task<MovimientoPaquete?> GetDetailAsync(int id)
        => await _context.MovimientosPaquetes
            .Include(m => m.CtaOrigen)
            .Include(m => m.CtaDestino)
            .FirstOrDefaultAsync(m => m.Id == id);

    public async Task<List<MovimientoPaquete>> GetByCtaAsync(int ctaId, EstadoMovimiento? filtroEstado = null)
    {
        var query = _context.MovimientosPaquetes
            .Include(m => m.CtaOrigen)
            .Include(m => m.CtaDestino)
            .Where(m => m.CtaOrigenId == ctaId || m.CtaDestinoId == ctaId);

        if (filtroEstado.HasValue)
            query = query.Where(m => m.Estado == filtroEstado.Value);

        return await query
            .OrderByDescending(m => m.EsUrgente)
            .ThenByDescending(m => m.FechaCreacion)
            .ToListAsync();
    }

    public async Task<List<MovimientoPaquete>> GetByExpedicionAsync(string numeroExpedicion)
        => await _context.MovimientosPaquetes
            .Include(m => m.CtaOrigen)
            .Include(m => m.CtaDestino)
            .Where(m => m.NumeroExpedicion == numeroExpedicion)
            .OrderBy(m => m.FechaCreacion)
            .ToListAsync();

    public async Task<MovimientoPaquete> CreateAsync(MovimientoPaquete movimiento)
    {
        _context.MovimientosPaquetes.Add(movimiento);
        await _context.SaveChangesAsync();
        return movimiento;
    }

    public async Task UpdateAsync(MovimientoPaquete movimiento)
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountByCtaAndEstadoAsync(int ctaId, EstadoMovimiento estado)
        => await _context.MovimientosPaquetes.CountAsync(m =>
            (m.CtaOrigenId == ctaId || m.CtaDestinoId == ctaId) && m.Estado == estado);

    public async Task<int> CountRecibidosHoyByCtaAsync(int ctaId)
    {
        var hoy = DateTime.UtcNow.Date;
        return await _context.MovimientosPaquetes.CountAsync(m =>
            m.CtaDestinoId == ctaId && m.Estado == EstadoMovimiento.Recibido &&
            m.FechaLlegada != null && m.FechaLlegada.Value.Date == hoy);
    }

    public async Task<List<MovimientoPaquete>> GetPendientesTransporteAsync()
        => await _context.MovimientosPaquetes
            .Include(m => m.CtaOrigen)
            .Include(m => m.CtaDestino)
            .Where(m => m.Estado == EstadoMovimiento.Programado)
            .OrderByDescending(m => m.EsUrgente)
            .ThenBy(m => m.FechaCreacion)
            .ToListAsync();

    public async Task<MovimientoPaquete?> GetProgramadoByExpedicionAndCtaOrigenAsync(string expedicion, int ctaOrigenId)
        => await _context.MovimientosPaquetes
            .Include(m => m.CtaOrigen)
            .Include(m => m.CtaDestino)
            .FirstOrDefaultAsync(m =>
                m.NumeroExpedicion == expedicion &&
                m.CtaOrigenId == ctaOrigenId &&
                m.Estado == EstadoMovimiento.Programado);

    public async Task<MovimientoPaquete?> GetEnTransitoByExpedicionAndCtaDestinoAsync(string expedicion, int ctaDestinoId)
        => await _context.MovimientosPaquetes
            .Include(m => m.CtaOrigen)
            .Include(m => m.CtaDestino)
            .FirstOrDefaultAsync(m =>
                m.NumeroExpedicion == expedicion &&
                m.CtaDestinoId == ctaDestinoId &&
                m.Estado == EstadoMovimiento.EnTransito);

    public async Task<MovimientoPaquete?> GetRecibidoByExpedicionAndCtaDestinoAsync(string expedicion, int ctaDestinoId)
        => await _context.MovimientosPaquetes
            .Include(m => m.CtaOrigen)
            .Include(m => m.CtaDestino)
            .FirstOrDefaultAsync(m =>
                m.NumeroExpedicion == expedicion &&
                m.CtaDestinoId == ctaDestinoId &&
                m.Estado == EstadoMovimiento.Recibido);

    public async Task<List<MovimientoPaquete>> GetEnTransitoAnterioresAAsync(DateTime umbral)
        => await _context.MovimientosPaquetes
            .Include(m => m.CtaOrigen)
            .Include(m => m.CtaDestino)
            .Where(m => m.Estado == EstadoMovimiento.EnTransito
                        && m.FechaSalida.HasValue
                        && m.FechaSalida.Value <= umbral)
            .ToListAsync();
}

public class IncidenciaRepository : IIncidenciaRepository
{
    private readonly IntranetDbContext _context;
    public IncidenciaRepository(IntranetDbContext context) => _context = context;

    public async Task<Incidencia?> GetByIdAsync(int id)
        => await _context.Incidencias.FindAsync(id);

    public async Task<Incidencia?> GetDetailAsync(int id)
        => await _context.Incidencias
            .Include(i => i.Cta)
            .Include(i => i.ReportadaPor)
            .FirstOrDefaultAsync(i => i.Id == id);

    public async Task<List<Incidencia>> GetByCtaAsync(int ctaId, EstadoIncidencia? filtroEstado = null)
    {
        var query = _context.Incidencias
            .Include(i => i.ReportadaPor)
            .Where(i => i.CtaId == ctaId);

        if (filtroEstado.HasValue)
            query = query.Where(i => i.Estado == filtroEstado.Value);

        return await query.OrderByDescending(i => i.FechaCreacion).ToListAsync();
    }

    public async Task<List<Incidencia>> GetByExpedicionAsync(string numeroExpedicion)
        => await _context.Incidencias
            .Include(i => i.ReportadaPor)
            .Where(i => i.NumeroExpedicion == numeroExpedicion)
            .OrderByDescending(i => i.FechaCreacion)
            .ToListAsync();

    public async Task<Incidencia> CreateAsync(Incidencia incidencia)
    {
        _context.Incidencias.Add(incidencia);
        await _context.SaveChangesAsync();
        return incidencia;
    }

    public async Task UpdateAsync(Incidencia incidencia)
    {
        await _context.SaveChangesAsync();
    }

    public async Task<int> CountByCtaAndEstadoAsync(int ctaId, EstadoIncidencia estado)
        => await _context.Incidencias.CountAsync(i => i.CtaId == ctaId && i.Estado == estado);
}

public class HistorialEstadoRepository : IHistorialEstadoRepository
{
    private readonly IntranetDbContext _context;
    public HistorialEstadoRepository(IntranetDbContext context) => _context = context;

    public async Task<HistorialEstado?> GetUltimoEventoAsync(string numeroExpedicion)
        => await _context.HistorialEstados
            .Where(h => h.NumeroExpedicion == numeroExpedicion)
            .OrderByDescending(h => h.FechaEvento)
            .FirstOrDefaultAsync();

    public async Task<List<HistorialEstado>> GetByExpedicionAsync(string numeroExpedicion)
        => await _context.HistorialEstados
            .Where(h => h.NumeroExpedicion == numeroExpedicion)
            .OrderBy(h => h.FechaEvento)
            .ToListAsync();

    public async Task<List<HistorialEstado>> GetPublicoByTrackingAsync(string numeroSeguimiento)
        => await _context.HistorialEstados
            .Where(h => h.NumeroSeguimiento == numeroSeguimiento && h.VisibleParaCliente)
            .OrderBy(h => h.FechaEvento)
            .ToListAsync();

    public async Task<HistorialEstado> CreateAsync(HistorialEstado historial)
    {
        _context.HistorialEstados.Add(historial);
        await _context.SaveChangesAsync();
        return historial;
    }

    public async Task<List<HistorialEstado>> GetExpedicionesPendientesEnEstadoAsync(string estado, DateTime umbral)
    {
        // Obtener el último evento de cada expedición y filtrar por estado y umbral
        var expedicionesConEstado = await _context.HistorialEstados
            .Where(h => h.Estado == estado && h.FechaEvento <= umbral)
            .Select(h => h.NumeroExpedicion)
            .Distinct()
            .ToListAsync();

        var resultado = new List<HistorialEstado>();
        foreach (var expedicion in expedicionesConEstado)
        {
            var ultimoEvento = await _context.HistorialEstados
                .Where(h => h.NumeroExpedicion == expedicion)
                .OrderByDescending(h => h.FechaEvento)
                .FirstOrDefaultAsync();

            if (ultimoEvento != null && ultimoEvento.Estado == estado)
                resultado.Add(ultimoEvento);
        }
        return resultado;
    }
}
