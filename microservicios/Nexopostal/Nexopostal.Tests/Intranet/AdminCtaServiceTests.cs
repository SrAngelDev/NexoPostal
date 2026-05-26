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
/// Tests unitarios para AdminCtaService.
/// </summary>
public class AdminCtaServiceTests
{
    private readonly Mock<ICentroTratamientoRepository> _ctaRepo = new();
    private readonly Mock<IOperarioCtaRepository> _operarioRepo = new();
    private readonly Mock<IAsignacionPaqueteRepository> _asignacionRepo = new();
    private readonly Mock<IMovimientoPaqueteRepository> _movimientoRepo = new();
    private readonly Mock<IClasificacionService> _clasificacionService = new();

    private AdminCtaService BuildService() => new AdminCtaService(
        _ctaRepo.Object,
        _operarioRepo.Object,
        _asignacionRepo.Object,
        _movimientoRepo.Object,
        _clasificacionService.Object,
        NullLogger<AdminCtaService>.Instance);

    // ═══════════════════════════════════════════
    //  CrearCta
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CrearCta_CodigoVacio_DeberiaRetornarError()
    {
        var service = BuildService();
        var dto = new CrearCtaDto { Codigo = "", Nombre = "CTA Test", Area = "Centro", Provincia = "Madrid", Ciudad = "Madrid", Direccion = "Calle Test", CodigoPostal = "28001" };

        var (cta, error) = await service.CrearCta(dto);

        cta.Should().BeNull();
        error.Should().Contain("código");
    }

    [Fact]
    public async Task CrearCta_AreaInvalida_DeberiaRetornarError()
    {
        var service = BuildService();
        var dto = new CrearCtaDto { Codigo = "CTA-TEST", Nombre = "CTA Test", Area = "AREA_INVALIDA", Provincia = "Madrid", Ciudad = "Madrid", Direccion = "Calle Test", CodigoPostal = "28001" };

        var (cta, error) = await service.CrearCta(dto);

        cta.Should().BeNull();
        error.Should().Contain("AREA_INVALIDA");
    }

    [Fact]
    public async Task CrearCta_CodigoDuplicado_DeberiaRetornarError()
    {
        _ctaRepo.Setup(r => r.GetByCodigoAsync("CTA-DUP"))
            .ReturnsAsync(new CentroTratamiento { Codigo = "CTA-DUP" });

        var service = BuildService();
        var dto = new CrearCtaDto { Codigo = "CTA-DUP", Nombre = "CTA Dup", Area = "Centro", Provincia = "Madrid", Ciudad = "Madrid", Direccion = "Calle", CodigoPostal = "28001" };

        var (cta, error) = await service.CrearCta(dto);

        cta.Should().BeNull();
        error.Should().Contain("CTA-DUP");
    }

    [Fact]
    public async Task CrearCta_DatosValidos_DeberiaCrearYRetornarDetalle()
    {
        _ctaRepo.Setup(r => r.GetByCodigoAsync(It.IsAny<string>())).ReturnsAsync((CentroTratamiento?)null);
        _ctaRepo.Setup(r => r.CreateAsync(It.IsAny<CentroTratamiento>()))
            .ReturnsAsync((CentroTratamiento c) => { c.Id = 10; return c; });
        _clasificacionService.Setup(s => s.ObtenerCtaDetalle(10))
            .ReturnsAsync(new CtaDetalleDto { Id = 10, Codigo = "CTA-MAD", Nombre = "CTA Madrid" });

        var service = BuildService();
        var dto = new CrearCtaDto { Codigo = "CTA-MAD", Nombre = "CTA Madrid", Area = "Centro", Provincia = "Madrid", Ciudad = "Madrid", Direccion = "Calle Gran Vía 1", CodigoPostal = "28013" };

        var (cta, error) = await service.CrearCta(dto);

        error.Should().BeNull();
        cta.Should().NotBeNull();
        cta!.Codigo.Should().Be("CTA-MAD");
    }

