using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.Hubs;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

/// <summary>
/// Tests unitarios para BroadcastService.
/// </summary>
public class BroadcastServiceTests
{
    private readonly Mock<IHubContext<IntranetHub>> _mockHub = new();
    private readonly Mock<IHubClients> _mockClients = new();
    private readonly Mock<IClientProxy> _mockClientProxy = new();

    public BroadcastServiceTests()
    {
        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockHub.Setup(h => h.Clients).Returns(_mockClients.Object);
        _mockClientProxy.Setup(p => p.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private BroadcastService BuildService() =>
        new BroadcastService(_mockHub.Object, NullLogger<BroadcastService>.Instance);

    // ═══════════════════════════════════════════
    //  Alcance "all"
    // ═══════════════════════════════════════════

    [Fact]
    public async Task BroadcastAsync_AlcanceTodos_DeberiaEnviarAClients_All()
    {
        var service = BuildService();
        var req = new BroadcastRequest { Titulo = "Test", Mensaje = "Hola", Alcance = "all" };

        await service.BroadcastAsync(req);

        _mockClients.Verify(c => c.All, Times.Once);
        _mockClientProxy.Verify(p => p.SendCoreAsync(
            "NotificacionBroadcast", It.IsAny<object[]>(), default), Times.Once);
    }

    // ═══════════════════════════════════════════
    //  Alcance "admin"
    // ═══════════════════════════════════════════

    [Fact]
    public async Task BroadcastAsync_AlcanceAdmin_DeberiaEnviarAlGrupoAdmin()
    {
        var service = BuildService();
        var req = new BroadcastRequest { Titulo = "Test", Mensaje = "Admin", Alcance = "admin" };

        await service.BroadcastAsync(req);

        _mockClients.Verify(c => c.Group("admin"), Times.Once);
        _mockClientProxy.Verify(p => p.SendCoreAsync(
            "NotificacionBroadcast", It.IsAny<object[]>(), default), Times.Once);
    }

    // ═══════════════════════════════════════════
    //  Alcance "cta"
    // ═══════════════════════════════════════════

    [Fact]
    public async Task BroadcastAsync_AlcanceCta_SinCtaId_DeberiaLanzarArgumentException()
    {
        var service = BuildService();
        var req = new BroadcastRequest { Titulo = "Test", Alcance = "cta", CtaId = null };

        await Assert.ThrowsAsync<ArgumentException>(() => service.BroadcastAsync(req));
    }

    [Fact]
    public async Task BroadcastAsync_AlcanceCta_ConCtaId_DeberiaEnviarAlGrupoCorrecto()
    {
        var service = BuildService();
        var req = new BroadcastRequest { Titulo = "Test", Alcance = "cta", CtaId = 3 };

        await service.BroadcastAsync(req);

        _mockClients.Verify(c => c.Group("cta-3"), Times.Once);
    }

    // ═══════════════════════════════════════════
    //  Alcance "cta-rol"
    // ═══════════════════════════════════════════

    [Fact]
    public async Task BroadcastAsync_AlcanceCtaRol_SinCtaId_DeberiaLanzarArgumentException()
    {
        var service = BuildService();
        var req = new BroadcastRequest { Titulo = "Test", Alcance = "cta-rol", CtaId = null, Rol = "supervisor" };

        await Assert.ThrowsAsync<ArgumentException>(() => service.BroadcastAsync(req));
    }

    [Fact]
    public async Task BroadcastAsync_AlcanceCtaRol_RolInvalido_DeberiaLanzarArgumentException()
    {
        var service = BuildService();
        var req = new BroadcastRequest { Titulo = "Test", Alcance = "cta-rol", CtaId = 1, Rol = "ROL_INVALIDO" };

        await Assert.ThrowsAsync<ArgumentException>(() => service.BroadcastAsync(req));
    }

    [Fact]
    public async Task BroadcastAsync_AlcanceCtaRol_DatosValidos_DeberiaEnviarAlGrupoCorrecto()
    {
        var service = BuildService();
        var req = new BroadcastRequest { Titulo = "Test", Alcance = "cta-rol", CtaId = 2, Rol = "supervisor" };

        await service.BroadcastAsync(req);

        _mockClients.Verify(c => c.Group("cta-2-supervisor"), Times.Once);
    }
}
