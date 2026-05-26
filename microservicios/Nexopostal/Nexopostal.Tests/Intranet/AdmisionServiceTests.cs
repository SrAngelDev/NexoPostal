using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

/// <summary>
/// Tests unitarios para AdmisionService.
/// </summary>
public class AdmisionServiceTests
{
    private readonly Mock<IMovimientoPaqueteRepository> _movimientoRepo = new();
    private readonly Mock<IClasificacionService> _clasificacionService = new();
    private readonly Mock<IOficinaPostalService> _oficinaService = new();
    private readonly Mock<INotificacionService> _notificacionService = new();
    private readonly Mock<IAsignacionService> _asignacionService = new();
    private readonly Mock<IOperarioOficinaRepository> _operarioOficinaRepo = new();

    private AdmisionService BuildService() => new AdmisionService(
        _movimientoRepo.Object,
        _clasificacionService.Object,
        _oficinaService.Object,
        _notificacionService.Object,
        _asignacionService.Object,
        _operarioOficinaRepo.Object,
        NullLogger<AdmisionService>.Instance);

    private static AdmisionPaqueteDto CrearDtoBase(string cpOrigen = "28001", string cpDestino = "08001") =>
        new AdmisionPaqueteDto
        {
            NumeroExpedicion = "EXP-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            CodigoPostalOrigen = cpOrigen,
            CodigoPostalDestino = cpDestino,
            EsUrgente = false,
            OperarioOficinaId = 1
        };

    // ═══════════════════════════════════════════
    //  CtaDestino no resolvible
    // ═══════════════════════════════════════════

    [Fact]
    public async Task AdmitirPaquete_CodigoPostalDestinoSinCta_DeberiaLanzarArgumentException()
    {
        _clasificacionService.Setup(s => s.ResolverCtaDestino(It.IsAny<string>()))
            .ReturnsAsync((ResolverCtaResponseDto?)null);

        var service = BuildService();
        var dto = CrearDtoBase(cpDestino: "99999");

        await Assert.ThrowsAsync<ArgumentException>(() => service.AdmitirPaquete(dto));
    }

    // ═══════════════════════════════════════════
    //  Sin movimiento troncal (mismo CTA)
    // ═══════════════════════════════════════════

    [Fact]
    public async Task AdmitirPaquete_OrigenYDestinoMismoCta_NoCreaMovimientoTroncal()
    {
        var ctaMAD = new ResolverCtaResponseDto { CtaId = 1, CtaCodigo = "CTA-MAD" };

        _clasificacionService.Setup(s => s.ResolverCtaDestino("28001")).ReturnsAsync(ctaMAD);
        _clasificacionService.Setup(s => s.ResolverCtaDestino("28080")).ReturnsAsync(ctaMAD);
        _notificacionService.Setup(n => n.NotificarNuevoPaqueteEnOficina(
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _oficinaService.Setup(o => o.ResolverOficinaPorCp(It.IsAny<string>()))
            .ReturnsAsync((ResolverOficinaCtaResponseDto?)null);

        var service = BuildService();
        var dto = CrearDtoBase(cpOrigen: "28001", cpDestino: "28080");

        var result = await service.AdmitirPaquete(dto);

        result.Should().NotBeNull();
        result.RequiereMovimientoTroncal.Should().BeFalse();
        _movimientoRepo.Verify(r => r.CreateAsync(It.IsAny<MovimientoPaquete>()), Times.Never);
    }

    // ═══════════════════════════════════════════
    //  Con movimiento troncal (CTAs distintos)
    // ═══════════════════════════════════════════

    [Fact]
    public async Task AdmitirPaquete_OrigenYDestinoCtaDistintos_CreaMovimientoTroncal()
    {
        var ctaMAD = new ResolverCtaResponseDto { CtaId = 1, CtaCodigo = "CTA-MAD" };
        var ctaBCN = new ResolverCtaResponseDto { CtaId = 2, CtaCodigo = "CTA-BCN" };

        _clasificacionService.Setup(s => s.ResolverCtaDestino("08001")).ReturnsAsync(ctaBCN);
        _clasificacionService.Setup(s => s.ResolverCtaDestino("28001")).ReturnsAsync(ctaMAD);
        _clasificacionService.Setup(s => s.DeterminarTipoTransporte(1, 2, false))
            .ReturnsAsync(TipoTransporte.Terrestre);
        _movimientoRepo.Setup(r => r.CreateAsync(It.IsAny<MovimientoPaquete>()))
            .ReturnsAsync((MovimientoPaquete m) => { m.Id = 100; return m; });
        _notificacionService.Setup(n => n.NotificarNuevoPaqueteEnOficina(
            It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<bool>(),
            It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _oficinaService.Setup(o => o.ResolverOficinaPorCp(It.IsAny<string>()))
            .ReturnsAsync((ResolverOficinaCtaResponseDto?)null);

        var service = BuildService();
        var dto = CrearDtoBase(cpOrigen: "28001", cpDestino: "08001");

        var result = await service.AdmitirPaquete(dto);

        result.Should().NotBeNull();
        result.RequiereMovimientoTroncal.Should().BeTrue();
        _movimientoRepo.Verify(r => r.CreateAsync(It.Is<MovimientoPaquete>(m =>
            m.CtaOrigenId == 1 && m.CtaDestinoId == 2)), Times.Once);
    }
}
