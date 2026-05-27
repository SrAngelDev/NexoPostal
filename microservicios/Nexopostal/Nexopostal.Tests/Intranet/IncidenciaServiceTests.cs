using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class IncidenciaServiceTests
{
    private readonly Mock<IIncidenciaRepository> _incRepo = new();
    private readonly Mock<IOperarioCtaRepository> _opRepo = new();
    private readonly Mock<ICentroTratamientoRepository> _ctaRepo = new();
    private readonly Mock<INotificacionService> _notif = new();

    private IncidenciaService Crear() => new(
        _incRepo.Object, _opRepo.Object, _ctaRepo.Object, _notif.Object,
        NullLogger<IncidenciaService>.Instance);

    private static Incidencia IncDetalle(int id) => new()
    {
        Id = id,
        NumeroExpedicion = "NXI-1",
        Tipo = TipoIncidencia.PaqueteDanado,
        Estado = EstadoIncidencia.Abierta,
        Descripcion = "d",
        CtaId = 7,
        Cta = new CentroTratamiento { Id = 7, Codigo = "CTA-7", Nombre = "C7" },
        ReportadaPor = new OperarioCta { Id = 1, NombreCompleto = "Ada", CodigoEmpleado = "E1" }
    };

    [Fact]
    public async Task CrearIncidencia_TipoInvalido_Lanza()
    {
        await FluentActions.Invoking(() => Crear().CrearIncidencia(
            new CrearIncidenciaDto { NumeroExpedicion = "X", Tipo = "NoExiste", Descripcion = "d" }, 1, 7))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CrearIncidencia_SupervisorNoEncontrado_Lanza()
    {
        _opRepo.Setup(o => o.GetWithCtaAsync(1)).ReturnsAsync((OperarioCta?)null);
        await FluentActions.Invoking(() => Crear().CrearIncidencia(
            new CrearIncidenciaDto { NumeroExpedicion = "X", Tipo = "PaqueteDanado", Descripcion = "d" }, 1, 7))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CrearIncidencia_Exito_NotificaYDevuelveDetalle()
    {
        _incRepo.Setup(i => i.CreateAsync(It.IsAny<Incidencia>()))
                .ReturnsAsync((Incidencia x) => { x.Id = 42; return x; });
        _opRepo.Setup(o => o.GetWithCtaAsync(1)).ReturnsAsync(new OperarioCta
        {
            Id = 1, NombreCompleto = "Ada", CodigoEmpleado = "E1",
            CentroTratamiento = new CentroTratamiento { Codigo = "CTA-7", Nombre = "C7" }
        });
        _incRepo.Setup(i => i.GetDetailAsync(It.IsAny<int>())).ReturnsAsync(IncDetalle(42));

        var r = await Crear().CrearIncidencia(
            new CrearIncidenciaDto { NumeroExpedicion = "NXI-1", Tipo = "PaqueteDanado", Descripcion = "d" }, 1, 7);

        r.Id.Should().Be(42);
        _notif.Verify(n => n.NotificarIncidenciaCreada(7, "CTA-7", "NXI-1", "PaqueteDanado", "Ada"), Times.Once);
    }

    [Fact]
    public async Task ActualizarIncidencia_NoExiste_Null()
    {
        _incRepo.Setup(i => i.GetByIdAsync(1)).ReturnsAsync((Incidencia?)null);
        (await Crear().ActualizarIncidencia(1, new ActualizarIncidenciaDto { Estado = "Abierta" })).Should().BeNull();
    }

    [Fact]
    public async Task ActualizarIncidencia_EstadoInvalido_Lanza()
    {
        _incRepo.Setup(i => i.GetByIdAsync(1)).ReturnsAsync(new Incidencia { Id = 1 });
        await FluentActions.Invoking(() => Crear().ActualizarIncidencia(1, new ActualizarIncidenciaDto { Estado = "WTF" }))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ActualizarIncidencia_ResueltaSinResolucion_Lanza()
    {
        _incRepo.Setup(i => i.GetByIdAsync(1)).ReturnsAsync(new Incidencia { Id = 1 });
        await FluentActions.Invoking(() => Crear().ActualizarIncidencia(1,
            new ActualizarIncidenciaDto { Estado = "Resuelta", Resolucion = "" }))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ActualizarIncidencia_ResueltaConResolucion_MarcaFechaYNotifica()
    {
        var inc = new Incidencia { Id = 1, NumeroExpedicion = "X", CtaId = 7, Tipo = TipoIncidencia.Otra };
        _incRepo.Setup(i => i.GetByIdAsync(1)).ReturnsAsync(inc);
        _ctaRepo.Setup(c => c.GetByIdAsync(7)).ReturnsAsync(new CentroTratamiento { Codigo = "CTA-7" });
        _incRepo.Setup(i => i.GetDetailAsync(1)).ReturnsAsync(IncDetalle(1));

        var r = await Crear().ActualizarIncidencia(1, new ActualizarIncidenciaDto { Estado = "Resuelta", Resolucion = "fix" });

        r.Should().NotBeNull();
        inc.Estado.Should().Be(EstadoIncidencia.Resuelta);
        inc.Resolucion.Should().Be("fix");
        inc.FechaResolucion.Should().NotBeNull();
        _notif.Verify(n => n.NotificarIncidenciaActualizada(7, "CTA-7", "X", "Otra", "Resuelta", "fix"), Times.Once);
    }

    [Fact]
    public async Task ObtenerIncidenciasCta_Mapea()
    {
        _incRepo.Setup(i => i.GetByCtaAsync(7, null)).ReturnsAsync(new List<Incidencia> { IncDetalle(1) });
        var r = await Crear().ObtenerIncidenciasCta(7);
        r.Should().ContainSingle();
        r[0].ReportadaPor.Should().Be("Ada");
    }

    [Fact]
    public async Task ObtenerIncidenciasGlobales_Mapea()
    {
        _incRepo.Setup(i => i.GetAllAsync(null, null, null)).ReturnsAsync(new List<Incidencia> { IncDetalle(1) });
        var r = await Crear().ObtenerIncidenciasGlobales();
        r.Should().ContainSingle();
        r[0].CtaCodigo.Should().Be("CTA-7");
    }

    [Fact]
    public async Task ObtenerDetalle_NoExiste_Null()
    {
        _incRepo.Setup(i => i.GetDetailAsync(1)).ReturnsAsync((Incidencia?)null);
        (await Crear().ObtenerDetalle(1)).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerDetalle_Existe_Mapea()
    {
        _incRepo.Setup(i => i.GetDetailAsync(1)).ReturnsAsync(IncDetalle(1));
        var r = await Crear().ObtenerDetalle(1);
        r!.Id.Should().Be(1);
        r.CtaCodigo.Should().Be("CTA-7");
    }

    [Fact]
    public async Task ObtenerIncidenciasPaquete_Mapea()
    {
        _incRepo.Setup(i => i.GetByExpedicionAsync("NXI-1")).ReturnsAsync(new List<Incidencia> { IncDetalle(1) });
        var r = await Crear().ObtenerIncidenciasPaquete("NXI-1");
        r.Should().ContainSingle();
    }
}
