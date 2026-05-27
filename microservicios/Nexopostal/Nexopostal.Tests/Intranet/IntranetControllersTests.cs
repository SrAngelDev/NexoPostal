using System.Security.Claims;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.Controllers;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Tests.Intranet;

internal static class IntranetControllerTestExtensions
{
    public static void WireUser(this ControllerBase ctrl, string? userId = "user-1", string? role = "Admin")
    {
        var claims = new List<Claim>();
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        if (role != null) claims.Add(new Claim(ClaimTypes.Role, role));
        var identity = new ClaimsIdentity(claims, role == null ? null : "TestAuth");
        ctrl.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }
}

// ============================================================
//  CtasController
// ============================================================
public class CtasControllerTests
{
    private readonly Mock<IClasificacionService> _clasif = new();
    private readonly Mock<IAdminCtaService> _adminCta = new();

    private CtasController CreateCtrl()
    {
        var c = new CtasController(_clasif.Object, _adminCta.Object);
        c.WireUser();
        return c;
    }

    [Fact]
    public async Task ObtenerTodos_DevuelveLista()
    {
        _clasif.Setup(s => s.ObtenerTodosCtas())
            .ReturnsAsync(new List<CtaResumenDto> { new() { Id = 1, Codigo = "CTA-MAD" } });

        var res = await CreateCtrl().ObtenerTodos();

        var ok = res.Result as OkObjectResult;
        ok!.Value.Should().BeOfType<List<CtaResumenDto>>().Which.Should().HaveCount(1);
    }

