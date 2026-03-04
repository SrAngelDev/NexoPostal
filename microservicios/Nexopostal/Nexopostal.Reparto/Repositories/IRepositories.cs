using Nexopostal.Reparto.Models;

namespace Nexopostal.Reparto.Repositories;

/// <summary>Repositorio para Repartidores</summary>
public interface IRepartidorRepository
{
    Task<Repartidor?> GetByIdAsync(int id);
    Task<Repartidor?> GetByIdentityUserIdAsync(string identityUserId);
    Task<List<Repartidor>> GetAllAsync(int? oficinaJsonId = null);
    Task<Repartidor> CreateAsync(Repartidor repartidor);
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
    Task<EntregaPaquete> CreateAsync(EntregaPaquete entrega);
    Task UpdateAsync(EntregaPaquete entrega);
    Task<int> CountByExpedicionAsync(string numeroExpedicion);
    Task<List<EntregaPaquete>> GetByRutaIdsAsync(List<int> rutaIds);
}
