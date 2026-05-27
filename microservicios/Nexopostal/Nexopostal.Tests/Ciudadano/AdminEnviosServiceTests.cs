using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

public class AdminEnviosServiceTests
{
    private readonly Mock<IEnvioRepository> _repo = new();
    private readonly Mock<ITrackingNotificacionService> _tracking = new();

    private AdminEnviosService Create() =>
        new(_repo.Object, _tracking.Object, NullLogger<AdminEnviosService>.Instance);

    private static Envio E(string tracking = "NP-1", EstadoEnvio estado = EstadoEnvio.EnTransito, string? obs = null) => new()
    {
        NumeroSeguimiento = tracking,
        NumeroExpedicion = "EXP-1",
        FechaCreacion = DateTime.UtcNow,
        EstadoActual = estado,
        EstadoInternoActual = EstadoInterno.EnTransitoHaciaCentroDestino,
        Origen = "Madrid",
        Destino = "Barcelona",
        CodigoPostalDestino = "08001",
        CodigoPostalOrigen = "28001",
        NombreRemitente = "R",
        EmailRemitente = "r@x.es",
        NombreDestinatario = "D",
        TipoTarifa = "Estandar",
        CosteCalculado = 10m,
        IdentityUserId = "u1",
        Observaciones = obs
    };

    [Fact]
    public async Task ListarAsync_DelegaAlRepo_YMapeaItems()
    {
        _repo.Setup(r => r.GetAdminListAsync(null, null, null, null, null, null, null, 100))
             .ReturnsAsync(new List<Envio> { E("A"), E("B") });
        var lista = await Create().ListarAsync(null, null, null, null, null, null, null, 100);
        lista.Should().HaveCount(2);
        lista.Select(x => x.NumeroSeguimiento).Should().BeEquivalentTo(new[] { "A", "B" });
    }

