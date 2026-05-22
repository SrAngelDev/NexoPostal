using Nexopostal.Intranet.Models;

namespace Nexopostal.Intranet.Repositories;

/// <summary>
/// Repositorio para Centros de Tratamiento Automatizado (CTAs) y rutas CP.
/// </summary>
public interface ICentroTratamientoRepository
{
    Task<CentroTratamiento?> GetByIdAsync(int id);
    Task<CentroTratamiento?> GetByCodigoAsync(string codigo);
    Task<List<CentroTratamiento>> GetAllAsync();
    Task<List<CentroTratamiento>> GetAllWithOperariosAsync();
    Task<CentroTratamiento?> GetWithDetailAsync(int id);
    Task<CentroTratamiento> CreateAsync(CentroTratamiento entity);
    Task UpdateAsync(CentroTratamiento entity);
}

/// <summary>
/// Repositorio para rutas de clasificación por código postal.
/// </summary>
public interface IRutaCtaRepository
{
    Task<RutaCta?> GetByPrefijoAsync(string prefijoCp);
    Task<List<RutaCta>> GetByCtaIdAsync(int ctaId);
}

/// <summary>
/// Repositorio para operarios de CTA.
/// </summary>
public interface IOperarioCtaRepository
{
    Task<OperarioCta?> GetByIdAsync(int id);
    Task<OperarioCta?> GetByIdentityUserIdAsync(string identityUserId);
    Task<List<OperarioCta>> GetAllByIdentityUserIdAsync(string identityUserId);
    Task<List<OperarioCta>> GetAllByIdentityUserIdIncludingInactiveAsync(string identityUserId);
    Task<OperarioCta?> GetWithCtaAsync(int id);
    Task<List<OperarioCta>> GetByCtaIdAsync(int ctaId, bool? soloActivos = true);
    Task<int> CountByCtaIdAsync(int ctaId, bool? soloActivos = null);
    Task<OperarioCta> CreateAsync(OperarioCta operario);
    Task UpdateAsync(OperarioCta operario);
    Task<bool> ExistsByIdentityUserIdAndCtaAsync(string identityUserId, int ctaId);
}

/// <summary>
/// Repositorio para operarios de oficinas postales.
/// </summary>
public interface IOperarioOficinaRepository
{
    Task<OperarioOficina?> GetByIdAsync(int id);
    Task<OperarioOficina?> GetByIdentityUserIdAsync(string identityUserId);
    Task<OperarioOficina?> GetByIdentityUserIdAnyAsync(string identityUserId);
    Task<List<OperarioOficina>> GetAllByIdentityUserIdAsync(string identityUserId);
    Task<List<OperarioOficina>> GetByOficinaAsync(int oficinaJsonId, bool soloActivos = true);
    Task<OperarioOficina> CreateAsync(OperarioOficina entity);
    Task UpdateAsync(OperarioOficina entity);
    Task UpdateRangeAsync(IEnumerable<OperarioOficina> entities);
}

/// <summary>
/// Repositorio para asignaciones de paquetes a operarios.
/// </summary>
public interface IAsignacionPaqueteRepository
{
    Task<AsignacionPaquete?> GetByIdAsync(int id);
    Task<AsignacionPaquete?> GetDetailAsync(int id);
    Task<List<AsignacionPaquete>> GetByOperarioAsync(int operarioId, EstadoTarea estado);
    Task<List<AsignacionPaquete>> GetByOperarioOficinaAsync(int operarioOficinaId, EstadoTarea? estado = null);
    Task<List<AsignacionPaquete>> GetByOficinaAsync(int oficinaJsonId, EstadoTarea? estado = null);
    Task<List<AsignacionPaquete>> GetByCtaAsync(int ctaId, EstadoTarea? filtroEstado = null);
    Task<AsignacionPaquete?> GetByExpedicionTipoCtaAsync(string numeroExpedicion, TipoTarea tipoTarea, int ctaId, bool incluirCanceladas = false);
    Task<AsignacionPaquete> CreateAsync(AsignacionPaquete asignacion);
    Task UpdateAsync(AsignacionPaquete asignacion);
    Task<int> CountByCtaAndEstadoAsync(int ctaId, EstadoTarea estado);
    Task<int> CountByCtaUrgentesAsync(int ctaId);
    Task<int> CountCompletadasHoyAsync(int ctaId);
    Task<int> CountByOperarioAndEstadoAsync(int operarioId, EstadoTarea estado);
    Task<int> CountCompletadasHoyByOperarioAsync(int operarioId);
}