    // ═══════════════════════════════════════════
    //  DesactivarCta
    // ═══════════════════════════════════════════

    [Fact]
    public async Task DesactivarCta_CtaNoEncontrado_DeberiaRetornarError()
    {
        _ctaRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((CentroTratamiento?)null);

        var service = BuildService();
        var (ok, error) = await service.DesactivarCta(99);

        ok.Should().BeFalse();
        error.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task DesactivarCta_ConOperariosActivos_DeberiaRetornarError()
    {
        _ctaRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new CentroTratamiento { Id = 1, Codigo = "CTA-MAD", Activo = true });
        _operarioRepo.Setup(r => r.CountByCtaIdAsync(1, true)).ReturnsAsync(3);

        var service = BuildService();
        var (ok, error) = await service.DesactivarCta(1);

        ok.Should().BeFalse();
        error.Should().Contain("operarios activos");
    }

    [Fact]
    public async Task DesactivarCta_ConTareasPendientes_DeberiaRetornarError()
    {
        _ctaRepo.Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(new CentroTratamiento { Id = 1, Codigo = "CTA-MAD", Activo = true });
        _operarioRepo.Setup(r => r.CountByCtaIdAsync(1, true)).ReturnsAsync(0);
        _asignacionRepo.Setup(r => r.CountByCtaAndEstadoAsync(1, EstadoTarea.Pendiente)).ReturnsAsync(2);
        _asignacionRepo.Setup(r => r.CountByCtaAndEstadoAsync(1, EstadoTarea.EnProgreso)).ReturnsAsync(0);

        var service = BuildService();
        var (ok, error) = await service.DesactivarCta(1);

        ok.Should().BeFalse();
        error.Should().Contain("tareas pendientes");
    }

    [Fact]
    public async Task DesactivarCta_SinDependencias_DeberiaDesactivar()
    {
        var cta = new CentroTratamiento { Id = 1, Codigo = "CTA-MAD", Activo = true };
        _ctaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(cta);
        _operarioRepo.Setup(r => r.CountByCtaIdAsync(1, true)).ReturnsAsync(0);
        _asignacionRepo.Setup(r => r.CountByCtaAndEstadoAsync(1, EstadoTarea.Pendiente)).ReturnsAsync(0);
        _asignacionRepo.Setup(r => r.CountByCtaAndEstadoAsync(1, EstadoTarea.EnProgreso)).ReturnsAsync(0);
        _movimientoRepo.Setup(r => r.CountByCtaAndEstadoAsync(1, EstadoMovimiento.Programado)).ReturnsAsync(0);
        _movimientoRepo.Setup(r => r.CountByCtaAndEstadoAsync(1, EstadoMovimiento.EnTransito)).ReturnsAsync(0);
        _ctaRepo.Setup(r => r.UpdateAsync(It.IsAny<CentroTratamiento>())).Returns(Task.CompletedTask);

        var service = BuildService();
        var (ok, error) = await service.DesactivarCta(1);

        ok.Should().BeTrue();
        error.Should().BeNull();
        cta.Activo.Should().BeFalse();
    }

    // ═══════════════════════════════════════════
    //  ReactivarCta
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ReactivarCta_CtaNoEncontrado_DeberiaRetornarError()
    {
        _ctaRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((CentroTratamiento?)null);

        var service = BuildService();
        var (ok, error) = await service.ReactivarCta(99);

        ok.Should().BeFalse();
        error.Should().Contain("no encontrado");
    }

    [Fact]
    public async Task ReactivarCta_CtaInactivo_DeberiaActivar()
    {
        var cta = new CentroTratamiento { Id = 2, Codigo = "CTA-BCN", Activo = false };
        _ctaRepo.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(cta);
        _ctaRepo.Setup(r => r.UpdateAsync(It.IsAny<CentroTratamiento>())).Returns(Task.CompletedTask);

        var service = BuildService();
        var (ok, error) = await service.ReactivarCta(2);

        ok.Should().BeTrue();
        error.Should().BeNull();
        cta.Activo.Should().BeTrue();
    }
}
