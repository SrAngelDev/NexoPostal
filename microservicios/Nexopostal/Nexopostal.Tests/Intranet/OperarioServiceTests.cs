using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Hubs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class OperarioServiceTests
{
    private readonly Mock<IOperarioCtaRepository> _opRepo = new();
    private readonly Mock<ICentroTratamientoRepository> _ctaRepo = new();
    private readonly Mock<IAsignacionPaqueteRepository> _asigRepo = new();
    private readonly Mock<IHubContext<IntranetHub>> _hub = new();
    private readonly Mock<IClientProxy> _clientProxy = new();

    public OperarioServiceTests()
    {
        var clients = new Mock<IHubClients>();
        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(_clientProxy.Object);
        _hub.Setup(h => h.Clients).Returns(clients.Object);
        _clientProxy.Setup(c => c.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
    }

    private OperarioService Crear() => new(
        _opRepo.Object, _ctaRepo.Object, _asigRepo.Object, _hub.Object,
        NullLogger<OperarioService>.Instance);

    private static OperarioCta Op(int id, int ctaId, string identity = "u1", bool activo = true, RolOperario rol = RolOperario.OperarioCTA) =>
        new()
        {
            Id = id,
            IdentityUserId = identity,
            CentroTratamientoId = ctaId,
            CentroTratamiento = new CentroTratamiento { Id = ctaId, Codigo = $"C{ctaId}", Nombre = $"CTA-{ctaId}" },
            NombreCompleto = "Ada Lovelace",
            CodigoEmpleado = "E1",
            Rol = rol,
            Activo = activo,
            FechaAsignacion = DateTime.UtcNow
        };

    [Fact]
    public async Task ObtenerPorIdentityUserId_DelegaEnRepositorio()
    {
        var op = Op(1, 7);
        _opRepo.Setup(r => r.GetByIdentityUserIdAsync("u1")).ReturnsAsync(op);
        (await Crear().ObtenerPorIdentityUserId("u1")).Should().Be(op);
    }

    [Fact]
    public async Task ObtenerMiCtaInfo_SinOperario_Null()
    {
        _opRepo.Setup(r => r.GetByIdentityUserIdAsync("u1")).ReturnsAsync((OperarioCta?)null);
        (await Crear().ObtenerMiCtaInfo("u1")).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerMiCtaInfo_Mapea()
    {
        _opRepo.Setup(r => r.GetByIdentityUserIdAsync("u1")).ReturnsAsync(Op(1, 7));
        var r = await Crear().ObtenerMiCtaInfo("u1");
        r!.CtaCodigo.Should().Be("C7");
    }

    [Fact]
    public async Task ObtenerMisCtasInfo_Vacio_Null()
    {
        _opRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u1")).ReturnsAsync(new List<OperarioCta>());
        (await Crear().ObtenerMisCtasInfo("u1")).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerMisCtasInfo_MapeaListado()
    {
        _opRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u1"))
               .ReturnsAsync(new List<OperarioCta> { Op(1, 7), Op(2, 8) });
        var r = await Crear().ObtenerMisCtasInfo("u1");
        r!.Ctas.Should().HaveCount(2);
    }

    [Fact]
    public async Task ObtenerOperariosCta_DevuelveResumenes()
    {
        _opRepo.Setup(r => r.GetByCtaIdAsync(7, null)).ReturnsAsync(new List<OperarioCta> { Op(1, 7) });
        (await Crear().ObtenerOperariosCta(7)).Should().ContainSingle();
    }

    [Fact]
    public async Task ObtenerDetalle_NoExiste_Null()
    {
        _opRepo.Setup(r => r.GetWithCtaAsync(1)).ReturnsAsync((OperarioCta?)null);
        (await Crear().ObtenerDetalle(1)).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerDetalle_Existe_IncluyeContadores()
    {
        _opRepo.Setup(r => r.GetWithCtaAsync(1)).ReturnsAsync(Op(1, 7));
        _asigRepo.Setup(a => a.CountByOperarioAndEstadoAsync(1, EstadoTarea.Pendiente)).ReturnsAsync(2);
        _asigRepo.Setup(a => a.CountByOperarioAndEstadoAsync(1, EstadoTarea.EnProgreso)).ReturnsAsync(1);
        _asigRepo.Setup(a => a.CountCompletadasHoyByOperarioAsync(1)).ReturnsAsync(3);

        var r = await Crear().ObtenerDetalle(1);
        r!.TareasPendientes.Should().Be(2);
        r.TareasEnProgreso.Should().Be(1);
        r.TareasCompletadasHoy.Should().Be(3);
    }

    [Fact]
    public async Task ObtenerDetalleAdminPorIdentityUserId_SinAsignaciones_Null()
    {
        _opRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u1")).ReturnsAsync(new List<OperarioCta>());
        (await Crear().ObtenerDetalleAdminPorIdentityUserId("u1")).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerDetalleAdminPorIdentityUserId_Mapea()
    {
        _opRepo.Setup(r => r.GetAllByIdentityUserIdAsync("u1"))
               .ReturnsAsync(new List<OperarioCta> { Op(1, 7), Op(2, 8, activo: false) });
        var r = await Crear().ObtenerDetalleAdminPorIdentityUserId("u1");
        r!.AsignacionesCta.Should().HaveCount(2);
        r.AsignacionesCta[0].Activo.Should().BeTrue();
    }

    [Fact]
    public async Task ActualizarCtaAdmin_IdentitySinValor_Devuelve_Error()
    {
        var (ok, err, conflict) = await Crear().ActualizarCtaAdmin("", new AdminActualizarCtaDto { NuevoCtaId = 1 });
        ok.Should().BeFalse();
        conflict.Should().BeFalse();
        err.Should().NotBeNull();
    }

    [Fact]
    public async Task ActualizarCtaAdmin_CtaDestinoNoExiste()
    {
        _opRepo.Setup(r => r.GetAllByIdentityUserIdIncludingInactiveAsync("u1")).ReturnsAsync(new List<OperarioCta>());
        _ctaRepo.Setup(c => c.GetByIdAsync(99)).ReturnsAsync((CentroTratamiento?)null);
        var (ok, err, _) = await Crear().ActualizarCtaAdmin("u1", new AdminActualizarCtaDto { NuevoCtaId = 99 });
        ok.Should().BeFalse();
        err.Should().Contain("99");
    }

    [Fact]
    public async Task ActualizarCtaAdmin_PrimeraAsignacion_FaltanDatos()
    {
        _opRepo.Setup(r => r.GetAllByIdentityUserIdIncludingInactiveAsync("u1")).ReturnsAsync(new List<OperarioCta>());
        _ctaRepo.Setup(c => c.GetByIdAsync(7)).ReturnsAsync(new CentroTratamiento { Id = 7 });
        var (ok, err, _) = await Crear().ActualizarCtaAdmin("u1", new AdminActualizarCtaDto { NuevoCtaId = 7 });
        ok.Should().BeFalse();
        err.Should().Contain("asignación previa");
    }

    [Fact]
    public async Task ActualizarCtaAdmin_PrimeraAsignacion_RolInvalido()
    {
        _opRepo.Setup(r => r.GetAllByIdentityUserIdIncludingInactiveAsync("u1")).ReturnsAsync(new List<OperarioCta>());
        _ctaRepo.Setup(c => c.GetByIdAsync(7)).ReturnsAsync(new CentroTratamiento { Id = 7 });
        var dto = new AdminActualizarCtaDto
        {
            NuevoCtaId = 7,
            NombreCompleto = "Ada",
            CodigoEmpleado = "E1",
            Rol = "Invento"
        };
        var (ok, err, _) = await Crear().ActualizarCtaAdmin("u1", dto);
        ok.Should().BeFalse();
        err.Should().Contain("Invento");
    }

    [Fact]
    public async Task ActualizarCtaAdmin_PrimeraAsignacion_Exito()
    {
        _opRepo.Setup(r => r.GetAllByIdentityUserIdIncludingInactiveAsync("u1")).ReturnsAsync(new List<OperarioCta>());
        _ctaRepo.Setup(c => c.GetByIdAsync(7)).ReturnsAsync(new CentroTratamiento { Id = 7, Codigo = "C7" });

        var dto = new AdminActualizarCtaDto
        {
            NuevoCtaId = 7, NombreCompleto = "Ada", CodigoEmpleado = "E1", Rol = "OperarioCTA"
        };
        var (ok, _, _) = await Crear().ActualizarCtaAdmin("u1", dto);
        ok.Should().BeTrue();
        _opRepo.Verify(r => r.CreateAsync(It.Is<OperarioCta>(o => o.CentroTratamientoId == 7)), Times.Once);
    }

    [Fact]
    public async Task ActualizarCtaAdmin_Idempotente()
    {
        var existente = Op(1, 7);
        _opRepo.Setup(r => r.GetAllByIdentityUserIdIncludingInactiveAsync("u1"))
               .ReturnsAsync(new List<OperarioCta> { existente });
        _ctaRepo.Setup(c => c.GetByIdAsync(7)).ReturnsAsync(new CentroTratamiento { Id = 7 });

        var (ok, _, _) = await Crear().ActualizarCtaAdmin("u1", new AdminActualizarCtaDto { NuevoCtaId = 7 });
        ok.Should().BeTrue();
        _opRepo.Verify(r => r.CreateAsync(It.IsAny<OperarioCta>()), Times.Never);
        _opRepo.Verify(r => r.UpdateAsync(It.IsAny<OperarioCta>()), Times.Never);
    }

    [Fact]
    public async Task ActualizarCtaAdmin_BloqueaSiTieneTareasPendientes()
    {
        var existente = Op(1, 7);
        _opRepo.Setup(r => r.GetAllByIdentityUserIdIncludingInactiveAsync("u1"))
               .ReturnsAsync(new List<OperarioCta> { existente });
        _ctaRepo.Setup(c => c.GetByIdAsync(8)).ReturnsAsync(new CentroTratamiento { Id = 8 });
        _asigRepo.Setup(a => a.CountByOperarioAndEstadoAsync(1, EstadoTarea.Pendiente)).ReturnsAsync(5);

        var (ok, err, conflict) = await Crear().ActualizarCtaAdmin("u1", new AdminActualizarCtaDto { NuevoCtaId = 8 });
        ok.Should().BeFalse();
        conflict.Should().BeTrue();
        err.Should().Contain("tareas");
    }

    [Fact]
    public async Task ActualizarCtaAdmin_MoverYReactivarDestino()
    {
        var origen = Op(1, 7);
        var destinoInactivo = Op(2, 8, activo: false);
        _opRepo.Setup(r => r.GetAllByIdentityUserIdIncludingInactiveAsync("u1"))
               .ReturnsAsync(new List<OperarioCta> { origen, destinoInactivo });
        _ctaRepo.Setup(c => c.GetByIdAsync(8)).ReturnsAsync(new CentroTratamiento { Id = 8, Codigo = "C8", Nombre = "CTA-8" });

        var (ok, _, _) = await Crear().ActualizarCtaAdmin("u1", new AdminActualizarCtaDto { NuevoCtaId = 8 });

        ok.Should().BeTrue();
        origen.Activo.Should().BeFalse();
        destinoInactivo.Activo.Should().BeTrue();
        _clientProxy.Verify(c => c.SendCoreAsync("CtaCambiada", It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarCtaAdmin_MoverConCreacionEnDestino()
    {
        var origen = Op(1, 7);
        _opRepo.Setup(r => r.GetAllByIdentityUserIdIncludingInactiveAsync("u1"))
               .ReturnsAsync(new List<OperarioCta> { origen });
        _ctaRepo.Setup(c => c.GetByIdAsync(8)).ReturnsAsync(new CentroTratamiento { Id = 8, Codigo = "C8", Nombre = "CTA-8" });
        _opRepo.Setup(r => r.CreateAsync(It.IsAny<OperarioCta>()))
               .ReturnsAsync((OperarioCta x) => { x.Id = 99; return x; });

        var (ok, _, _) = await Crear().ActualizarCtaAdmin("u1", new AdminActualizarCtaDto { NuevoCtaId = 8 });
        ok.Should().BeTrue();
        _opRepo.Verify(r => r.CreateAsync(It.Is<OperarioCta>(o => o.CentroTratamientoId == 8 && o.NombreCompleto == "Ada Lovelace")), Times.Once);
    }

    [Fact]
    public async Task CrearOperario_RolInvalido_Lanza()
    {
        await FluentActions.Invoking(() => Crear().CrearOperario(new CrearOperarioDto
        {
            IdentityUserId = "u1", NombreCompleto = "Ada", CodigoEmpleado = "E1", Rol = "Wat", CentroTratamientoId = 1
        })).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CrearOperario_CtaInexistente_Lanza()
    {
        _ctaRepo.Setup(c => c.GetByIdAsync(1)).ReturnsAsync((CentroTratamiento?)null);
        await FluentActions.Invoking(() => Crear().CrearOperario(new CrearOperarioDto
        {
            IdentityUserId = "u1", NombreCompleto = "Ada", CodigoEmpleado = "E1", Rol = "OperarioCTA", CentroTratamientoId = 1
        })).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CrearOperario_Duplicado_Lanza()
    {
        _ctaRepo.Setup(c => c.GetByIdAsync(1)).ReturnsAsync(new CentroTratamiento { Id = 1 });
        _opRepo.Setup(r => r.ExistsByIdentityUserIdAndCtaAsync("u1", 1)).ReturnsAsync(true);
        await FluentActions.Invoking(() => Crear().CrearOperario(new CrearOperarioDto
        {
            IdentityUserId = "u1", NombreCompleto = "Ada", CodigoEmpleado = "E1", Rol = "OperarioCTA", CentroTratamientoId = 1
        })).Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CrearOperario_Exito_LlamaCreate()
    {
        _ctaRepo.Setup(c => c.GetByIdAsync(1)).ReturnsAsync(new CentroTratamiento { Id = 1, Codigo = "C1" });
        _opRepo.Setup(r => r.ExistsByIdentityUserIdAndCtaAsync("u1", 1)).ReturnsAsync(false);
        var r = await Crear().CrearOperario(new CrearOperarioDto
        {
            IdentityUserId = "u1", NombreCompleto = "Ada", CodigoEmpleado = "E1", Rol = "OperarioCTA", CentroTratamientoId = 1
        });
        r.NombreCompleto.Should().Be("Ada");
        _opRepo.Verify(rp => rp.CreateAsync(It.IsAny<OperarioCta>()), Times.Once);
    }

    [Fact]
    public async Task DesactivarOperario_NoExiste_False()
    {
        _opRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((OperarioCta?)null);
        (await Crear().DesactivarOperario(1)).Should().BeFalse();
    }

    [Fact]
    public async Task DesactivarOperario_Existe_MarcaInactivo()
    {
        var op = Op(1, 7);
        _opRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(op);
        (await Crear().DesactivarOperario(1)).Should().BeTrue();
        op.Activo.Should().BeFalse();
    }
}
