using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;
using Xunit;

namespace Nexopostal.Tests.Reparto;

/// <summary>
/// Tests unitarios para BalanceoCargaService.
/// </summary>
public class BalanceoCargaServiceTests
{
    private readonly Mock<IRepartidorRepository> _repartidorRepo = new();
    private readonly Mock<IRutaRepartoRepository> _rutaRepo = new();
    private readonly Mock<IEntregaPaqueteRepository> _entregaRepo = new();

    private BalanceoCargaService BuildService() => new BalanceoCargaService(
        _repartidorRepo.Object,
        _rutaRepo.Object,
        _entregaRepo.Object,
        NullLogger<BalanceoCargaService>.Instance);

    // ═══════════════════════════════════════════
    //  BalancearCargaDiaria
    // ═══════════════════════════════════════════

    [Fact]
    public async Task BalancearCargaDiaria_SinRepartidoresActivos_DeberiaRetornarDiccionarioVacio()
    {
        _repartidorRepo.Setup(r => r.GetAllAsync(null, false))
            .ReturnsAsync(new List<Repartidor>());

        var service = BuildService();
        var result = await service.BalancearCargaDiaria(DateOnly.FromDateTime(DateTime.Today));

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task BalancearCargaDiaria_ConRepartidoresActivos_DeberiaRetornarEntrada_PorCadaRepartidor()
    {
        var repartidores = new List<Repartidor>
        {
            new() { Id = 1, NombreCompleto = "R1", Activo = true, TipoVehiculo = TipoVehiculo.Furgoneta, Rutas = [] },
            new() { Id = 2, NombreCompleto = "R2", Activo = true, TipoVehiculo = TipoVehiculo.Moto, Rutas = [] }
        };
        var fecha = DateOnly.FromDateTime(DateTime.Today);

        _repartidorRepo.Setup(r => r.GetAllAsync(null, false)).ReturnsAsync(repartidores);
        _rutaRepo.Setup(r => r.GetByFechaAsync(fecha, null)).ReturnsAsync(new List<RutaReparto>());
        _entregaRepo.Setup(r => r.GetByRutaIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<EntregaPaquete>());

        var service = BuildService();
        var result = await service.BalancearCargaDiaria(fecha);

        result.Should().ContainKey(1);
        result.Should().ContainKey(2);
    }

    // ═══════════════════════════════════════════
    //  CalcularCapacidadDisponible
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CalcularCapacidadDisponible_RepartidorSinRutas_DeberiaRetornarCapacidadMaxima()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today);

        _repartidorRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Repartidor { Id = 1, Activo = true, NombreCompleto = "Test", TipoVehiculo = TipoVehiculo.Furgoneta });
        _rutaRepo.Setup(r => r.GetAllAsync(fecha, 1)).ReturnsAsync(new List<RutaReparto>());

        var service = BuildService();
        var result = await service.CalcularCapacidadDisponible(1, fecha);

        result.Should().Be(30); // MaxEntregasPorRepartidorDia = 30
    }

    [Fact]
    public async Task CalcularCapacidadDisponible_RepartidorCon10EntregasPendientes_DeberiaRetornar20()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today);
        var rutaId = 100;
        var entregas = Enumerable.Range(1, 10)
            .Select(i => new EntregaPaquete
            {
                Id = i,
                RutaRepartoId = rutaId,
                Estado = EstadoEntrega.Pendiente
            })
            .ToList();
        var rutas = new List<RutaReparto>
        {
            new() { Id = rutaId, RepartidorId = 1, FechaReparto = fecha, Entregas = entregas }
        };

        _repartidorRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new Repartidor { Id = 1, Activo = true, NombreCompleto = "Test", TipoVehiculo = TipoVehiculo.Furgoneta });
        _rutaRepo.Setup(r => r.GetAllAsync(fecha, 1)).ReturnsAsync(rutas);
        _entregaRepo.Setup(r => r.GetByRutaIdsAsync(It.Is<List<int>>(l => l.Contains(rutaId))))
            .ReturnsAsync(entregas);

        var service = BuildService();
        var result = await service.CalcularCapacidadDisponible(1, fecha);

        result.Should().Be(20); // 30 - 10 = 20
    }

    // ═══════════════════════════════════════════
    //  ObtenerEstadisticasBalanceo
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerEstadisticasBalanceo_SinRepartidores_DeberiaRetornarEstadisticasVacias()
    {
        var fecha = DateOnly.FromDateTime(DateTime.Today);
        _repartidorRepo.Setup(r => r.GetAllAsync(null, false)).ReturnsAsync(new List<Repartidor>());
        _rutaRepo.Setup(r => r.GetByFechaAsync(fecha, null)).ReturnsAsync(new List<RutaReparto>());
        _entregaRepo.Setup(r => r.GetByRutaIdsAsync(It.IsAny<List<int>>()))
            .ReturnsAsync(new List<EntregaPaquete>());

        var service = BuildService();
        var result = await service.ObtenerEstadisticasBalanceo(fecha);

        result.Should().NotBeNull();
        result.TotalRepartidores.Should().Be(0);
        result.TotalEntregasPendientes.Should().Be(0);
        result.MediaEntregasPorRepartidor.Should().Be(0);
    }
}