/// <summary>
/// Repositorio para movimientos de paquetes entre CTAs (rutas troncales).
/// </summary>
public interface IMovimientoPaqueteRepository
{
    Task<MovimientoPaquete?> GetByIdAsync(int id);
    Task<MovimientoPaquete?> GetDetailAsync(int id);
    Task<List<MovimientoPaquete>> GetByCtaAsync(int ctaId, EstadoMovimiento? filtroEstado = null);
    Task<List<MovimientoPaquete>> GetAllAsync(EstadoMovimiento? filtroEstado = null, int? ctaOrigenId = null, int? ctaDestinoId = null);
    Task<List<MovimientoPaquete>> GetByExpedicionAsync(string numeroExpedicion);
    Task<MovimientoPaquete> CreateAsync(MovimientoPaquete movimiento);
    Task UpdateAsync(MovimientoPaquete movimiento);
    Task<int> CountByCtaAndEstadoAsync(int ctaId, EstadoMovimiento estado);
    Task<int> CountRecibidosHoyByCtaAsync(int ctaId);
    Task<List<MovimientoPaquete>> GetPendientesTransporteAsync();
    Task<MovimientoPaquete?> GetProgramadoByExpedicionAndCtaOrigenAsync(string expedicion, int ctaOrigenId);
    Task<MovimientoPaquete?> GetEnTransitoByExpedicionAndCtaDestinoAsync(string expedicion, int ctaDestinoId);
    Task<MovimientoPaquete?> GetRecibidoByExpedicionAndCtaDestinoAsync(string expedicion, int ctaDestinoId);
    Task<List<MovimientoPaquete>> GetEnTransitoAnterioresAAsync(DateTime umbral);
}

/// <summary>
/// Repositorio para incidencias.
/// </summary>
public interface IIncidenciaRepository
{
    Task<Incidencia?> GetByIdAsync(int id);
    Task<Incidencia?> GetDetailAsync(int id);
    Task<List<Incidencia>> GetByCtaAsync(int ctaId, EstadoIncidencia? filtroEstado = null);
    Task<List<Incidencia>> GetAllAsync(EstadoIncidencia? filtroEstado = null, int? ctaId = null, TipoIncidencia? tipo = null);
    Task<List<Incidencia>> GetByExpedicionAsync(string numeroExpedicion);
    Task<Incidencia> CreateAsync(Incidencia incidencia);
    Task UpdateAsync(Incidencia incidencia);
    Task<int> CountByCtaAndEstadoAsync(int ctaId, EstadoIncidencia estado);
}

/// <summary>
/// Repositorio para historial de estados (trazabilidad).
/// </summary>
public interface IHistorialEstadoRepository
{
    Task<HistorialEstado?> GetUltimoEventoAsync(string numeroExpedicion);
    Task<List<HistorialEstado>> GetByExpedicionAsync(string numeroExpedicion);
    Task<List<HistorialEstado>> GetPublicoByTrackingAsync(string numeroSeguimiento);
    Task<HistorialEstado> CreateAsync(HistorialEstado historial);
    /// <summary>
    /// Obtiene las expediciones cuyo último evento tiene el estado dado
    /// y fue registrado antes del umbral indicado.
    /// Retorna el último HistorialEstado por expedición.
    /// </summary>
    Task<List<HistorialEstado>> GetExpedicionesPendientesEnEstadoAsync(string estado, DateTime umbral);
}
