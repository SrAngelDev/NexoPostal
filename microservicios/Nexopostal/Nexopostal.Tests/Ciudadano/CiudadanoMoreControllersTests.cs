using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Ciudadano.Controllers;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using System.Security.Claims;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

public class AdminTarifasControllerTests
{
    private static CiudadanoDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<CiudadanoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CiudadanoDbContext(opts);
        db.TarifasBandas.AddRange(
            new TarifaBanda { Id = 1, Serie = TarifaSerie.LocalEstandar, OrdenBanda = 0, PesoHastaKg = 1m, PrecioBase = 4.50m },
            new TarifaBanda { Id = 2, Serie = TarifaSerie.LocalEstandar, OrdenBanda = 1, PesoHastaKg = 2m, PrecioBase = 5.25m },
            new TarifaBanda { Id = 3, Serie = TarifaSerie.PeninsulaPremium, OrdenBanda = 0, PesoHastaKg = 1m, PrecioBase = 8.95m });
        db.SaveChanges();
        return db;
    }

    private static AdminTarifasController CreateCtrl(CiudadanoDbContext db, Mock<ITarifasService>? svc = null)
    {
        svc ??= new Mock<ITarifasService>();
        var ctrl = new AdminTarifasController(db, svc.Object, NullLogger<AdminTarifasController>.Instance);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "Test"));
        ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
        return ctrl;
    }

    [Fact]
    public async Task Listar_DevuelveTodasOrdenadas()
    {
        using var db = CreateDb();
        var ctrl = CreateCtrl(db);
        var r = await ctrl.Listar();
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Obtener_Existente_Ok()
    {
        using var db = CreateDb();
        var ctrl = CreateCtrl(db);
        var r = await ctrl.Obtener(1);
        r.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Obtener_Inexistente_NotFound()
    {
        using var db = CreateDb();
        var ctrl = CreateCtrl(db);
        var r = await ctrl.Obtener(9999);
        r.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Editar_Existente_OkYInvalidaCache()
    {
        using var db = CreateDb();
        var svc = new Mock<ITarifasService>();
        var ctrl = CreateCtrl(db, svc);
        var r = await ctrl.Editar(1, new EditarTarifaBandaDto { PrecioBase = 9.99m });
        r.Result.Should().BeOfType<OkObjectResult>();
        svc.Verify(s => s.Invalidar(), Times.Once);
        (await db.TarifasBandas.FindAsync(1))!.PrecioBase.Should().Be(9.99m);
    }

    [Fact]
    public async Task Editar_Inexistente_NotFound()
    {
        using var db = CreateDb();
        var ctrl = CreateCtrl(db);
        var r = await ctrl.Editar(9999, new EditarTarifaBandaDto { PrecioBase = 1m });
        r.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Editar_ModelStateInvalido_BadRequest()
    {
        using var db = CreateDb();
        var ctrl = CreateCtrl(db);
        ctrl.ModelState.AddModelError("k", "err");
        var r = await ctrl.Editar(1, new EditarTarifaBandaDto());
        r.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EditarBulk_Ok()
    {
        using var db = CreateDb();
        var svc = new Mock<ITarifasService>();
        var ctrl = CreateCtrl(db, svc);
        var items = new List<EditarTarifaBandaBulkItemDto>
        {
            new() { Id = 1, PrecioBase = 5m },
            new() { Id = 2, PrecioBase = 6m }
        };
        var r = await ctrl.EditarBulk(items);
        r.Result.Should().BeOfType<OkObjectResult>();
        svc.Verify(s => s.Invalidar(), Times.Once);
    }

    [Fact]
    public async Task EditarBulk_Vacio_BadRequest()
    {
        using var db = CreateDb();
        var ctrl = CreateCtrl(db);
        (await ctrl.EditarBulk(new List<EditarTarifaBandaBulkItemDto>())).Result.Should().BeOfType<BadRequestObjectResult>();
        (await ctrl.EditarBulk(null!)).Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EditarBulk_IdInexistente_BadRequest()
    {
        using var db = CreateDb();
        var ctrl = CreateCtrl(db);
        var r = await ctrl.EditarBulk(new List<EditarTarifaBandaBulkItemDto> { new() { Id = 99, PrecioBase = 1m } });
        r.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task EditarBulk_PrecioInvalido_BadRequest()
    {
        using var db = CreateDb();
        var ctrl = CreateCtrl(db);
        var r = await ctrl.EditarBulk(new List<EditarTarifaBandaBulkItemDto> { new() { Id = 1, PrecioBase = 0m } });
        r.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Reset_RestauraDefaults()
    {
        using var db = CreateDb();
        var svc = new Mock<ITarifasService>();
        var ctrl = CreateCtrl(db, svc);
        var r = await ctrl.Reset();
        r.Result.Should().BeOfType<OkObjectResult>();
        (await db.TarifasBandas.FindAsync(1))!.PrecioBase.Should().Be(4.50m);
        svc.Verify(s => s.Invalidar(), Times.Once);
    }
}

public class TarifasControllerTests
{
    private readonly Mock<ITarifasService> _svc = new();
    private readonly TarifasController _ctrl;

    public TarifasControllerTests()
    {
        _ctrl = new TarifasController(NullLogger<TarifasController>.Instance, _svc.Object);
    }

    [Fact]
    public void Consultar_DevuelveOk()
    {
        _svc.Setup(s => s.Consultar(It.IsAny<TarifaConsultaInput>()))
            .Returns(new TarifaConsultaResult("Local", 1m, 1m, 1m, false, 0m,
                new List<TarifaOpcion> { new("Estandar", "d", "24h", 1, 4m, 0m, 0.84m, 4.84m) }));
        var r = _ctrl.ConsultarTarifas("todos", 1m);
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Consultar_FiltroPorTipo_DevuelveSoloCoincidentes()
    {
        _svc.Setup(s => s.Consultar(It.IsAny<TarifaConsultaInput>()))
            .Returns(new TarifaConsultaResult("Local", 1m, 1m, 1m, false, 0m,
                new List<TarifaOpcion>
                {
                    new("Estandar", "d", "24h", 1, 4m, 0m, 0.84m, 4.84m),
                    new("Premium", "d", "24h", 1, 6m, 0m, 1.26m, 7.26m)
                }));
        var r = _ctrl.ConsultarTarifas("estandar", 1m);
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Consultar_ExcepcionEnServicio_Devuelve500()
    {
        _svc.Setup(s => s.Consultar(It.IsAny<TarifaConsultaInput>())).Throws(new InvalidOperationException("x"));
        var r = _ctrl.ConsultarTarifas();
        r.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public void Calcular_Ok()
    {
        _svc.Setup(s => s.Calcular(It.IsAny<TarifaCalculoInput>()))
            .Returns(new TarifaCalculoResult("Estandar", "Local", "24h", 1, 1m, 1m, 1m, 4m, 0m, 0.84m, 4.84m, false, 0m));
        var r = _ctrl.CalcularPrecio(new CalcularPrecioRequestDto
        {
            Peso = 1m,
            CodigoPostalOrigen = "28001",
            CodigoPostalDestino = "28002"
        });
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Calcular_ModelStateInvalido_BadRequest()
    {
        _ctrl.ModelState.AddModelError("k", "err");
        var r = _ctrl.CalcularPrecio(new CalcularPrecioRequestDto());
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Calcular_Excepcion_Devuelve500()
    {
        _svc.Setup(s => s.Calcular(It.IsAny<TarifaCalculoInput>())).Throws(new InvalidOperationException("x"));
        var r = _ctrl.CalcularPrecio(new CalcularPrecioRequestDto
        {
            Peso = 1m,
            CodigoPostalOrigen = "28001",
            CodigoPostalDestino = "28002"
        });
        r.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}

public class OficinasControllerTests
{
    private readonly Mock<OficinasJsonService> _svc;
    private readonly OficinasController _ctrl;

    public OficinasControllerTests()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(Path.GetTempPath());
        _svc = new Mock<OficinasJsonService>(NullLogger<OficinasJsonService>.Instance, env.Object) { CallBase = false };
        _ctrl = new OficinasController(_svc.Object, NullLogger<OficinasController>.Instance);
    }

    [Fact]
    public void Buscar_PorCodigoPostal_Ok()
    {
        _svc.Setup(s => s.BuscarPorCodigoPostal("28001")).Returns(new List<OficinaDto> { new() { Id = 1, CodigoPostal = "28001" } });
        var r = _ctrl.BuscarOficinas("28001", null);
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Buscar_PorTexto_Ok()
    {
        _svc.Setup(s => s.BuscarPorTexto("madrid")).Returns(new List<OficinaDto>());
        var r = _ctrl.BuscarOficinas(null, "madrid");
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Buscar_SinParametros_BadRequest()
    {
        var r = _ctrl.BuscarOficinas(null, null);
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Buscar_Excepcion_500()
    {
        _svc.Setup(s => s.BuscarPorCodigoPostal(It.IsAny<string>())).Throws(new InvalidOperationException("x"));
        var r = _ctrl.BuscarOficinas("28001", null);
        r.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public void Obtener_Ok()
    {
        _svc.Setup(s => s.ObtenerTodas()).Returns(new List<OficinaDto> { new() { Id = 1 } });
        var r = _ctrl.ObtenerOficinas();
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void Obtener_Excepcion_500()
    {
        _svc.Setup(s => s.ObtenerTodas()).Throws(new InvalidOperationException("x"));
        var r = _ctrl.ObtenerOficinas();
        r.Should().BeOfType<ObjectResult>().Which.StatusCode.Should().Be(500);
    }
}

public class EtiquetasControllerTests
{
    private readonly Mock<IEnvioRepository> _envioRepo = new();
    private readonly Mock<IEtiquetaPdfService> _pdf = new();
    private readonly EtiquetasController _ctrl;

    public EtiquetasControllerTests()
    {
        _ctrl = new EtiquetasController(_envioRepo.Object, _pdf.Object, NullLogger<EtiquetasController>.Instance);
    }

    [Fact]
    public async Task DescargarEtiqueta_Existente_DevuelveFile()
    {
        _envioRepo.Setup(r => r.GetByTrackingAsync("NXP-1")).ReturnsAsync(new Envio { NumeroSeguimiento = "NXP-1" });
        _pdf.Setup(p => p.GenerarEtiqueta(It.IsAny<Envio>())).Returns(new byte[] { 1, 2, 3 });
        var r = await _ctrl.DescargarEtiqueta("NXP-1");
        r.Should().BeOfType<FileContentResult>();
    }

    [Fact]
    public async Task DescargarEtiqueta_Inexistente_NotFound()
    {
        _envioRepo.Setup(r => r.GetByTrackingAsync("X")).ReturnsAsync((Envio?)null);
        var r = await _ctrl.DescargarEtiqueta("X");
        r.Should().BeOfType<NotFoundObjectResult>();
    }
}

public class PagosControllerTests
{
    private readonly Mock<IEnvioRepository> _envioRepo = new();
    private readonly Mock<IStripeService> _stripe = new();
    private readonly Mock<IEtiquetaPdfService> _etiqueta = new();
    private readonly Mock<IFacturaPdfService> _factura = new();
    private readonly Mock<IEmailService> _email = new();
    private readonly Mock<ITrackingNumberGenerator> _tracking = new();
    private readonly Mock<ILogisticaNotifierService> _logistica = new();
    private readonly Mock<ITarifasService> _tarifas = new();
    private readonly PagosController _ctrl;

    public PagosControllerTests()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        _ctrl = new PagosController(
            _envioRepo.Object, _stripe.Object, _etiqueta.Object, _factura.Object,
            _email.Object, _tracking.Object, _logistica.Object, _tarifas.Object,
            config, NullLogger<PagosController>.Instance);
        WireUser("user-1");
    }

    private void WireUser(string? userId)
    {
        var claims = userId is null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, userId is null ? null : "Test");
        var user = new ClaimsPrincipal(identity);
        _ctrl.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
    }

    private static CrearSesionPagoDto ValidDto() => new()
    {
        Peso = 1m,
        Dimensiones = "10x10x10",
        CodigoPostalOrigen = "28001",
        CodigoPostalDestino = "08001",
        TipoTarifa = "Estandar",
        NombreRemitente = "A", ApellidosRemitente = "B", TelefonoRemitente = "1", EmailRemitente = "a@a.com",
        NombreDestinatario = "C", ApellidosDestinatario = "D", TelefonoDestinatario = "2",
        DireccionOrigen = "X", DireccionDestino = "Y",
        OficinaOrigenId = 1, OficinaDestinoId = 5, TipoEntrega = "Oficina",
        UrlBase = "https://app.local"
    };

    [Fact]
    public async Task CrearSesion_ModelStateInvalido_BadRequest()
    {
        _ctrl.ModelState.AddModelError("k", "err");
        var r = await _ctrl.CrearSesionPago(new CrearSesionPagoDto());
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearSesion_SinUserId_Unauthorized()
    {
        WireUser(null);
        var r = await _ctrl.CrearSesionPago(ValidDto());
        r.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task CrearSesion_TipoEntregaInvalido_BadRequest()
    {
        var dto = ValidDto();
        dto.TipoEntrega = "Aire";
        var r = await _ctrl.CrearSesionPago(dto);
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearSesion_SinOficinaOrigen_BadRequest()
    {
        var dto = ValidDto();
        dto.OficinaOrigenId = null;
        var r = await _ctrl.CrearSesionPago(dto);
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearSesion_OficinaSinDestino_BadRequest()
    {
        var dto = ValidDto();
        dto.TipoEntrega = "Oficina";
        dto.OficinaDestinoId = null;
        var r = await _ctrl.CrearSesionPago(dto);
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearSesion_DomicilioConOficinaDestino_BadRequest()
    {
        var dto = ValidDto();
        dto.TipoEntrega = "Domicilio";
        dto.OficinaDestinoId = 5;
        var r = await _ctrl.CrearSesionPago(dto);
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearSesion_Ok_FlujoCompleto()
    {
        _tarifas.Setup(t => t.ParseDimensiones(It.IsAny<string>())).Returns((10m, 10m, 10m));
        _tarifas.Setup(t => t.Calcular(It.IsAny<TarifaCalculoInput>()))
            .Returns(new TarifaCalculoResult("Estandar", "Local", "24h", 1, 1m, 1m, 1m, 4m, 0m, 0.84m, 4.84m, false, 0m));
        _tracking.Setup(t => t.Generate()).Returns("NXP-NEW");
        _tracking.Setup(t => t.GenerateExpedicion()).Returns("EXP-NEW");
        _envioRepo.Setup(r => r.CreateAsync(It.IsAny<Envio>())).ReturnsAsync((Envio e) => e);
        _stripe.Setup(s => s.CrearSesionCheckout(It.IsAny<Envio>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("https://stripe/sess", "sess_1"));
        _envioRepo.Setup(r => r.UpdateAsync(It.IsAny<Envio>())).Returns(Task.CompletedTask);

        var r = await _ctrl.CrearSesionPago(ValidDto());
        r.Should().BeOfType<OkObjectResult>();
        _envioRepo.Verify(r => r.CreateAsync(It.IsAny<Envio>()), Times.Once);
        _envioRepo.Verify(r => r.UpdateAsync(It.IsAny<Envio>()), Times.Once);
    }

    [Fact]
    public async Task VerificarPago_SinUserId_Unauthorized()
    {
        WireUser(null);
        var r = await _ctrl.VerificarPago("sess_1");
        r.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task VerificarPago_NoEncuentraEnvio_NotFound()
    {
        _envioRepo.Setup(r => r.GetByStripeSessionAsync("x")).ReturnsAsync((Envio?)null);
        var r = await _ctrl.VerificarPago("x");
        r.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task VerificarPago_OtroUsuario_NotFound()
    {
        _envioRepo.Setup(r => r.GetByStripeSessionAsync("sess_1"))
            .ReturnsAsync(new Envio { IdentityUserId = "otro" });
        var r = await _ctrl.VerificarPago("sess_1");
        r.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task VerificarPago_YaPagado_DevuelveOk()
    {
        _envioRepo.Setup(r => r.GetByStripeSessionAsync("sess_1"))
            .ReturnsAsync(new Envio { IdentityUserId = "user-1", Pagado = true, NumeroSeguimiento = "NXP-1" });
        var r = await _ctrl.VerificarPago("sess_1");
        r.Should().BeOfType<OkObjectResult>();
        _stripe.Verify(s => s.VerificarPagoSesion(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task VerificarPago_NoPagadoAun_LlamaStripeYDevuelveOk()
    {
        _envioRepo.Setup(r => r.GetByStripeSessionAsync("sess_1"))
            .ReturnsAsync(new Envio { IdentityUserId = "user-1", Pagado = false, NumeroSeguimiento = "NXP-1" });
        _stripe.Setup(s => s.VerificarPagoSesion("sess_1")).ReturnsAsync(false);
        var r = await _ctrl.VerificarPago("sess_1");
        r.Should().BeOfType<OkObjectResult>();
        _stripe.Verify(s => s.VerificarPagoSesion("sess_1"), Times.Once);
    }

    [Fact]
    public async Task ReintentarPago_ModelStateInvalido_BadRequest()
    {
        _ctrl.ModelState.AddModelError("k", "err");
        var r = await _ctrl.ReintentarPago("NXP-1", new ReintentarPagoDto());
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ReintentarPago_SinUserId_Unauthorized()
    {
        WireUser(null);
        var r = await _ctrl.ReintentarPago("NXP-1", new ReintentarPagoDto { UrlBase = "https://x" });
        r.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task ReintentarPago_EnvioNoExiste_NotFound()
    {
        _envioRepo.Setup(r => r.GetByTrackingAndUserAsync("NXP-X", "user-1")).ReturnsAsync((Envio?)null);
        var r = await _ctrl.ReintentarPago("NXP-X", new ReintentarPagoDto { UrlBase = "https://x" });
        r.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ReintentarPago_YaPagado_BadRequest()
    {
        _envioRepo.Setup(r => r.GetByTrackingAndUserAsync("NXP-1", "user-1"))
            .ReturnsAsync(new Envio { NumeroSeguimiento = "NXP-1", Pagado = true, EstadoActual = EstadoEnvio.Admitido });
        var r = await _ctrl.ReintentarPago("NXP-1", new ReintentarPagoDto { UrlBase = "https://x" });
        r.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ReintentarPago_Ok()
    {
        _envioRepo.Setup(r => r.GetByTrackingAndUserAsync("NXP-1", "user-1"))
            .ReturnsAsync(new Envio
            {
                NumeroSeguimiento = "NXP-1",
                Pagado = false,
                EstadoActual = EstadoEnvio.PendientePago,
                CosteCalculado = 5m,
                TiempoEntregaEstimado = "24h",
                TipoTarifa = "Estandar"
            });
        _stripe.Setup(s => s.CrearSesionCheckout(It.IsAny<Envio>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(("https://stripe/sess", "sess_new"));
        _envioRepo.Setup(r => r.UpdateAsync(It.IsAny<Envio>())).Returns(Task.CompletedTask);
        var r = await _ctrl.ReintentarPago("NXP-1", new ReintentarPagoDto { UrlBase = "https://x" });
        r.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Webhook_SinFirmaNiSecret_DevuelveOkAunconBodyInvalido()
    {
        // Sin webhook secret y body inválido → catch interno devuelve Ok igualmente
        var http = new DefaultHttpContext();
        var bodyBytes = System.Text.Encoding.UTF8.GetBytes("not-json");
        http.Request.Body = new MemoryStream(bodyBytes);
        _ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        var r = await _ctrl.StripeWebhook();
        r.Should().BeOfType<OkResult>();
    }
}
