using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;

namespace Nexopostal.Reparto.Services;

/// <summary>
/// Servicio de gestión de vehículos de la flota.
///
/// Garantiza la coherencia entre la entidad <see cref="Vehiculo"/> y los campos embebidos
/// <see cref="Repartidor.TipoVehiculo"/> + <see cref="Repartidor.MatriculaVehiculo"/>:
/// al asignar/desasignar un vehículo se actualizan esos campos en el Repartidor.
/// </summary>
public interface IVehiculoService
{
    Task<List<Vehiculo>> ListarAsync(bool incluirInactivos = false, int? oficinaJsonId = null, int? repartidorId = null);
    Task<Vehiculo?> ObtenerAsync(int id);
    Task<(Vehiculo? vehiculo, string? error)> CrearAsync(CrearVehiculoDto dto, string? userId);
    Task<(Vehiculo? vehiculo, string? error)> ActualizarAsync(int id, ActualizarVehiculoDto dto, string? userId);
    Task<(bool ok, string? error)> DesactivarAsync(int id, string? userId);
    Task<(bool ok, string? error)> ReactivarAsync(int id, string? userId);
    Task<(Vehiculo? vehiculo, string? error)> AsignarAsync(int vehiculoId, int? repartidorId, string? userId);
    Task<ImportarDesdeRepartidoresResultDto> ImportarDesdeRepartidoresAsync(string? userId);
}

public class VehiculoService : IVehiculoService
{
    private readonly RepartoDbContext _db;
    private readonly IVehiculoRepository _repo;
    private readonly IRepartidorRepository _repartidorRepo;
    private readonly ILogger<VehiculoService> _logger;

    public VehiculoService(
        RepartoDbContext db,
        IVehiculoRepository repo,
        IRepartidorRepository repartidorRepo,
        ILogger<VehiculoService> logger)
    {
        _db = db;
        _repo = repo;
        _repartidorRepo = repartidorRepo;
        _logger = logger;
    }

    public Task<List<Vehiculo>> ListarAsync(bool incluirInactivos = false, int? oficinaJsonId = null, int? repartidorId = null)
        => _repo.GetAllAsync(incluirInactivos, oficinaJsonId, repartidorId);

    public Task<Vehiculo?> ObtenerAsync(int id) => _repo.GetByIdAsync(id);

    public async Task<(Vehiculo? vehiculo, string? error)> CrearAsync(CrearVehiculoDto dto, string? userId)
    {
        var matricula = dto.Matricula.Trim().ToUpperInvariant();
        if (await _repo.MatriculaExistsAsync(matricula))
            return (null, $"Ya existe un vehículo con matrícula '{matricula}'");

        var ahora = DateTime.UtcNow;
        var v = new Vehiculo
        {
            Matricula = matricula,
            Tipo = dto.Tipo,
            Marca = dto.Marca?.Trim(),
            Modelo = dto.Modelo?.Trim(),
            Color = dto.Color?.Trim(),
            AnioFabricacion = dto.AnioFabricacion,
            OficinaJsonId = dto.OficinaJsonId,
            Notas = dto.Notas?.Trim(),
            Activo = true,
            FechaAlta = ahora,
            FechaModificacion = ahora,
            ModificadoPorUserId = userId
        };
        await _repo.CreateAsync(v);
        _logger.LogInformation("Vehículo {Matricula} creado por {UserId}", v.Matricula, userId);
        return (v, null);
    }

    public async Task<(Vehiculo? vehiculo, string? error)> ActualizarAsync(int id, ActualizarVehiculoDto dto, string? userId)
    {
        var v = await _repo.GetByIdAsync(id);
        if (v == null) return (null, "Vehículo no encontrado");

        var matricula = dto.Matricula.Trim().ToUpperInvariant();
        if (matricula != v.Matricula && await _repo.MatriculaExistsAsync(matricula, id))
            return (null, $"Ya existe otro vehículo con matrícula '{matricula}'");

        v.Matricula = matricula;
        v.Tipo = dto.Tipo;
        v.Marca = dto.Marca?.Trim();
        v.Modelo = dto.Modelo?.Trim();
        v.Color = dto.Color?.Trim();
        v.AnioFabricacion = dto.AnioFabricacion;
        v.OficinaJsonId = dto.OficinaJsonId;
        v.Notas = dto.Notas?.Trim();
        v.FechaModificacion = DateTime.UtcNow;
        v.ModificadoPorUserId = userId;
        await _repo.UpdateAsync(v);

        // Si tiene repartidor asignado, sincronizar el embedded del Repartidor.
        if (v.RepartidorAsignadoId.HasValue)
        {
            var rep = await _repartidorRepo.GetByIdAsync(v.RepartidorAsignadoId.Value);
            if (rep != null)
            {
                rep.TipoVehiculo = v.Tipo;
                rep.MatriculaVehiculo = v.Matricula;
                await _repartidorRepo.UpdateAsync(rep);
            }
        }
        return (v, null);
    }

    public async Task<(bool ok, string? error)> DesactivarAsync(int id, string? userId)
    {
        var v = await _repo.GetByIdAsync(id);
        if (v == null) return (false, "Vehículo no encontrado");
        if (!v.Activo) return (false, "El vehículo ya está desactivado");

        if (v.RepartidorAsignadoId.HasValue)
            return (false, "No se puede desactivar un vehículo asignado a un repartidor. Desasígnalo primero.");

        v.Activo = false;
        v.FechaModificacion = DateTime.UtcNow;
        v.ModificadoPorUserId = userId;
        await _repo.UpdateAsync(v);
        return (true, null);
    }

