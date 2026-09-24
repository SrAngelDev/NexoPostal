using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Application.AdminEnvios;
/// <summary>Contrato de lectura sin escrituras para AdminEnvios.</summary>
public interface IAdminEnviosQueries
{
    Task<List<AdminEnvioListItemDto>> ListarAsync(EstadoEnvio? estado, EstadoInterno? estadoInterno, DateTime? fechaDesde, DateTime? fechaHasta, string? q, string? codigoPostal, bool? pagado, int limit);
    Task<AdminEnvioDetalleDto?> ObtenerAsync(string numeroSeguimiento);
}
