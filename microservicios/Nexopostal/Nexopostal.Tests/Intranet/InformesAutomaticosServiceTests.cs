using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class InformesAutomaticosServiceTests
{
    private readonly Mock<IMovimientoPaqueteRepository> _mov = new();
    private readonly Mock<IIncidenciaRepository> _inc = new();
    private readonly Mock<IAsignacionPaqueteRepository> _asig = new();
    private readonly Mock<IOperarioCtaRepository> _op = new();
    private readonly Mock<ICentroTratamientoRepository> _cta = new();

    private InformesAutomaticosService Crear() => new(
        _mov.Object, _inc.Object, _asig.Object, _op.Object, _cta.Object,
        NullLogger<InformesAutomaticosService>.Instance);

    private void StubCtas(params CentroTratamiento[] ctas) =>
        _cta.Setup(c => c.GetAllAsync()).ReturnsAsync(ctas.ToList());

    [Fact]
    public async Task GenerarResumenDiario_AgregaPorCta()
    {
        StubCtas(new CentroTratamiento { Id = 1, Codigo = "A", Nombre = "A" });
        _mov.Setup(m => m.CountRecibidosHoyByCtaAsync(1)).ReturnsAsync(10);
        _mov.Setup(m => m.CountByCtaAndEstadoAsync(1, EstadoMovimiento.EnTransito)).ReturnsAsync(3);
        _mov.Setup(m => m.CountByCtaAndEstadoAsync(1, EstadoMovimiento.Recibido)).ReturnsAsync(7);
        _inc.Setup(i => i.CountByCtaAndEstadoAsync(1, EstadoIncidencia.Abierta)).ReturnsAsync(2);
        _inc.Setup(i => i.CountByCtaAndEstadoAsync(1, EstadoIncidencia.Resuelta)).ReturnsAsync(1);
        _op.Setup(o => o.CountByCtaIdAsync(1, true)).ReturnsAsync(5);
        _asig.Setup(a => a.CountCompletadasHoyAsync(1)).ReturnsAsync(4);
        _asig.Setup(a => a.CountByCtaAndEstadoAsync(1, EstadoTarea.Pendiente)).ReturnsAsync(1);
        _asig.Setup(a => a.CountByCtaAndEstadoAsync(1, EstadoTarea.EnProgreso)).ReturnsAsync(1);

        var r = await Crear().GenerarResumenDiario(DateTime.UtcNow);
        r.PaquetesRecibidos.Should().Be(10);
        r.PaquetesExpedidos.Should().Be(3);
        r.PaquetesEntregados.Should().Be(7);
        r.IncidenciasCreadas.Should().Be(2);
        r.IncidenciasResueltas.Should().Be(1);
        r.OperariosActivos.Should().Be(5);
        // 4 completadas / (1+1+4)=6 totales → 66.67%
        r.TasaEficiencia.Should().BeApproximately(66.67, 0.1);
    }

    [Fact]
    public async Task GenerarResumenDiario_SinTareas_EficienciaCero()
    {
        StubCtas(new CentroTratamiento { Id = 1 });
        // todos a 0 por default

        var r = await Crear().GenerarResumenDiario(DateTime.UtcNow);
        r.TasaEficiencia.Should().Be(0.0);
    }

    [Fact]
    public async Task GenerarResumenSemanal_Suma7Dias()
    {
        StubCtas(new CentroTratamiento { Id = 1 });
        _mov.Setup(m => m.CountRecibidosHoyByCtaAsync(1)).ReturnsAsync(1);

        var r = await Crear().GenerarResumenSemanal(new DateTime(2025, 1, 6));
        r.Dias.Should().HaveCount(7);
        r.TotalPaquetesProcesados.Should().Be(7); // 1 recibido cada día * 7
        r.FechaFin.Should().Be(new DateTime(2025, 1, 12));
    }

    [Fact]
    public async Task ObtenerAlertasActivas_SinNada_DevuelveVacio()
    {
        StubCtas();
        _mov.Setup(m => m.GetEnTransitoAnterioresAAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<MovimientoPaquete>());
        var r = await Crear().ObtenerAlertasActivas();
        r.Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerAlertasActivas_PaqueteSinMovimiento_CalculaSeveridad()
    {
        StubCtas();
        _mov.Setup(m => m.GetEnTransitoAnterioresAAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<MovimientoPaquete>
        {
            new() { NumeroExpedicion = "A", FechaSalida = DateTime.UtcNow.AddHours(-200) }, // Critica
            new() { NumeroExpedicion = "B", FechaSalida = DateTime.UtcNow.AddHours(-80) },  // Alta
            new() { NumeroExpedicion = "C", FechaSalida = DateTime.UtcNow.AddHours(-50) }   // Media
        });
        var r = await Crear().ObtenerAlertasActivas();
        r.Should().HaveCount(3);
        r.Should().Contain(a => a.Severidad == "Critica");
        r.Should().Contain(a => a.Severidad == "Alta");
        r.Should().Contain(a => a.Severidad == "Media");
    }

    [Fact]
    public async Task ObtenerAlertasActivas_IncidenciasNoResueltas_Y_Sobrecarga()
    {
        StubCtas(new CentroTratamiento { Id = 1, Codigo = "C1", Nombre = "C1" });
        _mov.Setup(m => m.GetEnTransitoAnterioresAAsync(It.IsAny<DateTime>())).ReturnsAsync(new List<MovimientoPaquete>());
        _inc.Setup(i => i.CountByCtaAndEstadoAsync(1, EstadoIncidencia.Abierta)).ReturnsAsync(8);
        _inc.Setup(i => i.CountByCtaAndEstadoAsync(1, EstadoIncidencia.EnRevision)).ReturnsAsync(2); // 10 → Critica
        _asig.Setup(a => a.CountByCtaAndEstadoAsync(1, EstadoTarea.Pendiente)).ReturnsAsync(40);
        _op.Setup(o => o.CountByCtaIdAsync(1, true)).ReturnsAsync(2); // carga = 20 → Critica

        var r = await Crear().ObtenerAlertasActivas();
        r.Should().Contain(a => a.Tipo == "IncidenciaNoResuelta" && a.Severidad == "Critica");
        r.Should().Contain(a => a.Tipo == "SobrecargaOperario" && a.Severidad == "Critica");
    }
}