    public async Task<(bool ok, string? error)> ReactivarAsync(int id, string? userId)
    {
        var v = await _repo.GetByIdAsync(id);
        if (v == null) return (false, "Vehículo no encontrado");
        if (v.Activo) return (false, "El vehículo ya está activo");

        v.Activo = true;
        v.FechaModificacion = DateTime.UtcNow;
        v.ModificadoPorUserId = userId;
        await _repo.UpdateAsync(v);
        return (true, null);
    }

    public async Task<(Vehiculo? vehiculo, string? error)> AsignarAsync(int vehiculoId, int? repartidorId, string? userId)
    {
        var v = await _repo.GetByIdAsync(vehiculoId);
        if (v == null) return (null, "Vehículo no encontrado");
        if (!v.Activo) return (null, "El vehículo está desactivado");

        using var tx = await _db.Database.BeginTransactionAsync();

        // 1. Si hay reasignación, liberar al repartidor anterior.
        if (v.RepartidorAsignadoId.HasValue && v.RepartidorAsignadoId != repartidorId)
        {
            var anterior = await _repartidorRepo.GetByIdAsync(v.RepartidorAsignadoId.Value);
            if (anterior != null)
            {
                anterior.MatriculaVehiculo = null;
                await _repartidorRepo.UpdateAsync(anterior);
            }
        }

        if (repartidorId.HasValue)
        {
            var rep = await _repartidorRepo.GetByIdAsync(repartidorId.Value);
            if (rep == null)
            {
                await tx.RollbackAsync();
                return (null, "Repartidor no encontrado");
            }
            if (!rep.Activo)
            {
                await tx.RollbackAsync();
                return (null, "El repartidor está desactivado");
            }

            // Si el repartidor ya tiene otro vehículo, lo liberamos.
            var actual = await _repo.GetByRepartidorAsync(rep.Id);
            if (actual != null && actual.Id != v.Id)
            {
                actual.RepartidorAsignadoId = null;
                actual.RepartidorAsignadoNombre = null;
                actual.FechaModificacion = DateTime.UtcNow;
                actual.ModificadoPorUserId = userId;
                await _repo.UpdateAsync(actual);
            }

            v.RepartidorAsignadoId = rep.Id;
            v.RepartidorAsignadoNombre = rep.NombreCompleto;

            // Sincronizar embedded en Repartidor.
            rep.TipoVehiculo = v.Tipo;
            rep.MatriculaVehiculo = v.Matricula;
            await _repartidorRepo.UpdateAsync(rep);
        }
        else
        {
            // Desasignar.
            v.RepartidorAsignadoId = null;
            v.RepartidorAsignadoNombre = null;
        }

        v.FechaModificacion = DateTime.UtcNow;
        v.ModificadoPorUserId = userId;
        await _repo.UpdateAsync(v);

        await tx.CommitAsync();
        _logger.LogInformation("Vehículo {Matricula} (re)asignado a repartidor {RepartidorId} por {UserId}",
            v.Matricula, repartidorId, userId);
        return (v, null);
    }

    public async Task<ImportarDesdeRepartidoresResultDto> ImportarDesdeRepartidoresAsync(string? userId)
    {
        var resultado = new ImportarDesdeRepartidoresResultDto();
        var ahora = DateTime.UtcNow;

        var repartidoresConVehiculo = await _db.Repartidores
            .Where(r => r.MatriculaVehiculo != null && r.MatriculaVehiculo != "")
            .ToListAsync();

        foreach (var rep in repartidoresConVehiculo)
        {
            var matricula = rep.MatriculaVehiculo!.Trim().ToUpperInvariant();
            var existente = await _repo.GetByMatriculaAsync(matricula);

            if (existente != null)
            {
                resultado.Omitidos++;
                resultado.Mensajes.Add($"Repartidor {rep.NombreCompleto}: matrícula {matricula} ya existe (omitido)");

                // Si el vehículo existente no está asignado, asignarlo a este repartidor.
                if (!existente.RepartidorAsignadoId.HasValue)
                {
                    existente.RepartidorAsignadoId = rep.Id;
                    existente.RepartidorAsignadoNombre = rep.NombreCompleto;
                    existente.FechaModificacion = ahora;
                    existente.ModificadoPorUserId = userId;
                    await _repo.UpdateAsync(existente);
                }
                continue;
            }

            var v = new Vehiculo
            {
                Matricula = matricula,
                Tipo = rep.TipoVehiculo,
                RepartidorAsignadoId = rep.Id,
                RepartidorAsignadoNombre = rep.NombreCompleto,
                OficinaJsonId = rep.OficinaJsonId,
                Activo = true,
                FechaAlta = ahora,
                FechaModificacion = ahora,
                ModificadoPorUserId = userId
            };
            await _repo.CreateAsync(v);
            resultado.Importados++;
            resultado.MatriculasImportadas.Add(matricula);
        }

        _logger.LogInformation("Importación desde repartidores: {Importados} importados, {Omitidos} omitidos",
            resultado.Importados, resultado.Omitidos);
        return resultado;
    }
}
