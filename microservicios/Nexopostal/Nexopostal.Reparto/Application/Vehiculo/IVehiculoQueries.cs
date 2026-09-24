using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.Vehiculos;
/// <summary>Contrato de lectura sin escrituras para Vehiculo.</summary>
public interface IVehiculoQueries
{
    Task<List<Vehiculo>> ListarAsync(bool incluirInactivos = false, int? oficinaJsonId = null, int? repartidorId = null);
    Task<Vehiculo?> ObtenerAsync(int id);
}
