using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class GestionIncidenciasAutomaticaServiceTests
{
    private readonly Mock<IIncidenciaRepository> _inc = new();
    private readonly Mock<IMovimientoPaqueteRepository> _mov = new();
    private readonly Mock<IHistorialEstadoRepository> _hist = new();

    private GestionIncidenciasAutomaticaService Crear() => new(
        _inc.Object, _mov.Object, _hist.Object,
        NullLogger<GestionIncidenciasAutomaticaService>.Instance);

    [Fact]
    public async Task DetectarPaquetesSinMovimiento_DevuelveDistintos()
    {
        _mov.Setup(m => m.GetEnTransitoAnterioresAAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<MovimientoPaquete>
            {
                new() { NumeroExpedicion = "A" },
                new() { NumeroExpedicion = "A" },
                new() { NumeroExpedicion = "B" }
            });
        var r = await Crear().DetectarPaquetesSinMovimiento();
        r.Should().BeEquivalentTo(new[] { "A", "B" });
    }

    [Fact]
    public async Task CrearIncidenciasAutomaticas_OmiteExpedicionesConIncidenciaAbierta()
    {
        _mov.Setup(m => m.GetEnTransitoAnterioresAAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<MovimientoPaquete>
            {
                new() { NumeroExpedicion = "A", CtaOrigenId = 1, FechaCreacion = DateTime.UtcNow }
            });
        _inc.Setup(i => i.GetByExpedicionAsync("A")).ReturnsAsync(new List<Incidencia>
        {
            new() { Estado = EstadoIncidencia.Abierta }
        });

        var count = await Crear().CrearIncidenciasAutomaticas();
        count.Should().Be(0);
        _inc.Verify(i => i.CreateAsync(It.IsAny<Incidencia>()), Times.Never);
    }

    [Fact]
    public async Task CrearIncidenciasAutomaticas_CreaParaPaqueteEstancado()
    {
        var movEstancado = new MovimientoPaquete
        {
            NumeroExpedicion = "A", CtaOrigenId = 1, CtaDestinoId = 2,
            Estado = EstadoMovimiento.EnTransito, FechaCreacion = DateTime.UtcNow.AddHours(-50)
        };
        _mov.Setup(m => m.GetEnTransitoAnterioresAAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<MovimientoPaquete> { movEstancado });
        _inc.Setup(i => i.GetByExpedicionAsync("A")).ReturnsAsync(new List<Incidencia>());
        _mov.Setup(m => m.GetByExpedicionAsync("A")).ReturnsAsync(new List<MovimientoPaquete> { movEstancado });

        var count = await Crear().CrearIncidenciasAutomaticas();
        count.Should().BeGreaterThan(0);
        _inc.Verify(i => i.CreateAsync(It.Is<Incidencia>(x =>
            x.NumeroExpedicion == "A" &&
            x.Tipo == TipoIncidencia.PaqueteExtraviado &&
            x.Estado == EstadoIncidencia.Abierta)), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EscalarIncidencia_NoExiste_DevuelveFalse()
    {
        _inc.Setup(i => i.GetByIdAsync(1)).ReturnsAsync((Incidencia?)null);
        (await Crear().EscalarIncidencia(1)).Should().BeFalse();
    }

    [Fact]
    public async Task EscalarIncidencia_Abierta_PasaAEnRevision()
    {
        var inc = new Incidencia { Id = 1, Estado = EstadoIncidencia.Abierta, NumeroExpedicion = "A", Descripcion = "x" };
        _inc.Setup(i => i.GetByIdAsync(1)).ReturnsAsync(inc);
        (await Crear().EscalarIncidencia(1)).Should().BeTrue();
        inc.Estado.Should().Be(EstadoIncidencia.EnRevision);
        inc.Descripcion.Should().Contain("ESCALADA");
    }

    [Fact]
    public async Task EscalarIncidencia_EnRevision_AnadeNotaAdicional()
    {
        var inc = new Incidencia { Id = 1, Estado = EstadoIncidencia.EnRevision, NumeroExpedicion = "A", Descripcion = "x" };
        _inc.Setup(i => i.GetByIdAsync(1)).ReturnsAsync(inc);
        (await Crear().EscalarIncidencia(1)).Should().BeTrue();
        inc.Descripcion.Should().Contain("ADICIONAL");
    }

    [Fact]
    public async Task EscalarIncidencia_Resuelta_NoEscala()
    {
        _inc.Setup(i => i.GetByIdAsync(1)).ReturnsAsync(new Incidencia { Id = 1, Estado = EstadoIncidencia.Resuelta });
        (await Crear().EscalarIncidencia(1)).Should().BeFalse();
    }

    [Theory]
    [InlineData(TipoIncidencia.PaqueteDanado, "indemnización")]
    [InlineData(TipoIncidencia.PaqueteExtraviado, "trazabilidad")]
    [InlineData(TipoIncidencia.DireccionIncorrecta, "remitente")]
    [InlineData(TipoIncidencia.PaqueteRetenido, "aduanas")]
    [InlineData(TipoIncidencia.ErrorClasificacion, "rutas")]
    [InlineData(TipoIncidencia.Otra, "correctivas")]
    [InlineData(TipoIncidencia.PaqueteFueraDeTareas, "manualmente")]
    public async Task ProponerResolucion_PorTipo(TipoIncidencia tipo, string fragmento)
    {
        _inc.Setup(i => i.GetByIdAsync(1)).ReturnsAsync(new Incidencia { Id = 1, Tipo = tipo });
        var r = await Crear().ProponerResolucion(1);
        r.ToLowerInvariant().Should().Contain(fragmento.ToLowerInvariant());
    }

    [Fact]
    public async Task ProponerResolucion_NoExiste()
    {
        _inc.Setup(i => i.GetByIdAsync(99)).ReturnsAsync((Incidencia?)null);
        (await Crear().ProponerResolucion(99)).Should().Contain("No se encontró");
    }
}
