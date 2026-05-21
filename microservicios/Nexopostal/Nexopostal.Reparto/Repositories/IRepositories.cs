using Nexopostal.Reparto.Models;

namespace Nexopostal.Reparto.Repositories;

/// <summary>Repositorio para Repartidores</summary>
public interface IRepartidorRepository
{
    Task<Repartidor?> GetByIdAsync(int id);
    Task<Repartidor?> GetByIdentityUserIdAsync(string identityUserId);
    Task<List<Repartidor>> GetAllAsync(int? oficinaJsonId = null, bool incluirInactivos = false);
    Task<Repartidor> CreateAsync(Repartidor repartidor);
    Task UpdateAsync(Repartidor repartidor);

    /// <summary>True si el repartidor tiene rutas en estado Planificada o EnCurso.</summary>
    Task<bool> TieneRutasActivasAsync(int repartidorId);
}

/// <summary>Repositorio para RutasReparto</summary>
public interface IRutaRepartoRepository
{
    Task<RutaReparto?> GetByIdAsync(int id);
    Task<RutaReparto?> GetByCodigoAsync(string codigo);
    Task<RutaReparto?> GetWithEntregasAsync(int id);
    Task<List<RutaReparto>> GetAllAsync(DateOnly? fecha = null, int? repartidorId = null);
    Task<RutaReparto> CreateAsync(RutaReparto ruta);
    Task UpdateAsync(RutaReparto ruta);
    Task<int> CountByFechaAsync(DateOnly fecha);
    Task<List<RutaReparto>> GetByFechaAsync(DateOnly fecha, int? oficinaJsonId = null);
}

/// <summary>Repositorio para EntregasPaquetes</summary>
public interface IEntregaPaqueteRepository
{
    Task<EntregaPaquete?> GetByIdAsync(int id);
    Task<EntregaPaquete?> GetWithRutaAsync(int id);
    Task<List<EntregaPaquete>> GetByRutaAsync(int rutaId);
    Task<List<EntregaPaquete>> GetBySeguimientoAsync(string numeroSeguimiento);
    Task<List<EntregaPaquete>> GetByExpedicionAsync(string numeroExpedicion);
    Task<EntregaPaquete> CreateAsync(EntregaPaquete entrega);
    Task UpdateAsync(EntregaPaquete entrega);
    Task<int> CountByExpedicionAsync(string numeroExpedicion);
    Task<List<EntregaPaquete>> GetByRutaIdsAsync(List<int> rutaIds);
}

/// <summary>Repositorio para ubicaciones GPS de los repartidores</summary>
public interface IUbicacionRepartidorRepository
{
    Task UpsertAsync(int repartidorId, double latitud, double longitud, int? rutaActivaId);
    Task<List<UbicacionRepartidor>> GetActivasAsync(TimeSpan ventana, int? oficinaJsonId = null);
}
