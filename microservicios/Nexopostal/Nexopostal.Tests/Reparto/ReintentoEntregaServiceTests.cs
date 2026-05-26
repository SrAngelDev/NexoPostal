using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;
using Xunit;

namespace Nexopostal.Tests.Reparto;

/// <summary>
/// Tests unitarios para ReintentoEntregaService.
/// </summary>
public class ReintentoEntregaServiceTests
{
    private readonly Mock<IEntregaPaqueteRepository> _entregaRepo = new();
    private readonly Mock<IRutaRepartoRepository> _rutaRepo = new();

    private ReintentoEntregaService BuildService() => new ReintentoEntregaService(
        _entregaRepo.Object,
        _rutaRepo.Object,
        NullLogger<ReintentoEntregaService>.Instance);

    // ═══════════════════════════════════════════
    //  ProgramarReintento
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProgramarReintento_EntregaNoExiste_DeberiaRetornarFalse()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((EntregaPaquete?)null);

        var service = BuildService();
        var result = await service.ProgramarReintento(999, "Motivo test");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProgramarReintento_EntregaEntregada_DeberiaRetornarFalse()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new EntregaPaquete { Id = 1, Estado = EstadoEntrega.Entregado, NumeroIntento = 1 });

        var service = BuildService();
        var result = await service.ProgramarReintento(1, "Motivo test");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProgramarReintento_EntregaPendiente_DeberiaRetornarFalse()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new EntregaPaquete { Id = 1, Estado = EstadoEntrega.Pendiente, NumeroIntento = 1 });

        var service = BuildService();
        var result = await service.ProgramarReintento(1, "Motivo test");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ProgramarReintento_PrimerIntentoFallido_DeberiaCrearNuevoIntentoYRetornarTrue()
    {
        var entregaOriginal = new EntregaPaquete
        {
            Id = 1,
            Estado = EstadoEntrega.Ausente,
            NumeroIntento = 1,
            RutaRepartoId = 10,
            NumeroExpedicion = "EXP-TEST-001",
            NumeroSeguimiento = "NX000TEST001ES",
            DireccionEntrega = "Calle Test 1",
            CodigoPostal = "28001",
            Ciudad = "Madrid",
            NombreDestinatario = "Test Destinatario",
            FechaCreacion = DateTime.UtcNow.AddDays(-1)
        };

        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entregaOriginal);
        _entregaRepo.Setup(r => r.GetByExpedicionAsync("EXP-TEST-001"))
            .ReturnsAsync(new List<EntregaPaquete> { entregaOriginal });
        _entregaRepo.Setup(r => r.CreateAsync(It.IsAny<EntregaPaquete>()))
            .ReturnsAsync((EntregaPaquete e) => { e.Id = 2; return e; });

        var service = BuildService();
        var result = await service.ProgramarReintento(1, "Cliente ausente");

        result.Should().BeTrue();
        _entregaRepo.Verify(r => r.CreateAsync(It.Is<EntregaPaquete>(e =>
            e.NumeroIntento == 2 && e.Estado == EstadoEntrega.Pendiente)), Times.Once);
    }

    [Fact]
    public async Task ProgramarReintento_SegundoIntentoFallido_DeberiaRetornarFalse()
    {
        // 2 intentos previos → DeterminarAccion → "DepositarOficina" → no crea nuevo intento
        var entregaOriginal = new EntregaPaquete
        {
            Id = 1,
            Estado = EstadoEntrega.Ausente,
            NumeroIntento = 2,
            RutaRepartoId = 10,
            NumeroExpedicion = "EXP-TEST-002",
            FechaCreacion = DateTime.UtcNow.AddDays(-2)
        };
        var intentosAnteriores = new List<EntregaPaquete>
        {
            new() { NumeroIntento = 1, NumeroExpedicion = "EXP-TEST-002", Estado = EstadoEntrega.Ausente, FechaCreacion = DateTime.UtcNow.AddDays(-3) },
            entregaOriginal
        };

        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entregaOriginal);
        _entregaRepo.Setup(r => r.GetByExpedicionAsync("EXP-TEST-002"))
            .ReturnsAsync(intentosAnteriores);

        var service = BuildService();
        var result = await service.ProgramarReintento(1, "Cliente ausente segunda vez");

        result.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    //  DeterminarAccion
    // ═══════════════════════════════════════════

    [Fact]
    public async Task DeterminarAccion_PrimerIntento_DeberiaRetornarReintentar()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new EntregaPaquete
            {
                Id = 1,
                NumeroExpedicion = "EXP-001",
                NumeroIntento = 1,
                FechaCreacion = DateTime.UtcNow.AddDays(-1)
            });
        _entregaRepo.Setup(r => r.GetByExpedicionAsync("EXP-001"))
            .ReturnsAsync(new List<EntregaPaquete>
            {
                new() { NumeroIntento = 1, FechaCreacion = DateTime.UtcNow.AddDays(-1) }
            });

        var service = BuildService();
        var result = await service.DeterminarAccion(1);

        result.Should().Be("Reintentar");
    }

    [Fact]
    public async Task DeterminarAccion_SegundoIntento_DeberiaRetornarDepositarOficina()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new EntregaPaquete
            {
                Id = 1,
                NumeroExpedicion = "EXP-002",
                NumeroIntento = 2,
                FechaCreacion = DateTime.UtcNow.AddDays(-2)
            });
        _entregaRepo.Setup(r => r.GetByExpedicionAsync("EXP-002"))
            .ReturnsAsync(new List<EntregaPaquete>
            {
                new() { NumeroIntento = 1, FechaCreacion = DateTime.UtcNow.AddDays(-3) },
                new() { NumeroIntento = 2, FechaCreacion = DateTime.UtcNow.AddDays(-2) }
            });

        var service = BuildService();
        var result = await service.DeterminarAccion(1);

        result.Should().Be("DepositarOficina");
    }
}
