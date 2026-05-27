using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Ciudadano.Hubs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using System.Net;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

// ─── NotificacionClienteService ──────────────────────────────────────────────

public class NotificacionClienteServiceTests
{
    private readonly Mock<IHubContext<TrackingHub>> _hub = new();
    private readonly Mock<IHubClients> _clients = new();
    private readonly Mock<IClientProxy> _clientProxy = new();
    private readonly Mock<IEnvioRepository> _repo = new();

    public NotificacionClienteServiceTests()
    {
        _clients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
        _hub.Setup(h => h.Clients).Returns(_clients.Object);
        _clientProxy
            .Setup(c => c.SendCoreAsync(
                It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private NotificacionClienteService Create() =>
        new(_hub.Object, _repo.Object, NullLogger<NotificacionClienteService>.Instance);

    private static Envio Envio(string tracking = "NXP-1") => new()
    {
        NumeroSeguimiento = tracking,
        NombreDestinatario = "Ana",
        ApellidosDestinatario = "García"
    };

    // ── NotificarCambioEstado ──────────────────────────────────────────────

    [Fact]
    public async Task NotificarCambioEstado_EnvioEncontrado_EnviaSignalR()
    {
        _repo.Setup(r => r.GetByTrackingAsync("NXP-1")).ReturnsAsync(Envio());

        await Create().NotificarCambioEstado("NXP-1", "Admitido", "EnTransito");

        _clientProxy.Verify(c => c.SendCoreAsync(
            "notificacion", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarCambioEstado_EnvioNoEncontrado_NoEnviaSignalR()
    {
        _repo.Setup(r => r.GetByTrackingAsync("X")).ReturnsAsync((Envio?)null);

        await Create().NotificarCambioEstado("X", "A", "B");

        _clientProxy.Verify(c => c.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── NotificarEntregaCompletada ─────────────────────────────────────────

    [Fact]
    public async Task NotificarEntregaCompletada_EnvioEncontrado_EnviaSignalR()
    {
        _repo.Setup(r => r.GetByTrackingAsync("NXP-1")).ReturnsAsync(Envio());

        await Create().NotificarEntregaCompletada("NXP-1");

        _clientProxy.Verify(c => c.SendCoreAsync(
            "notificacion", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarEntregaCompletada_EnvioNoEncontrado_NoEnviaSignalR()
    {
        _repo.Setup(r => r.GetByTrackingAsync("X")).ReturnsAsync((Envio?)null);

        await Create().NotificarEntregaCompletada("X");

        _clientProxy.Verify(c => c.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── NotificarIncidencia ────────────────────────────────────────────────

    [Fact]
    public async Task NotificarIncidencia_EnvioEncontrado_EnviaSignalR()
    {
        _repo.Setup(r => r.GetByTrackingAsync("NXP-1")).ReturnsAsync(Envio());

        await Create().NotificarIncidencia("NXP-1", "Paquete dañado");

        _clientProxy.Verify(c => c.SendCoreAsync(
            "notificacion", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarIncidencia_EnvioNoEncontrado_NoEnviaSignalR()
    {
        _repo.Setup(r => r.GetByTrackingAsync("X")).ReturnsAsync((Envio?)null);

        await Create().NotificarIncidencia("X", "desc");

        _clientProxy.Verify(c => c.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ── NotificarRecordatorioRecogida ──────────────────────────────────────

    [Fact]
    public async Task NotificarRecordatorioRecogida_EnvioEncontrado_EnviaSignalR()
    {
        _repo.Setup(r => r.GetByTrackingAsync("NXP-1")).ReturnsAsync(Envio());

        await Create().NotificarRecordatorioRecogida("NXP-1", 3);

        _clientProxy.Verify(c => c.SendCoreAsync(
            "notificacion", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotificarRecordatorioRecogida_EnvioNoEncontrado_NoEnviaSignalR()
    {
        _repo.Setup(r => r.GetByTrackingAsync("X")).ReturnsAsync((Envio?)null);

        await Create().NotificarRecordatorioRecogida("X", 5);

        _clientProxy.Verify(c => c.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

// ─── LogisticaNotifierService ─────────────────────────────────────────────────

public class LogisticaNotifierServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public Func<HttpRequestMessage, HttpResponseMessage> Responder { get; set; } =
            _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Responder(request));
        }
    }

    private static (LogisticaNotifierService svc, StubHandler stub) CreateSvc(string? serviceKey = null)
    {
        var stub = new StubHandler();
        var http = new HttpClient(stub) { BaseAddress = new Uri("http://intranet-test/") };
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["IntranetSettings:ServiceKey"] = serviceKey ?? "test-key"
            })
            .Build();
        var svc = new LogisticaNotifierService(http, config, NullLogger<LogisticaNotifierService>.Instance);
        return (svc, stub);
    }

    [Fact]
    public async Task NotificarAdmision_Exito_LlamaEndpoint()
    {
        var (svc, stub) = CreateSvc();

        await svc.NotificarAdmisionAsync(
            "NXI-001", "28001",
            codigoPostalOrigen: "08001",
            remitente: "Juan",
            destinatario: "Ana",
            esUrgente: false,
            numeroSeguimiento: "NXP-001",
            direccionEntrega: "C/ Mayor 1",
            ciudadDestino: "Madrid",
            telefonoDestinatario: "600000001",
            oficinaOrigenId: 1,
            oficinaDestinoId: 2,
            tipoEntrega: "Domicilio");

        stub.Calls.Should().Be(1);
    }

    [Fact]
    public async Task NotificarAdmision_RespuestaError_NoLanzaExcepcion()
    {
        var (svc, stub) = CreateSvc();
        stub.Responder = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("{\"error\":\"internal\"}")
        };

        // No debe lanzar: la notificación logística es best-effort
        var ex = await Record.ExceptionAsync(() =>
            svc.NotificarAdmisionAsync("NXI-002", "28002"));

        ex.Should().BeNull();
        stub.Calls.Should().Be(1);
    }

    [Fact]
    public async Task NotificarAdmision_ExcepcionHttpClient_NoLanzaExcepcion()
    {
        var stub = new StubHandler();
        stub.Responder = _ => throw new HttpRequestException("Connection refused");
        var http = new HttpClient(stub) { BaseAddress = new Uri("http://intranet-test/") };
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var svc = new LogisticaNotifierService(http, config, NullLogger<LogisticaNotifierService>.Instance);

        var ex = await Record.ExceptionAsync(() =>
            svc.NotificarAdmisionAsync("NXI-003", "28003"));

        ex.Should().BeNull();
    }

    [Fact]
    public async Task NotificarAdmision_UrgenteSinTipoEntrega_UsaDomicilioDefault()
    {
        var (svc, stub) = CreateSvc();

        await svc.NotificarAdmisionAsync(
            "NXI-004", "08001",
            esUrgente: true);

        stub.Calls.Should().Be(1);
    }
}
