using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class ClasificacionAutomaticaServiceTests
{
    private readonly Mock<ICentroTratamientoRepository> _cta = new();
    private readonly Mock<IRutaCtaRepository> _ruta = new();
    private readonly Mock<IMovimientoPaqueteRepository> _mov = new();

    private ClasificacionAutomaticaService Crear() => new(
        _cta.Object, _ruta.Object, _mov.Object,
        NullLogger<ClasificacionAutomaticaService>.Instance);

    [Fact]
    public async Task ClasificarPaquete_CpInvalido_LanzaArgumentException()
    {
        await FluentActions.Invoking(() => Crear().ClasificarPaquete("NXI-1", "1", 1m, false))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ClasificarPaquete_SinRuta_LanzaInvalidOperationException()
    {
        _ruta.Setup(r => r.GetByPrefijoAsync("28")).ReturnsAsync((RutaCta?)null);
        await FluentActions.Invoking(() => Crear().ClasificarPaquete("NXI-1", "28013", 1m, false))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Theory]
    [InlineData(true, 1, 1)]
    [InlineData(false, 6, 2)]
    [InlineData(false, 1, 3)]
    public async Task ClasificarPaquete_CalculaPrioridad(bool urgente, int peso, int esperado)
    {
        _ruta.Setup(r => r.GetByPrefijoAsync("28"))
             .ReturnsAsync(new RutaCta { Cta = new CentroTratamiento { Id = 9, Nombre = "MAD" } });
        var r = await Crear().ClasificarPaquete("NXI-1", "28013", (decimal)peso, urgente);
        r.Prioridad.Should().Be(esperado);
        r.CtaDestinoId.Should().Be(9);
        r.ZonaPostal.Should().Be("28");
    }

    [Fact]
    public async Task AgruparPorRuta_Vacio_DevuelveVacio()
    {
        var r = await Crear().AgruparPorRuta(new List<string>());
        r.Should().BeEmpty();
    }

    [Fact]
    public async Task AgruparPorRuta_SinMovimientos_LosOmite()
    {
        _mov.Setup(m => m.GetByExpedicionAsync("A")).ReturnsAsync(new List<MovimientoPaquete>());
        var r = await Crear().AgruparPorRuta(new List<string> { "A" });
        r.Should().BeEmpty();
    }

    [Fact]
    public async Task AgruparPorRuta_AgrupaPorZonaYCta()
    {
        _mov.Setup(m => m.GetByExpedicionAsync("A")).ReturnsAsync(new List<MovimientoPaquete>
        {
            new() { CtaDestinoId = 5, FechaCreacion = DateTime.UtcNow }
        });
        _mov.Setup(m => m.GetByExpedicionAsync("B")).ReturnsAsync(new List<MovimientoPaquete>
        {
            new() { CtaDestinoId = 5, FechaCreacion = DateTime.UtcNow }
        });
        _cta.Setup(c => c.GetByIdAsync(5)).ReturnsAsync(new CentroTratamiento { Id = 5, CodigoPostal = "28013" });

        var r = await Crear().AgruparPorRuta(new List<string> { "A", "B" });
        r.Should().ContainSingle();
        r[0].TotalPaquetes.Should().Be(2);
        r[0].ZonaPostal.Should().Be("28");
    }

    [Fact]
    public async Task AsignarCTADestino_DevuelveIdRuta()
    {
        _ruta.Setup(r => r.GetByPrefijoAsync("28"))
             .ReturnsAsync(new RutaCta { Cta = new CentroTratamiento { Id = 42, Nombre = "MAD" } });
        (await Crear().AsignarCTADestino("28001")).Should().Be(42);
    }

    [Fact]
    public async Task AsignarCTADestino_CpInvalido_Lanza()
    {
        await FluentActions.Invoking(() => Crear().AsignarCTADestino(""))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task AsignarCTADestino_SinRuta_Lanza()
    {
        _ruta.Setup(r => r.GetByPrefijoAsync("99")).ReturnsAsync((RutaCta?)null);
        await FluentActions.Invoking(() => Crear().AsignarCTADestino("99001"))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
