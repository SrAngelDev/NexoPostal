using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

public class ProcesoDevolucionServiceTests
{
    private readonly Mock<IEnvioRepository> _repo = new();

    private ProcesoDevolucionService Create() =>
        new(_repo.Object, NullLogger<ProcesoDevolucionService>.Instance);

    private static Envio Envio(EstadoInterno estado, decimal coste = 10m, string? obs = null)
        => new()
        {
            NumeroSeguimiento = "NX123ES",
            NumeroExpedicion = "NXI-1",
            EstadoInternoActual = estado,
            EstadoActual = EstadoEnvio.Admitido,
            CosteCalculado = coste,
            Observaciones = obs,
            FechaCreacion = DateTime.UtcNow
        };

    // ----- IniciarDevolucion -----

    [Fact]
    public async Task IniciarDevolucion_EnvioNoExiste_RetornaFalse()
    {
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync((Envio?)null);
        var ok = await Create().IniciarDevolucion("NXNULL", "motivo");
        ok.Should().BeFalse();
        _repo.Verify(r => r.UpdateAsync(It.IsAny<Envio>()), Times.Never);
    }

    [Theory]
    [InlineData(EstadoInterno.DevueltoAlRemitente)]
    [InlineData(EstadoInterno.EnDevolucionAlRemitente)]
    public async Task IniciarDevolucion_YaDevuelto_RetornaFalse(EstadoInterno estado)
    {
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync(Envio(estado));
        var ok = await Create().IniciarDevolucion("NX1", "motivo");
        ok.Should().BeFalse();
    }

    [Theory]
    [InlineData(EstadoInterno.EntregadoEnDomicilio)]
    [InlineData(EstadoInterno.EntregadoEnOficina)]
    [InlineData(EstadoInterno.EntregadoAAutorizado)]
    public async Task IniciarDevolucion_YaEntregado_RetornaFalse(EstadoInterno estado)
    {
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync(Envio(estado));
        var ok = await Create().IniciarDevolucion("NX1", "motivo");
        ok.Should().BeFalse();
    }

    [Fact]
    public async Task IniciarDevolucion_EnvioEnTransito_ActualizaEstadoYObservaciones()
    {
        var envio = Envio(EstadoInterno.EnTransitoHaciaCentroDestino);
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync(envio);
        var ok = await Create().IniciarDevolucion("NX1", "Cliente no localizado");
        ok.Should().BeTrue();
        envio.EstadoInternoActual.Should().Be(EstadoInterno.EnDevolucionAlRemitente);
        envio.EstadoActual.Should().Be(EstadoEnvio.Devuelto);
        envio.Observaciones.Should().Contain("Cliente no localizado");
        _repo.Verify(r => r.UpdateAsync(envio), Times.Once);
    }

    [Fact]
    public async Task IniciarDevolucion_ConObservacionesPrevias_AnexaConSeparador()
    {
        var envio = Envio(EstadoInterno.RecogidoEnOrigen, obs: "Previa");
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync(envio);
        await Create().IniciarDevolucion("NX1", "Nuevo motivo");
        envio.Observaciones.Should().StartWith("Previa | ");
        envio.Observaciones.Should().Contain("Nuevo motivo");
    }

    // ----- ProcesarDevolucionRecibida -----

    [Fact]
    public async Task ProcesarDevolucionRecibida_EnvioNoExiste_RetornaFalse()
    {
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync((Envio?)null);
        (await Create().ProcesarDevolucionRecibida("X")).Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarDevolucionRecibida_NoEnDevolucion_RetornaFalse()
    {
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync(Envio(EstadoInterno.EnReparto));
        (await Create().ProcesarDevolucionRecibida("X")).Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarDevolucionRecibida_EnDevolucion_MarcaDevueltoYActualiza()
    {
        var envio = Envio(EstadoInterno.EnDevolucionAlRemitente);
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync(envio);
        (await Create().ProcesarDevolucionRecibida("NX1")).Should().BeTrue();
        envio.EstadoInternoActual.Should().Be(EstadoInterno.DevueltoAlRemitente);
        envio.Observaciones.Should().Contain("devuelto al remitente");
    }

    // ----- CalcularReembolso -----

    [Fact]
    public async Task CalcularReembolso_EnvioNoExiste_DevuelveCero()
    {
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync((Envio?)null);
        (await Create().CalcularReembolso("X")).Should().Be(0m);
    }

    [Fact]
    public async Task CalcularReembolso_CosteAlto_80PorCiento()
    {
        // coste 100 → 80% = 80, comisión 20 (> 2) → reembolso 80
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync(Envio(EstadoInterno.EnReparto, 100m));
        var r = await Create().CalcularReembolso("X");
        r.Should().Be(80m);
    }

    [Fact]
    public async Task CalcularReembolso_CosteBajo_AplicaComisionMinima()
    {
        // coste 5 → 80% = 4, comisión 1 (< 2 mín) → reembolso ajustado a 5-2=3
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync(Envio(EstadoInterno.EnReparto, 5m));
        var r = await Create().CalcularReembolso("X");
        r.Should().Be(3m);
    }

    [Fact]
    public async Task CalcularReembolso_CosteMuyBajo_NoNegativo()
    {
        // coste 1 → 80% = 0.8, comisión 0.2 (< 2) → reembolso 1-2 = -1 → clamp 0
        _repo.Setup(r => r.GetByTrackingAsync(It.IsAny<string>())).ReturnsAsync(Envio(EstadoInterno.EnReparto, 1m));
        var r = await Create().CalcularReembolso("X");
        r.Should().Be(0m);
    }

    // ----- ObtenerDevolucionesPendientes -----

    [Fact]
    public async Task ObtenerDevolucionesPendientes_MapeaCamposYExtraeMotivo()
    {
        var envios = new List<Envio>
        {
            Envio(EstadoInterno.EnDevolucionAlRemitente, 10m,
                  obs: "[DEVOLUCIÓN 01/01/2025] Motivo: Destinatario ausente | otra cosa"),
            Envio(EstadoInterno.EnDevolucionAlRemitente, 50m, obs: null)
        };
        _repo.Setup(r => r.GetByEstadoInternoAsync(EstadoInterno.EnDevolucionAlRemitente, null))
             .ReturnsAsync(envios);

        var pendientes = await Create().ObtenerDevolucionesPendientes();
        pendientes.Should().HaveCount(2);
        pendientes[0].Motivo.Should().Be("Destinatario ausente");
        pendientes[0].ReembolsoEstimado.Should().Be(8m);    // 80% de 10
        pendientes[1].Motivo.Should().Be("Sin motivo especificado");
        pendientes[1].ReembolsoEstimado.Should().Be(40m);   // 80% de 50
    }
}
