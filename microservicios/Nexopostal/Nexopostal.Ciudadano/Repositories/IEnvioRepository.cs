using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.Repositories;

/// <summary>
/// Repositorio para la gestión de envíos.
/// Encapsula todo el acceso a datos de la tabla Envios.
/// </summary>
public interface IEnvioRepository
{
    Task<Envio?> GetByTrackingAsync(string numeroSeguimiento);
    Task<Envio?> GetByExpedicionAsync(string numeroExpedicion);
    Task<Envio?> GetByTrackingAndUserAsync(string numeroSeguimiento, string userId);
    Task<Envio?> GetByStripeSessionAsync(string stripeSessionId);
    Task<List<Envio>> GetByUserAsync(string userId);
    Task<List<Envio>> GetByEstadoInternoAsync(EstadoInterno? estadoInterno, string? codigoPostal);
    Task<Envio> CreateAsync(Envio envio);
    Task UpdateAsync(Envio envio);
    Task<bool> ExistsAsync(string numeroSeguimiento);
}
