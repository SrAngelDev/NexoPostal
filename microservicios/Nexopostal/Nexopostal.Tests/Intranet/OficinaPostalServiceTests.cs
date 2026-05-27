using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

[Collection("OficinasJsonIntranet")]
public class OficinaPostalServiceTests : IDisposable
{
    private readonly string _root;
    private readonly ServiceProvider _sp;
    private readonly OficinasJsonService _oficinasJson;
    private readonly Mock<IClasificacionService> _clasificacion = new();
    private readonly Mock<IOperarioOficinaRepository> _operarioRepo = new();
    private readonly Mock<IRutaCtaRepository> _rutaRepo = new();

    public OficinaPostalServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nxp-of-svc-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Data"));
        File.WriteAllText(Path.Combine(_root, "Data", "oficinas.json"), Fixture);

        var services = new ServiceCollection();
        services.AddDbContext<IntranetDbContext>(o => o.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        _sp = services.BuildServiceProvider();

        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(_root);
        _oficinasJson = new OficinasJsonService(
            NullLogger<OficinasJsonService>.Instance,
            _sp.GetRequiredService<IServiceScopeFactory>(),
            env.Object);
        _oficinasJson.Invalidar();
    }

    public void Dispose()
    {
        _sp.Dispose();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    private const string Fixture = """
    {
      "@graph": [
        { "id": "1", "title": "Sol", "address": { "locality": "Madrid", "postal-code": "28013", "street-address": "X" }, "location": { "latitude": 40.4, "longitude": -3.7 } },
        { "id": "2", "title": "Cham", "address": { "locality": "Madrid", "postal-code": "28010", "street-address": "Y" }, "location": { "latitude": 40.4, "longitude": -3.7 } }
      ]
    }
    """;

    private OficinaPostalService Crear() => new(
        _oficinasJson,
        _clasificacion.Object,
        _operarioRepo.Object,
        _rutaRepo.Object,
        NullLogger<OficinaPostalService>.Instance);

    [Fact]
    public void ObtenerTodas_DelegaEnJsonService()
    {
        Crear().ObtenerTodas().Should().HaveCount(2);
    }

    [Fact]
    public void BuscarPorCp_Delega() => Crear().BuscarPorCodigoPostal("28013").Should().ContainSingle();

    [Fact]
    public void BuscarPorTexto_Delega() => Crear().BuscarPorTexto("Madrid").Should().HaveCount(2);

    [Fact]
    public void ObtenerPorId_Delega() => Crear().ObtenerPorId(1)!.Nombre.Should().Be("Sol");

    [Fact]
    public async Task ResolverOficinaPorCp_OficinaNoExiste_DevuelveNull()
    {
        var r = await Crear().ResolverOficinaPorCp("99999");
        r.Should().BeNull();
    }

    [Fact]
    public async Task ResolverOficinaPorCp_CtaNoExiste_DevuelveNull()
    {
        _clasificacion.Setup(c => c.ResolverCtaDestino("28013")).ReturnsAsync((ResolverCtaResponseDto?)null);
        var r = await Crear().ResolverOficinaPorCp("28013");
        r.Should().BeNull();
    }

    [Fact]
    public async Task ResolverOficinaPorCp_Exito_CombinaInfo()
    {
        _clasificacion.Setup(c => c.ResolverCtaDestino("28013")).ReturnsAsync(new ResolverCtaResponseDto
        {
            CtaId = 7, CtaCodigo = "CTA-MAD", CtaNombre = "MAD", Area = "Centro"
        });
        var r = await Crear().ResolverOficinaPorCp("28013");
        r!.CtaId.Should().Be(7);
        r.OficinaNombre.Should().Be("Sol");
    }

    [Fact]
    public async Task ObtenerOperariosOficina_Mapea()
    {
        _operarioRepo.Setup(r => r.GetByOficinaAsync(1, true)).ReturnsAsync(new List<OperarioOficina>
        {
            new() { Id = 11, NombreCompleto = "Ada", CodigoEmpleado = "E1", Rol = RolOperario.OperarioOficina, Activo = true, OficinaJsonId = 1, OficinaNombre = "Sol" }
        });
        var r = await Crear().ObtenerOperariosOficina(1);
        r.Should().ContainSingle();
        r[0].NombreCompleto.Should().Be("Ada");
    }

    [Fact]
    public async Task ObtenerOficinasPorCta_SinRutas_DevuelveVacio()
    {
        _rutaRepo.Setup(r => r.GetByCtaIdAsync(7)).ReturnsAsync(new List<RutaCta>());
        (await Crear().ObtenerOficinasPorCta(7)).Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerOficinasPorCta_FiltraPorPrefijo()
    {
        _rutaRepo.Setup(r => r.GetByCtaIdAsync(7)).ReturnsAsync(new List<RutaCta>
        {
            new() { PrefijoCp = "280" }
        });
        var r = await Crear().ObtenerOficinasPorCta(7);
        r.Should().HaveCount(2);
        r.Select(o => o.CodigoPostal).Should().OnlyContain(c => c.StartsWith("280"));
    }

    [Fact]
    public async Task ObtenerMiOficina_NoAsignado_DevuelveNull()
    {
        _operarioRepo.Setup(r => r.GetByIdentityUserIdAsync("u1")).ReturnsAsync((OperarioOficina?)null);
        (await Crear().ObtenerMiOficina("u1")).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerMiOficina_Asignado_Mapea()
    {
        _operarioRepo.Setup(r => r.GetByIdentityUserIdAsync("u1")).ReturnsAsync(new OperarioOficina
        {
            OficinaJsonId = 1, OficinaNombre = "Sol", Rol = RolOperario.OperarioOficina, Activo = true
        });
        var r = await Crear().ObtenerMiOficina("u1");
        r!.OficinaJsonId.Should().Be(1);
        r.CodigoPostal.Should().Be("28013");
        r.Ciudad.Should().Be("Madrid");
    }

    [Fact]
    public async Task ObtenerOficinaAdmin_UsaAny_DevuelveDto()
    {
        _operarioRepo.Setup(r => r.GetByIdentityUserIdAnyAsync("u1")).ReturnsAsync(new OperarioOficina
        {
            OficinaJsonId = 99, OficinaNombre = "X", Rol = RolOperario.OperarioOficina
        });
        var r = await Crear().ObtenerOficinaAdmin("u1");
        r!.OficinaJsonId.Should().Be(99);
        r.CodigoPostal.Should().BeEmpty(); // oficina 99 no existe en json
    }

    [Fact]
    public async Task ActualizarOficinaAdmin_IdentityVacio_Error()
    {
        var (ok, err, _) = await Crear().ActualizarOficinaAdmin("  ", new AdminActualizarOficinaDto { NuevoOficinaJsonId = 1 });
        ok.Should().BeFalse();
        err.Should().Contain("Identity");
    }

    [Fact]
    public async Task ActualizarOficinaAdmin_OficinaIdInvalido_Error()
    {
        var (ok, err, _) = await Crear().ActualizarOficinaAdmin("u", new AdminActualizarOficinaDto { NuevoOficinaJsonId = 0 });
        ok.Should().BeFalse();
        err.Should().Contain("oficina");
    }

    [Fact]
    public async Task ActualizarOficinaAdmin_OficinaInexistente_Error()
    {
        var (ok, err, _) = await Crear().ActualizarOficinaAdmin("u", new AdminActualizarOficinaDto { NuevoOficinaJsonId = 999 });
        ok.Should().BeFalse();
        err.Should().Contain("no encontrada");
    }

    [Fact]
    public async Task ActualizarOficinaAdmin_PrimeraAsignacion_FaltanCampos_Error()
    {
        _operarioRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u")).ReturnsAsync(new List<OperarioOficina>());
        var (ok, err, _) = await Crear().ActualizarOficinaAdmin("u", new AdminActualizarOficinaDto { NuevoOficinaJsonId = 1 });
        ok.Should().BeFalse();
        err.Should().Contain("NombreCompleto");
    }

    [Fact]
    public async Task ActualizarOficinaAdmin_PrimeraAsignacion_RolInvalido_Error()
    {
        _operarioRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u")).ReturnsAsync(new List<OperarioOficina>());
        var dto = new AdminActualizarOficinaDto { NuevoOficinaJsonId = 1, NombreCompleto = "n", CodigoEmpleado = "c", Rol = "RolInexistente" };
        var (ok, err, _) = await Crear().ActualizarOficinaAdmin("u", dto);
        ok.Should().BeFalse();
        err.Should().Contain("Rol");
    }

    [Fact]
    public async Task ActualizarOficinaAdmin_PrimeraAsignacion_Crea()
    {
        _operarioRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u")).ReturnsAsync(new List<OperarioOficina>());
        OperarioOficina? creado = null;
        _operarioRepo.Setup(r => r.CreateAsync(It.IsAny<OperarioOficina>()))
            .Callback<OperarioOficina>(o => creado = o)
            .ReturnsAsync((OperarioOficina o) => o);

        var dto = new AdminActualizarOficinaDto { NuevoOficinaJsonId = 1, NombreCompleto = "n", CodigoEmpleado = "c" };
        var (ok, err, info) = await Crear().ActualizarOficinaAdmin("u", dto);
        ok.Should().BeTrue();
        err.Should().BeNull();
        info!.OficinaJsonId.Should().Be(1);
        creado!.IdentityUserId.Should().Be("u");
    }

    [Fact]
    public async Task ActualizarOficinaAdmin_ReactivaExistente()
    {
        var existentes = new List<OperarioOficina>
        {
            new() { Id = 10, OficinaJsonId = 1, OficinaNombre = "Sol", Activo = false, Rol = RolOperario.OperarioOficina },
            new() { Id = 11, OficinaJsonId = 2, OficinaNombre = "Cham", Activo = true, Rol = RolOperario.OperarioOficina }
        };
        _operarioRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u")).ReturnsAsync(existentes);

        var (ok, err, info) = await Crear().ActualizarOficinaAdmin("u", new AdminActualizarOficinaDto { NuevoOficinaJsonId = 1 });
        ok.Should().BeTrue();
        info!.OficinaJsonId.Should().Be(1);
        existentes[0].Activo.Should().BeTrue();
        existentes[1].Activo.Should().BeFalse();
        _operarioRepo.Verify(r => r.UpdateRangeAsync(It.IsAny<IEnumerable<OperarioOficina>>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarOficinaAdmin_RepurposaPrincipal()
    {
        var existentes = new List<OperarioOficina>
        {
            new() { Id = 20, OficinaJsonId = 2, OficinaNombre = "Cham", Activo = true, Rol = RolOperario.OperarioOficina }
        };
        _operarioRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u")).ReturnsAsync(existentes);

        var (ok, _, info) = await Crear().ActualizarOficinaAdmin("u", new AdminActualizarOficinaDto { NuevoOficinaJsonId = 1 });
        ok.Should().BeTrue();
        info!.OficinaJsonId.Should().Be(1);
        existentes[0].OficinaJsonId.Should().Be(1);
        existentes[0].OficinaNombre.Should().Be("Sol");
        existentes[0].Activo.Should().BeTrue();
    }

    [Fact]
    public async Task ActualizarOficinaAdmin_ExcepcionInterna_RespondeError()
    {
        _operarioRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u")).ThrowsAsync(new InvalidOperationException("boom"));
        var (ok, err, _) = await Crear().ActualizarOficinaAdmin("u", new AdminActualizarOficinaDto { NuevoOficinaJsonId = 1 });
        ok.Should().BeFalse();
        err.Should().Contain("boom");
    }
}
