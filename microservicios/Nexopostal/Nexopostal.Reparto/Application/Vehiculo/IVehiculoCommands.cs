using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.Vehiculos;
/// <summary>Contrato de operaciones que modifican estado para Vehiculo.</summary>
public interface IVehiculoCommands
{
    Task<(Vehiculo? vehiculo, string? error)> CrearAsync(CrearVehiculoDto dto, string? userId);
    Task<(Vehiculo? vehiculo, string? error)> ActualizarAsync(int id, ActualizarVehiculoDto dto, string? userId);
    Task<(bool ok, string? error)> DesactivarAsync(int id, string? userId);
    Task<(bool ok, string? error)> ReactivarAsync(int id, string? userId);
    Task<(Vehiculo? vehiculo, string? error)> AsignarAsync(int vehiculoId, int? repartidorId, string? userId);
    Task<ImportarDesdeRepartidoresResultDto> ImportarDesdeRepartidoresAsync(string? userId);
}
