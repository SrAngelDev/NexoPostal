using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;

namespace Nexopostal.Ciudadano.Application.AdminEnvios;
/// <summary>Contrato de operaciones que modifican estado para AdminEnvios.</summary>
public interface IAdminEnviosCommands
{
    Task<(AdminEnvioDetalleDto? envio, string? error)> CambiarEstadoAsync(string numeroSeguimiento, CambiarEstadoEnvioDto dto, string? adminUserId);
    Task<(AdminEnvioDetalleDto? envio, string? error)> AnularAsync(string numeroSeguimiento, AccionEnvioDto dto, string? adminUserId);
    Task<(AdminEnvioDetalleDto? envio, string? error)> ReabrirAsync(string numeroSeguimiento, AccionEnvioDto dto, string? adminUserId);
}