    [Fact]
    public async Task ObtenerAsync_NoExiste_DevuelveNull()
    {
        _repo.Setup(r => r.GetByTrackingAsync("x")).ReturnsAsync((Envio?)null);
        (await Create().ObtenerAsync("x")).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerAsync_Existe_DevuelveDetalle()
    {
        _repo.Setup(r => r.GetByTrackingAsync("NP-1")).ReturnsAsync(E());
        var r = await Create().ObtenerAsync("NP-1");
        r.Should().NotBeNull();
        r!.NumeroSeguimiento.Should().Be("NP-1");
    }

    [Fact]
    public async Task CambiarEstadoAsync_NoExiste_DevuelveError()
    {
        _repo.Setup(r => r.GetByTrackingAsync("x")).ReturnsAsync((Envio?)null);
        var (envio, err) = await Create().CambiarEstadoAsync("x", new(), "admin");
        envio.Should().BeNull();
        err.Should().Be("Envío no encontrado");
    }

    [Fact]
    public async Task CambiarEstadoAsync_Existe_ActualizaEstadoYNotifica()
    {
        var e = E();
        _repo.Setup(r => r.GetByTrackingAsync("NP-1")).ReturnsAsync(e);
        var dto = new CambiarEstadoEnvioDto { EstadoPublico = EstadoEnvio.Entregado, EstadoInterno = EstadoInterno.EntregadoEnDomicilio, Motivo = "Confirmado" };
        var (envio, err) = await Create().CambiarEstadoAsync("NP-1", dto, "admin-1");
        err.Should().BeNull();
        envio.Should().NotBeNull();
        e.EstadoActual.Should().Be(EstadoEnvio.Entregado);
        e.EstadoInternoActual.Should().Be(EstadoInterno.EntregadoEnDomicilio);
        e.Observaciones.Should().Contain("Confirmado").And.Contain("admin-1");
        _repo.Verify(r => r.UpdateAsync(e), Times.Once);
        _tracking.Verify(t => t.NotificarCambioEstado("NP-1", "Entregado", "EntregadoEnDomicilio", It.IsAny<string>(), null), Times.Once);
    }

    [Fact]
    public async Task CambiarEstadoAsync_SinMotivo_NoIncluyeMotivoEnObservacion()
    {
        var e = E();
        _repo.Setup(r => r.GetByTrackingAsync("NP-1")).ReturnsAsync(e);
        var dto = new CambiarEstadoEnvioDto { EstadoPublico = EstadoEnvio.EnTransito, EstadoInterno = EstadoInterno.EnTransitoHaciaCentroDestino };
        await Create().CambiarEstadoAsync("NP-1", dto, null);
        e.Observaciones.Should().NotContain("Motivo:");
    }

    [Fact]
    public async Task CambiarEstadoAsync_NotificacionLanzaExcepcion_NoPropaga()
    {
        var e = E();
        _repo.Setup(r => r.GetByTrackingAsync("NP-1")).ReturnsAsync(e);
        _tracking.Setup(t => t.NotificarCambioEstado(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), null))
                 .ThrowsAsync(new InvalidOperationException("SignalR down"));
        var act = async () => await Create().CambiarEstadoAsync("NP-1", new CambiarEstadoEnvioDto { EstadoPublico = EstadoEnvio.Entregado, EstadoInterno = EstadoInterno.EntregadoEnDomicilio }, "a");
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task AnularAsync_NoExiste_Error()
    {
        _repo.Setup(r => r.GetByTrackingAsync("x")).ReturnsAsync((Envio?)null);
        var (e, err) = await Create().AnularAsync("x", new(), "a");
        e.Should().BeNull(); err.Should().Be("Envío no encontrado");
    }

    [Fact]
    public async Task AnularAsync_YaEntregado_Error()
    {
        _repo.Setup(r => r.GetByTrackingAsync("x")).ReturnsAsync(E(estado: EstadoEnvio.Entregado));
        var (_, err) = await Create().AnularAsync("x", new(), "a");
        err.Should().Be("No se puede anular un envío ya entregado");
    }

    [Fact]
    public async Task AnularAsync_YaDevuelto_Error()
    {
        _repo.Setup(r => r.GetByTrackingAsync("x")).ReturnsAsync(E(estado: EstadoEnvio.Devuelto));
        var (_, err) = await Create().AnularAsync("x", new(), "a");
        err.Should().Be("El envío ya está marcado como devuelto");
    }

    [Fact]
    public async Task AnularAsync_EnTransito_MarcaDevuelto()
    {
        var e = E();
        _repo.Setup(r => r.GetByTrackingAsync("NP-1")).ReturnsAsync(e);
        var (envio, err) = await Create().AnularAsync("NP-1", new AccionEnvioDto { Motivo = "Petición admin" }, "admin");
        err.Should().BeNull();
        envio.Should().NotBeNull();
        e.EstadoActual.Should().Be(EstadoEnvio.Devuelto);
        e.EstadoInternoActual.Should().Be(EstadoInterno.EnDevolucionAlRemitente);
        e.Observaciones.Should().Contain("ANULADO").And.Contain("Petición admin");
    }

    [Fact]
    public async Task ReabrirAsync_NoExiste_Error()
    {
        _repo.Setup(r => r.GetByTrackingAsync("x")).ReturnsAsync((Envio?)null);
        var (_, err) = await Create().ReabrirAsync("x", new(), "a");
        err.Should().Be("Envío no encontrado");
    }

    [Fact]
    public async Task ReabrirAsync_EnTransito_ErrorPorqueNoEsDevueltoNiIncidencia()
    {
        _repo.Setup(r => r.GetByTrackingAsync("NP-1")).ReturnsAsync(E());
        var (_, err) = await Create().ReabrirAsync("NP-1", new(), "a");
        err.Should().Contain("Devuelto o Incidencia");
    }

    [Fact]
    public async Task ReabrirAsync_Devuelto_VuelveAAdmitido()
    {
        var e = E(estado: EstadoEnvio.Devuelto);
        _repo.Setup(r => r.GetByTrackingAsync("NP-1")).ReturnsAsync(e);
        var (envio, err) = await Create().ReabrirAsync("NP-1", new AccionEnvioDto { Motivo = "Reclamación" }, "admin");
        err.Should().BeNull();
        envio.Should().NotBeNull();
        e.EstadoActual.Should().Be(EstadoEnvio.Admitido);
        e.EstadoInternoActual.Should().Be(EstadoInterno.PendienteRecogida);
        e.Observaciones.Should().Contain("REABIERTO");
    }

    [Fact]
    public async Task ObservacionesPrevias_SeConcatenanConSalto()
    {
        var e = E(obs: "Previa");
        _repo.Setup(r => r.GetByTrackingAsync("NP-1")).ReturnsAsync(e);
        await Create().CambiarEstadoAsync("NP-1", new CambiarEstadoEnvioDto { EstadoPublico = EstadoEnvio.Entregado, EstadoInterno = EstadoInterno.EntregadoEnDomicilio }, "a");
        e.Observaciones.Should().StartWith("Previa\n");
    }

    [Fact]
    public async Task Observaciones_SeTruncanA1000Caracteres()
    {
        var e = E(obs: new string('x', 1500));
        _repo.Setup(r => r.GetByTrackingAsync("NP-1")).ReturnsAsync(e);
        await Create().CambiarEstadoAsync("NP-1", new CambiarEstadoEnvioDto { EstadoPublico = EstadoEnvio.Entregado, EstadoInterno = EstadoInterno.EntregadoEnDomicilio }, "a");
        e.Observaciones!.Length.Should().Be(1000);
    }
}
