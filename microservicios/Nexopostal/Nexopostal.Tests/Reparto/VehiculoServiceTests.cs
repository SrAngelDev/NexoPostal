using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;
using Xunit;

namespace Nexopostal.Tests.Reparto;

/// <summary>
/// Tests unitarios para VehiculoService.
/// </summary>
public class VehiculoServiceTests
{
    private readonly Mock<IVehiculoRepository> _vehiculoRepo = new();
    private readonly Mock<IRepartidorRepository> _repartidorRepo = new();

    private RepartoDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<RepartoDbContext>()
            .UseInMemoryDatabase("InMemoryVehiculoTests_" + Guid.NewGuid())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new RepartoDbContext(options);
    }

    private VehiculoService BuildService(RepartoDbContext? db = null) => new VehiculoService(
        db ?? CreateInMemoryDb(),
        _vehiculoRepo.Object,
        _repartidorRepo.Object,
        NullLogger<VehiculoService>.Instance);

    // ═══════════════════════════════════════════
    //  CrearAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CrearVehiculo_MatriculaDuplicada_DeberiaRetornarError()
    {
        _vehiculoRepo.Setup(r => r.MatriculaExistsAsync("1234ABC", null)).ReturnsAsync(true);

        var service = BuildService();
        var dto = new CrearVehiculoDto { Matricula = "1234abc", Tipo = TipoVehiculo.Furgoneta, OficinaJsonId = 1 };

        var (vehiculo, error) = await service.CrearAsync(dto, "admin-id");

        vehiculo.Should().BeNull();
        error.Should().Contain("1234ABC");
    }

    [Fact]
    public async Task CrearVehiculo_MatriculaNueva_DeberiaCrearVehiculo()
    {
        _vehiculoRepo.Setup(r => r.MatriculaExistsAsync("5678XYZ", null)).ReturnsAsync(false);
        _vehiculoRepo.Setup(r => r.CreateAsync(It.IsAny<Vehiculo>()))
            .ReturnsAsync((Vehiculo v) => { v.Id = 1; return v; });

        var service = BuildService();
        var dto = new CrearVehiculoDto { Matricula = "5678xyz", Tipo = TipoVehiculo.Moto, OficinaJsonId = 1 };

        var (vehiculo, error) = await service.CrearAsync(dto, "admin-id");

        error.Should().BeNull();
        vehiculo.Should().NotBeNull();
        vehiculo!.Matricula.Should().Be("5678XYZ");
    }

    // ═══════════════════════════════════════════
    //  DesactivarAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task DesactivarVehiculo_VehiculoNoEncontrado_DeberiaRetornarError()
    {
        _vehiculoRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Vehiculo?)null);

        var service = BuildService();
        var (ok, error) = await service.DesactivarAsync(999, "admin-id");

        ok.Should().BeFalse();
        error.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task DesactivarVehiculo_AsignadoARepartidor_DeberiaRetornarError()
    {
        _vehiculoRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Vehiculo { Id = 1, Matricula = "TEST1", Activo = true, RepartidorAsignadoId = 5 });

        var service = BuildService();
        var (ok, error) = await service.DesactivarAsync(1, "admin-id");

        ok.Should().BeFalse();
        error.Should().Contain("asignado a un repartidor");
    }

    [Fact]
    public async Task DesactivarVehiculo_SinRepartidorAsignado_DeberiaDesactivar()
    {
        var vehiculo = new Vehiculo { Id = 1, Matricula = "TEST1", Activo = true, RepartidorAsignadoId = null };
        _vehiculoRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vehiculo);
        _vehiculoRepo.Setup(r => r.UpdateAsync(It.IsAny<Vehiculo>())).Returns(Task.CompletedTask);

        var service = BuildService();
        var (ok, error) = await service.DesactivarAsync(1, "admin-id");

        ok.Should().BeTrue();
        error.Should().BeNull();
        vehiculo.Activo.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    //  AsignarAsync
    // ═══════════════════════════════════════════

    [Fact]
    public async Task AsignarVehiculo_VehiculoNoEncontrado_DeberiaRetornarError()
    {
        _vehiculoRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Vehiculo?)null);

        var service = BuildService(CreateInMemoryDb());
        var (vehiculo, error) = await service.AsignarAsync(999, 1, "admin-id");

        vehiculo.Should().BeNull();
        error.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task AsignarVehiculo_RepartidorNoEncontrado_DeberiaRetornarError()
    {
        _vehiculoRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Vehiculo { Id = 1, Matricula = "TEST1", Activo = true, RepartidorAsignadoId = null });
        _repartidorRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Repartidor?)null);
        _vehiculoRepo.Setup(r => r.GetByRepartidorAsync(It.IsAny<int>())).ReturnsAsync((Vehiculo?)null);

        var db = CreateInMemoryDb();
        var service = BuildService(db);
        var (vehiculo, error) = await service.AsignarAsync(1, 999, "admin-id");

        vehiculo.Should().BeNull();
        error.Should().Contain("Repartidor no encontrado");
    }
}