    [Fact]
    public async Task ObtenerDetalle_NotFound_CuandoNoExiste()
    {
        _clasif.Setup(s => s.ObtenerCtaDetalle(99)).ReturnsAsync((CtaDetalleDto?)null);
        var res = await CreateCtrl().ObtenerDetalle(99);
        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ObtenerDetalle_OK_CuandoExiste()
    {
        _clasif.Setup(s => s.ObtenerCtaDetalle(1)).ReturnsAsync(new CtaDetalleDto { Id = 1 });
        var res = await CreateCtrl().ObtenerDetalle(1);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResolverCta_NotFound()
    {
        _clasif.Setup(s => s.ResolverCtaDestino("99999")).ReturnsAsync((ResolverCtaResponseDto?)null);
        var res = await CreateCtrl().ResolverCta("99999");
        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ResolverCta_OK()
    {
        _clasif.Setup(s => s.ResolverCtaDestino("28001"))
            .ReturnsAsync(new ResolverCtaResponseDto { CodigoPostal = "28001", CtaCodigo = "CTA-MAD" });
        var res = await CreateCtrl().ResolverCta("28001");
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ObtenerDashboard_NotFound_CuandoNoExiste()
    {
        _clasif.Setup(s => s.ObtenerDashboardCta(5)).ReturnsAsync((DashboardCtaDto?)null);
        var res = await CreateCtrl().ObtenerDashboard(5);
        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ObtenerDashboard_OK()
    {
        _clasif.Setup(s => s.ObtenerDashboardCta(1)).ReturnsAsync(new DashboardCtaDto { CtaId = 1 });
        var res = await CreateCtrl().ObtenerDashboard(1);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ObtenerDashboardAdmin_OK()
    {
        _clasif.Setup(s => s.ObtenerDashboardAdmin()).ReturnsAsync(new DashboardAdminDto());
        var res = await CreateCtrl().ObtenerDashboardAdmin();
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Crear_OK()
    {
        var dto = new CrearCtaDto { Codigo = "CTA-NEW", Nombre = "Nuevo" };
        _adminCta.Setup(s => s.CrearCta(dto)).ReturnsAsync((new CtaDetalleDto { Id = 5 }, null));
        var res = await CreateCtrl().Crear(dto);
        res.Result.Should().BeOfType<CreatedAtActionResult>();
    }

    [Fact]
    public async Task Crear_BadRequest_CuandoError()
    {
        var dto = new CrearCtaDto();
        _adminCta.Setup(s => s.CrearCta(dto)).ReturnsAsync(((CtaDetalleDto?)null, "Código obligatorio"));
        var res = await CreateCtrl().Crear(dto);
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Editar_NotFound()
    {
        var dto = new EditarCtaDto();
        _adminCta.Setup(s => s.EditarCta(1, dto)).ReturnsAsync(((CtaDetalleDto?)null, "CTA no encontrado."));
        var res = await CreateCtrl().Editar(1, dto);
        res.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Editar_BadRequest()
    {
        var dto = new EditarCtaDto();
        _adminCta.Setup(s => s.EditarCta(1, dto)).ReturnsAsync(((CtaDetalleDto?)null, "Otro error"));
        var res = await CreateCtrl().Editar(1, dto);
        res.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Editar_OK()
    {
        var dto = new EditarCtaDto();
        _adminCta.Setup(s => s.EditarCta(1, dto)).ReturnsAsync((new CtaDetalleDto { Id = 1 }, null));
        var res = await CreateCtrl().Editar(1, dto);
        res.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Desactivar_NoContent()
    {
        _adminCta.Setup(s => s.DesactivarCta(1)).ReturnsAsync((true, null));
        var res = await CreateCtrl().Desactivar(1);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Desactivar_BadRequest()
    {
        _adminCta.Setup(s => s.DesactivarCta(1)).ReturnsAsync((false, "Tiene tareas"));
        var res = await CreateCtrl().Desactivar(1);
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Reactivar_NoContent()
    {
        _adminCta.Setup(s => s.ReactivarCta(1)).ReturnsAsync((true, null));
        var res = await CreateCtrl().Reactivar(1);
        res.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Reactivar_BadRequest()
    {
        _adminCta.Setup(s => s.ReactivarCta(1)).ReturnsAsync((false, "Ya activo"));
        var res = await CreateCtrl().Reactivar(1);
        res.Should().BeOfType<BadRequestObjectResult>();
    }
}

// ============================================================
//  OficinasPostalesController
// ============================================================
public class OficinasPostalesControllerTests
{
    private readonly Mock<IOficinaPostalService> _svc = new();

    private OficinasPostalesController CreateCtrl()
    {
        var c = new OficinasPostalesController(_svc.Object);
        c.WireUser(role: "OperarioCTA");
        return c;
    }

    [Fact]
    public void ObtenerTodas_OK()
    {
        _svc.Setup(s => s.ObtenerTodas())
            .Returns(new List<OficinaJsonDto> { new() { Id = 1, Nombre = "X" } });
        var res = CreateCtrl().ObtenerTodas();
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Buscar_PorCodigoPostal()
    {
        _svc.Setup(s => s.BuscarPorCodigoPostal("28001"))
            .Returns(new List<OficinaJsonDto> { new() { Id = 1 } });
        var res = CreateCtrl().Buscar("28001", null);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Buscar_PorTexto()
    {
        _svc.Setup(s => s.BuscarPorTexto("madrid"))
            .Returns(new List<OficinaJsonDto> { new() { Id = 1 } });
        var res = CreateCtrl().Buscar(null, "madrid");
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Buscar_BadRequest_SinParametros()
    {
        var res = CreateCtrl().Buscar(null, null);
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void ObtenerPorId_NotFound()
    {
        _svc.Setup(s => s.ObtenerPorId(99)).Returns((OficinaJsonDto?)null);
        var res = CreateCtrl().ObtenerPorId(99);
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public void ObtenerPorId_OK()
    {
        _svc.Setup(s => s.ObtenerPorId(1)).Returns(new OficinaJsonDto { Id = 1 });
        var res = CreateCtrl().ObtenerPorId(1);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ResolverOficinaPorCp_NotFound()
    {
        _svc.Setup(s => s.ResolverOficinaPorCp("9999"))
            .ReturnsAsync((ResolverOficinaCtaResponseDto?)null);
        var res = await CreateCtrl().ResolverOficinaPorCp("9999");
        res.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ResolverOficinaPorCp_OK()
    {
        _svc.Setup(s => s.ResolverOficinaPorCp("28001"))
            .ReturnsAsync(new ResolverOficinaCtaResponseDto());
        var res = await CreateCtrl().ResolverOficinaPorCp("28001");
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ObtenerOperarios_OK()
    {
        _svc.Setup(s => s.ObtenerOperariosOficina(1))
            .ReturnsAsync(new List<OperarioOficinaResumenDto>());
        var res = await CreateCtrl().ObtenerOperarios(1);
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ObtenerPorCta_OK()
    {
        _svc.Setup(s => s.ObtenerOficinasPorCta(1))
            .ReturnsAsync(new List<OficinaJsonDto>());
        var res = await CreateCtrl().ObtenerPorCta(1);
        res.Should().BeOfType<OkObjectResult>();
    }
}

// ============================================================
//  ScanController
// ============================================================
public class ScanControllerTests
{
    private readonly Mock<IScanProcessorService> _processor = new();

    private ScanController CreateCtrl(string role = "OperarioCTA")
    {
        var c = new ScanController(_processor.Object, NullLogger<ScanController>.Instance);
        c.WireUser(role: role);
        return c;
    }

    [Fact]
    public async Task ProcesarEscaneo_BadRequest_SinCodigo()
    {
        var res = await CreateCtrl().ProcesarEscaneo(new ScanRequestDto { ModoOperacion = ModosEscaneo.RecepcionCta });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ProcesarEscaneo_BadRequest_ModoInvalido()
    {
        var res = await CreateCtrl().ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-123",
            ModoOperacion = "modo_invalido"
        });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ProcesarEscaneo_Forbid_CuandoRolNoCorresponde()
    {
        // OperarioOficina intenta modo CTA
        var res = await CreateCtrl(role: "OperarioOficina").ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-123",
            ModoOperacion = ModosEscaneo.RecepcionCta
        });
        res.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ProcesarEscaneo_OK()
    {
        _processor.Setup(p => p.ProcesarEscaneo(It.IsAny<ScanRequestDto>()))
            .ReturnsAsync(new ScanResultDto { Exito = true, EstadoNuevo = "EnCta" });
        var res = await CreateCtrl().ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-123",
            ModoOperacion = ModosEscaneo.RecepcionCta
        });
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ProcesarEscaneo_UnprocessableEntity_CuandoFalla()
    {
        _processor.Setup(p => p.ProcesarEscaneo(It.IsAny<ScanRequestDto>()))
            .ReturnsAsync(new ScanResultDto { Exito = false, Mensaje = "no encontrado" });
        var res = await CreateCtrl().ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-123",
            ModoOperacion = ModosEscaneo.RecepcionCta
        });
        res.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task ProcesarLote_BadRequest_ListaVacia()
    {
        var res = await CreateCtrl().ProcesarLote(new ScanBatchRequestDto
        {
            CodigosEscaneados = new(),
            ModoOperacion = ModosEscaneo.RecepcionCta
        });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ProcesarLote_BadRequest_ModoInvalido()
    {
        var res = await CreateCtrl().ProcesarLote(new ScanBatchRequestDto
        {
            CodigosEscaneados = new() { "A" },
            ModoOperacion = "no_existe"
        });
        res.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ProcesarLote_Forbid_RolIncorrecto()
    {
        var res = await CreateCtrl(role: "OperarioOficina").ProcesarLote(new ScanBatchRequestDto
        {
            CodigosEscaneados = new() { "A" },
            ModoOperacion = ModosEscaneo.Clasificacion
        });
        res.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ProcesarLote_OK()
    {
        _processor.Setup(p => p.ProcesarLote(It.IsAny<ScanBatchRequestDto>()))
            .ReturnsAsync(new ScanBatchResultDto { TotalEscaneados = 1, Exitosos = 1 });
        var res = await CreateCtrl().ProcesarLote(new ScanBatchRequestDto
        {
            CodigosEscaneados = new() { "A" },
            ModoOperacion = ModosEscaneo.RecepcionCta
        });
        res.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void ObtenerModos_AdminVeTodos()
    {
        var res = CreateCtrl(role: "Admin").ObtenerModos() as OkObjectResult;
        res.Should().NotBeNull();
    }

    [Fact]
    public void ObtenerModos_OperarioCtaSoloModosCta()
    {
        var res = CreateCtrl(role: "OperarioCTA").ObtenerModos() as OkObjectResult;
        res.Should().NotBeNull();
    }

    [Fact]
    public void ObtenerModos_OperarioOficinaSoloModosOficina()
    {
        var res = CreateCtrl(role: "OperarioOficina").ObtenerModos() as OkObjectResult;
        res.Should().NotBeNull();
    }
}
