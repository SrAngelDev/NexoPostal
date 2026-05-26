using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;
using Xunit;

namespace Nexopostal.Tests.Reparto;

/// <summary>
/// Tests unitarios para RepartoService.
/// </summary>
public class RepartoServiceTests
{
    private readonly Mock<IRepartidorRepository> _repartidorRepo = new();
    private readonly Mock<IRutaRepartoRepository> _rutaRepo = new();
    private readonly Mock<IEntregaPaqueteRepository> _entregaRepo = new();
    private readonly Mock<IUbicacionRepartidorRepository> _ubicacionRepo = new();
    private readonly Mock<IRepartoNotifier> _notifier = new();

    private RepartoService BuildService() => new RepartoService(
        _repartidorRepo.Object,
        _rutaRepo.Object,
        _entregaRepo.Object,
        _ubicacionRepo.Object,
        _notifier.Object,
        NullLogger<RepartoService>.Instance);

    // ═══════════════════════════════════════════
    //  CrearRepartidor
    // ═══════════════════════════════════════════

    [Theory]
    [InlineData("JefeReparto", "JefeReparto")]
    [InlineData("jefereparto", "JefeReparto")]
    [InlineData("JEFEREPARTO", "JefeReparto")]
    [InlineData("Repartidor", "Repartidor")]
    [InlineData("OtroRol", "Repartidor")]
    public async Task CrearRepartidor_RolNormalizado_DeberiaAsignarRolCorrecto(string rolEntrada, string rolEsperado)
    {
        _repartidorRepo.Setup(r => r.CreateAsync(It.IsAny<Repartidor>()))
            .ReturnsAsync((Repartidor r) => { r.Id = 1; return r; });

        var service = BuildService();
        var dto = new CrearRepartidorDto
        {
            IdentityUserId = "user-test-id",
            NombreCompleto = "Test Repartidor",
            CodigoEmpleado = "EMP-001",
            Rol = rolEntrada,
            TipoVehiculo = "Furgoneta",
            OficinaJsonId = 1,
            OficinaNombre = "Oficina Central"
        };

        var result = await service.CrearRepartidor(dto);

        result.Rol.Should().Be(rolEsperado);
    }

    // ═══════════════════════════════════════════
    //  EditarRepartidor
    // ═══════════════════════════════════════════

    [Fact]
    public async Task EditarRepartidor_IdInexistente_DeberiaRetornarError()
    {
        _repartidorRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Repartidor?)null);

        var service = BuildService();
        var dto = new EditarRepartidorDto
        {
            NombreCompleto = "Nuevo Nombre",
            TipoVehiculo = "Furgoneta",
            OficinaJsonId = 1,
            OficinaNombre = "Oficina Test"
        };

        var (repartidor, error) = await service.EditarRepartidor(999, dto);

        repartidor.Should().BeNull();
        error.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task EditarRepartidor_TipoVehiculoInvalido_DeberiaRetornarError()
    {
        _repartidorRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Repartidor { Id = 1, NombreCompleto = "Test", TipoVehiculo = TipoVehiculo.Furgoneta });

        var service = BuildService();
        var dto = new EditarRepartidorDto
        {
            NombreCompleto = "Test",
            TipoVehiculo = "TIPO_INVALIDO",
            OficinaJsonId = 1,
            OficinaNombre = "Oficina Test"
        };

        var (repartidor, error) = await service.EditarRepartidor(1, dto);

        repartidor.Should().BeNull();
        error.Should().Contain("TIPO_INVALIDO");
    }

    // ═══════════════════════════════════════════
    //  DesactivarRepartidor
    // ═══════════════════════════════════════════

    [Fact]
    public async Task DesactivarRepartidor_IdInexistente_DeberiaRetornarError()
    {
        _repartidorRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Repartidor?)null);

        var service = BuildService();
        var (ok, error) = await service.DesactivarRepartidor(999);

        ok.Should().BeFalse();
        error.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task DesactivarRepartidor_ConRutasActivas_DeberiaRetornarError()
    {
        _repartidorRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Repartidor { Id = 1, Activo = true });
        _repartidorRepo.Setup(r => r.TieneRutasActivasAsync(1)).ReturnsAsync(true);

        var service = BuildService();
        var (ok, error) = await service.DesactivarRepartidor(1);

        ok.Should().BeFalse();
        error.Should().Contain("rutas planificadas");
    }

    [Fact]
    public async Task DesactivarRepartidor_SinRutasActivas_DeberiaDesactivar()
    {
        var repartidor = new Repartidor { Id = 1, Activo = true };
        _repartidorRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(repartidor);
        _repartidorRepo.Setup(r => r.TieneRutasActivasAsync(1)).ReturnsAsync(false);
        _repartidorRepo.Setup(r => r.UpdateAsync(It.IsAny<Repartidor>())).Returns(Task.CompletedTask);

        var service = BuildService();
        var (ok, error) = await service.DesactivarRepartidor(1);

        ok.Should().BeTrue();
        error.Should().BeNull();
        repartidor.Activo.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    //  ObtenerRepartidores
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerRepartidores_SinFiltro_DeberiaRetornarTodosLosActivos()
    {
        var repartidores = new List<Repartidor>
        {
            new() { Id = 1, NombreCompleto = "Repartidor 1", Activo = true, TipoVehiculo = TipoVehiculo.Furgoneta, Rutas = [] },
            new() { Id = 2, NombreCompleto = "Repartidor 2", Activo = true, TipoVehiculo = TipoVehiculo.Moto, Rutas = [] }
        };
        _repartidorRepo.Setup(r => r.GetAllAsync(null, false)).ReturnsAsync(repartidores);

        var service = BuildService();
        var result = await service.ObtenerRepartidores();

        result.Should().HaveCount(2);
    }
}
