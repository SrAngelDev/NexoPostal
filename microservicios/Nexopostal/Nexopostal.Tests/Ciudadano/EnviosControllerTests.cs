using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Ciudadano.Controllers;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using System.Security.Claims;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

public class EnviosControllerTests
{
    private const string ServiceKey = "test-service-key-12345";

    private readonly Mock<IEnvioRepository> _repo = new();
    private readonly Mock<ITrackingNumberGenerator> _gen = new();
    private readonly Mock<IFacturaPdfService> _factura = new();
    private readonly Mock<IEtiquetaPdfService> _etiqueta = new();
    private readonly Mock<ITarifasService> _tarifas = new();
    private readonly Mock<ITrackingNotificacionService> _notif = new();
    private readonly IConfiguration _config;

    public EnviosControllerTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InterServiceSettings:ServiceKey"] = ServiceKey
            })
            .Build();

        _tarifas.Setup(t => t.ParseDimensiones(It.IsAny<string?>()))
            .Returns(((decimal?)20m, (decimal?)15m, (decimal?)10m));
        _tarifas.Setup(t => t.Calcular(It.IsAny<TarifaCalculoInput>()))
            .Returns(new TarifaCalculoResult(
                TipoTarifa: "Estandar",
                Zona: "Local",
                TiempoEntregaEstimado: "1-2 días",
                TiempoEstimadoDias: 2,
                PesoReal: 1m, PesoVolumetrico: 1m, PesoFacturable: 1m,
                PrecioBase: 5m, Recargo: 0m, Iva: 1.05m,
                PrecioTotal: 6.05m,
                AplicaRecargo: false,
                RecargoPorcentaje: 0m));
        _gen.Setup(g => g.Generate()).Returns("NXP-TEST-001ES");
        _gen.Setup(g => g.GenerateExpedicion()).Returns("NXI-TEST-001");
    }

    private EnviosController CreateCtrl(string? userId = "user-1", string? serviceKey = null, int? oficinaHeader = null)
    {
        var ctrl = new EnviosController(
            _repo.Object, _gen.Object, _factura.Object, _etiqueta.Object,
            _tarifas.Object, _notif.Object, _config,
            NullLogger<EnviosController>.Instance);

        var claims = new List<Claim>();
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        var identity = new ClaimsIdentity(claims, userId != null ? "Test" : null);
        var user = new ClaimsPrincipal(identity);
        var http = new DefaultHttpContext { User = user };
        if (serviceKey != null) http.Request.Headers["X-Service-Key"] = serviceKey;
        ctrl.ControllerContext = new ControllerContext { HttpContext = http };
        return ctrl;
    }

    // ===== Cotizar =====

    [Fact]
    public void Cotizar_Ok_DevuelveResultado()
    {
        var ctrl = CreateCtrl();
        var dto = new CotizarEnvioDto { Peso = 1, CodigoPostalOrigen = "28001", CodigoPostalDestino = "28010" };

        var result = ctrl.Cotizar(dto) as OkObjectResult;

        result.Should().NotBeNull();
        var body = result!.Value as CotizacionResultadoDto;
        body!.Precio.Should().Be(6.05m);
        body.TiempoEstimadoDias.Should().Be(2);
    }

    [Fact]
    public void Cotizar_ModelStateInvalido_BadRequest()
    {
        var ctrl = CreateCtrl();
        ctrl.ModelState.AddModelError("Peso", "requerido");

        var result = ctrl.Cotizar(new CotizarEnvioDto());

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public void Cotizar_TiempoMayorQue2_ObservacionesEstandar()
    {
        _tarifas.Setup(t => t.Calcular(It.IsAny<TarifaCalculoInput>()))
            .Returns(new TarifaCalculoResult("Estandar", "Peninsula", "3-4 días", 4,
                1m, 1m, 1m, 5m, 0m, 1m, 6m, false, 0m));
        var ctrl = CreateCtrl();
        var result = ctrl.Cotizar(new CotizarEnvioDto
        {
            Peso = 1, CodigoPostalOrigen = "08001", CodigoPostalDestino = "28010"
        }) as OkObjectResult;
        var body = result!.Value as CotizacionResultadoDto;
        body!.Observaciones.Should().Be("Entrega estándar");
    }

    // ===== CrearEnvio =====

    private static CrearEnvioDto ValidCrearDto(string tipoEntrega = "Domicilio", int? oficinaDestino = null) => new()
    {
        Peso = 1m, Dimensiones = "20x15x10",
        NombreRemitente = "R", Origen = "C/ O", CodigoPostalOrigen = "28001", TelefonoRemitente = "1",
        NombreDestinatario = "D", Destino = "C/ D", CodigoPostalDestino = "28010", TelefonoDestinatario = "2",
        OficinaOrigenId = 5,
        TipoEntrega = tipoEntrega,
        OficinaDestinoId = oficinaDestino
    };

    [Fact]
    public async Task CrearEnvio_ModelStateInvalido_BadRequest()
    {
        var ctrl = CreateCtrl();
        ctrl.ModelState.AddModelError("Peso", "req");
        var result = await ctrl.CrearEnvio(new CrearEnvioDto());
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearEnvio_TipoEntregaInvalido_BadRequest()
    {
        var ctrl = CreateCtrl();
        var result = await ctrl.CrearEnvio(ValidCrearDto("Otro"));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearEnvio_OficinaTipoSinDestino_BadRequest()
    {
        var ctrl = CreateCtrl();
        var result = await ctrl.CrearEnvio(ValidCrearDto("Oficina"));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearEnvio_DomicilioConDestino_BadRequest()
    {
        var ctrl = CreateCtrl();
        var result = await ctrl.CrearEnvio(ValidCrearDto("Domicilio", oficinaDestino: 9));
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearEnvio_OficinaOrigenInvalida_BadRequest()
    {
        var ctrl = CreateCtrl();
        var dto = ValidCrearDto();
        dto.OficinaOrigenId = 0;
        var result = await ctrl.CrearEnvio(dto);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CrearEnvio_SinUserId_Unauthorized()
    {
        var ctrl = CreateCtrl(userId: null);
        var result = await ctrl.CrearEnvio(ValidCrearDto());
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task CrearEnvio_Ok_DevuelveCreated()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.CreateAsync(It.IsAny<Envio>())).ReturnsAsync((Envio e) => e);

        var result = await ctrl.CrearEnvio(ValidCrearDto("Oficina", oficinaDestino: 9));
        var created = result as CreatedAtActionResult;

        created.Should().NotBeNull();
        var body = created!.Value as EnvioCreadoDto;
        body!.NumeroSeguimiento.Should().Be("NXP-TEST-001ES");
        body.NumeroExpedicion.Should().Be("NXI-TEST-001");
        body.TipoEntrega.Should().Be("Oficina");
        _repo.Verify(r => r.CreateAsync(It.IsAny<Envio>()), Times.Once);
    }

    // ===== GetEnvioPorNumero (tracking público) =====

    [Fact]
    public async Task GetEnvioPorNumero_NotFound()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByTrackingAsync("X")).ReturnsAsync((Envio?)null);
        var result = await ctrl.GetEnvioPorNumero("X");
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetEnvioPorNumero_Ok_ConFechaEntrega()
    {
        var ctrl = CreateCtrl();
        var envio = new Envio
        {
            NumeroSeguimiento = "T1",
            EstadoActual = EstadoEnvio.Entregado,
            EstadoInternoActual = EstadoInterno.EntregadoEnDomicilio,
            FechaCreacion = DateTime.UtcNow.AddDays(-3),
            FechaPago = DateTime.UtcNow.AddDays(-2)
        };
        _repo.Setup(r => r.GetByTrackingAsync("T1")).ReturnsAsync(envio);

        var result = await ctrl.GetEnvioPorNumero("T1") as OkObjectResult;
        var body = result!.Value as EnvioTrackingDto;

        body!.EstadoActual.Should().Be("Entregado");
        body.FechaEntrega.Should().NotBeNull();
    }

    [Fact]
    public async Task GetEnvioPorNumero_NoEntregado_FechaEntregaNull()
    {
        var ctrl = CreateCtrl();
        var envio = new Envio { NumeroSeguimiento = "T2", EstadoActual = EstadoEnvio.EnTransito, EstadoInternoActual = EstadoInterno.EnTransitoIntermedio };
        _repo.Setup(r => r.GetByTrackingAsync("T2")).ReturnsAsync(envio);

        var result = await ctrl.GetEnvioPorNumero("T2") as OkObjectResult;
        var body = result!.Value as EnvioTrackingDto;
        body!.FechaEntrega.Should().BeNull();
    }

    // ===== GetMisEnvios =====

    [Fact]
    public async Task GetMisEnvios_SinUserId_Unauthorized()
    {
        var ctrl = CreateCtrl(userId: null);
        var result = await ctrl.GetMisEnvios();
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetMisEnvios_Ok_MapeaResumen()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByUserAsync("user-1")).ReturnsAsync(new List<Envio>
        {
            new() { NumeroSeguimiento = "A", EstadoActual = EstadoEnvio.Admitido, CosteCalculado = 5, Destino = "D", TipoTarifa = "Estandar", Pagado = true }
        });
        var result = await ctrl.GetMisEnvios() as OkObjectResult;
        var body = result!.Value as List<EnvioResumenDto>;
        body!.Should().HaveCount(1);
    }

    // ===== DescargarFactura =====

    [Fact]
    public async Task DescargarFactura_SinUser_Unauthorized()
    {
        var ctrl = CreateCtrl(userId: null);
        var result = await ctrl.DescargarFactura("X");
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task DescargarFactura_NotFound()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByTrackingAndUserAsync("X", "user-1")).ReturnsAsync((Envio?)null);
        var result = await ctrl.DescargarFactura("X");
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DescargarFactura_NoPagado_BadRequest()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByTrackingAndUserAsync("X", "user-1"))
            .ReturnsAsync(new Envio { Pagado = false });
        var result = await ctrl.DescargarFactura("X");
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DescargarFactura_Pagado_DevuelveFile()
    {
        var ctrl = CreateCtrl();
        var envio = new Envio { Pagado = true };
        _repo.Setup(r => r.GetByTrackingAndUserAsync("X", "user-1")).ReturnsAsync(envio);
        _factura.Setup(f => f.GenerarFactura(envio)).Returns(new byte[] { 1, 2, 3 });

        var result = await ctrl.DescargarFactura("X") as FileContentResult;
        result.Should().NotBeNull();
        result!.ContentType.Should().Be("application/pdf");
    }

    // ===== DescargarEtiqueta =====

    [Fact]
    public async Task DescargarEtiqueta_SinUser_Unauthorized()
    {
        var ctrl = CreateCtrl(userId: null);
        var result = await ctrl.DescargarEtiqueta("X");
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task DescargarEtiqueta_NotFound()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByTrackingAndUserAsync("X", "user-1")).ReturnsAsync((Envio?)null);
        var result = await ctrl.DescargarEtiqueta("X");
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DescargarEtiqueta_NoPagado_BadRequest()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByTrackingAndUserAsync("X", "user-1"))
            .ReturnsAsync(new Envio { Pagado = false });
        var result = await ctrl.DescargarEtiqueta("X");
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DescargarEtiqueta_Pagado_DevuelveFile()
    {
        var ctrl = CreateCtrl();
        var envio = new Envio { Pagado = true };
        _repo.Setup(r => r.GetByTrackingAndUserAsync("X", "user-1")).ReturnsAsync(envio);
        _etiqueta.Setup(f => f.GenerarEtiqueta(envio)).Returns(new byte[] { 9, 9 });

        var result = await ctrl.DescargarEtiqueta("X") as FileContentResult;
        result.Should().NotBeNull();
    }

    // ===== AltaEnvioOficinaInterno =====

    private static AltaEnvioOficinaDto ValidAltaDto(string tipoEntrega = "Domicilio", int? oficinaDestino = null) => new()
    {
        Peso = 1m, Dimensiones = "20x15x10",
        NombreRemitente = "R", Origen = "C/O", CodigoPostalOrigen = "28001", TelefonoRemitente = "1",
        NombreDestinatario = "D", Destino = "C/D", CodigoPostalDestino = "28010", TelefonoDestinatario = "2",
        TipoEntrega = tipoEntrega,
        OficinaDestinoId = oficinaDestino,
        MetodoCobro = "Efectivo"
    };

    [Fact]
    public async Task AltaOficinaInterno_SinServiceKey_Forbid()
    {
        var ctrl = CreateCtrl(serviceKey: null);
        var result = await ctrl.AltaEnvioOficinaInterno(ValidAltaDto(), 5);
        (result as ObjectResult)!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task AltaOficinaInterno_KeyInvalida_Forbid()
    {
        var ctrl = CreateCtrl(serviceKey: "wrong");
        var result = await ctrl.AltaEnvioOficinaInterno(ValidAltaDto(), 5);
        (result as ObjectResult)!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task AltaOficinaInterno_ModelStateInvalido_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        ctrl.ModelState.AddModelError("X", "err");
        var result = await ctrl.AltaEnvioOficinaInterno(ValidAltaDto(), 5);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AltaOficinaInterno_HeaderSinValor_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var result = await ctrl.AltaEnvioOficinaInterno(ValidAltaDto(), null);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AltaOficinaInterno_TipoEntregaInvalido_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var result = await ctrl.AltaEnvioOficinaInterno(ValidAltaDto("Otro"), 5);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AltaOficinaInterno_OficinaSinDestino_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var result = await ctrl.AltaEnvioOficinaInterno(ValidAltaDto("Oficina"), 5);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AltaOficinaInterno_DomicilioConDestino_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var result = await ctrl.AltaEnvioOficinaInterno(ValidAltaDto("Domicilio", 7), 5);
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AltaOficinaInterno_Ok_Created()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        _repo.Setup(r => r.CreateAsync(It.IsAny<Envio>())).ReturnsAsync((Envio e) => e);

        var result = await ctrl.AltaEnvioOficinaInterno(ValidAltaDto("Oficina", 9), 5);
        var status = result as ObjectResult;
        status!.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    // ===== GetEnvioInternoService =====

    [Fact]
    public async Task GetEnvioInternoService_SinKey_Forbid()
    {
        var ctrl = CreateCtrl();
        var result = await ctrl.GetEnvioInternoService("E1");
        (result as ObjectResult)!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task GetEnvioInternoService_NotFound()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        _repo.Setup(r => r.GetByExpedicionAsync("E1")).ReturnsAsync((Envio?)null);
        var result = await ctrl.GetEnvioInternoService("E1");
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetEnvioInternoService_Ok()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        _repo.Setup(r => r.GetByExpedicionAsync("E1")).ReturnsAsync(new Envio
        {
            NumeroSeguimiento = "S1", NumeroExpedicion = "E1",
            EstadoActual = EstadoEnvio.Admitido, EstadoInternoActual = EstadoInterno.PendienteRecogida,
            TipoEntrega = TipoEntrega.Domicilio
        });
        var result = await ctrl.GetEnvioInternoService("E1") as OkObjectResult;
        result!.Value.Should().BeOfType<EnvioInternoServiceDto>();
    }

    // ===== GetEnvioInterno y GetEnvioInternoPorSeguimiento =====

    [Fact]
    public async Task GetEnvioInterno_NotFound()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByExpedicionAsync("X")).ReturnsAsync((Envio?)null);
        var result = await ctrl.GetEnvioInterno("X");
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetEnvioInterno_Ok()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByExpedicionAsync("X")).ReturnsAsync(new Envio
        {
            NumeroSeguimiento = "S", NumeroExpedicion = "X",
            EstadoActual = EstadoEnvio.Admitido, EstadoInternoActual = EstadoInterno.PendienteRecogida
        });
        var result = await ctrl.GetEnvioInterno("X") as OkObjectResult;
        result!.Value.Should().BeOfType<EnvioInternoDetalladoDto>();
    }

    [Fact]
    public async Task GetEnvioInternoPorSeguimiento_NotFound()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync((Envio?)null);
        var result = await ctrl.GetEnvioInternoPorSeguimiento("S");
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetEnvioInternoPorSeguimiento_Ok()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync(new Envio
        {
            NumeroSeguimiento = "S", NumeroExpedicion = "E",
            EstadoActual = EstadoEnvio.EnTransito, EstadoInternoActual = EstadoInterno.EnTransitoIntermedio
        });
        var result = await ctrl.GetEnvioInternoPorSeguimiento("S");
        result.Should().BeOfType<OkObjectResult>();
    }

    // ===== ListarEnviosInternos =====

    [Fact]
    public async Task ListarEnviosInternos_SinFiltros_Ok()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByEstadoInternoAsync(null, null)).ReturnsAsync(new List<Envio>
        {
            new() { NumeroSeguimiento = "A", NumeroExpedicion = "EA", EstadoActual = EstadoEnvio.Admitido, EstadoInternoActual = EstadoInterno.PendienteRecogida }
        });
        var result = await ctrl.ListarEnviosInternos();
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task ListarEnviosInternos_ConFiltroEstadoValido_PasaEnum()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByEstadoInternoAsync(EstadoInterno.EnReparto, "28001"))
            .ReturnsAsync(new List<Envio>());
        var result = await ctrl.ListarEnviosInternos("EnReparto", "28001");
        result.Should().BeOfType<OkObjectResult>();
        _repo.Verify(r => r.GetByEstadoInternoAsync(EstadoInterno.EnReparto, "28001"), Times.Once);
    }

    [Fact]
    public async Task ListarEnviosInternos_FiltroEstadoInvalido_PasaNull()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByEstadoInternoAsync(null, null)).ReturnsAsync(new List<Envio>());
        var result = await ctrl.ListarEnviosInternos("EstadoBasura");
        result.Should().BeOfType<OkObjectResult>();
        _repo.Verify(r => r.GetByEstadoInternoAsync(null, null), Times.Once);
    }

    // ===== ActualizarEstadoInterno =====

    [Fact]
    public async Task ActualizarEstadoInterno_ModelStateInvalido_BadRequest()
    {
        var ctrl = CreateCtrl();
        ctrl.ModelState.AddModelError("x", "e");
        var result = await ctrl.ActualizarEstadoInterno("E", new ActualizarEstadoInternoDto());
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ActualizarEstadoInterno_NotFound()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByExpedicionAsync("E")).ReturnsAsync((Envio?)null);
        var result = await ctrl.ActualizarEstadoInterno("E", new ActualizarEstadoInternoDto { NuevoEstadoInterno = "EnReparto" });
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ActualizarEstadoInterno_EstadoInvalido_BadRequest()
    {
        var ctrl = CreateCtrl();
        _repo.Setup(r => r.GetByExpedicionAsync("E")).ReturnsAsync(new Envio { NumeroSeguimiento = "S", NumeroExpedicion = "E" });
        var result = await ctrl.ActualizarEstadoInterno("E", new ActualizarEstadoInternoDto { NuevoEstadoInterno = "NoExiste" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ActualizarEstadoInterno_Ok_NotificaCambio()
    {
        var ctrl = CreateCtrl();
        var envio = new Envio { NumeroSeguimiento = "S", NumeroExpedicion = "E", Observaciones = "previa" };
        _repo.Setup(r => r.GetByExpedicionAsync("E")).ReturnsAsync(envio);
        _repo.Setup(r => r.UpdateAsync(envio)).Returns(Task.CompletedTask);

        var result = await ctrl.ActualizarEstadoInterno("E", new ActualizarEstadoInternoDto
        {
            NuevoEstadoInterno = "EnReparto",
            Observaciones = "se está repartiendo"
        });

        result.Should().BeOfType<OkObjectResult>();
        envio.EstadoInternoActual.Should().Be(EstadoInterno.EnReparto);
        envio.EstadoActual.Should().Be(EstadoEnvio.EnReparto);
        envio.Observaciones.Should().Contain("se está repartiendo");
        _notif.Verify(n => n.NotificarCambioEstado(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarEstadoInterno_Entregado_NotificaEntrega()
    {
        var ctrl = CreateCtrl();
        var envio = new Envio { NumeroSeguimiento = "S", NumeroExpedicion = "E" };
        _repo.Setup(r => r.GetByExpedicionAsync("E")).ReturnsAsync(envio);

        var result = await ctrl.ActualizarEstadoInterno("E", new ActualizarEstadoInternoDto
        {
            NuevoEstadoInterno = "EntregadoEnDomicilio"
        });

        result.Should().BeOfType<OkObjectResult>();
        _notif.Verify(n => n.NotificarEntregaCompletada(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarEstadoInterno_Incidencia_NotificaIncidencia()
    {
        var ctrl = CreateCtrl();
        var envio = new Envio { NumeroSeguimiento = "S", NumeroExpedicion = "E" };
        _repo.Setup(r => r.GetByExpedicionAsync("E")).ReturnsAsync(envio);

        var result = await ctrl.ActualizarEstadoInterno("E", new ActualizarEstadoInternoDto
        {
            NuevoEstadoInterno = "IncidenciaDireccionIncorrecta"
        });

        result.Should().BeOfType<OkObjectResult>();
        _notif.Verify(n => n.NotificarIncidencia(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    // ===== NotificarUbicacionReparto =====

    [Fact]
    public async Task NotificarUbicacion_SinKey_Forbid()
    {
        var ctrl = CreateCtrl();
        var result = await ctrl.NotificarUbicacionReparto(new TrackingUbicacionRepartoDto { NumeroSeguimiento = "S" });
        (result as ObjectResult)!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task NotificarUbicacion_ModelStateInvalido_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        ctrl.ModelState.AddModelError("x", "e");
        var result = await ctrl.NotificarUbicacionReparto(new TrackingUbicacionRepartoDto { NumeroSeguimiento = "S" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task NotificarUbicacion_NotFound()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync((Envio?)null);
        var result = await ctrl.NotificarUbicacionReparto(new TrackingUbicacionRepartoDto { NumeroSeguimiento = "S" });
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task NotificarUbicacion_Ok_Accepted()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync(new Envio { NumeroSeguimiento = "S" });
        var dto = new TrackingUbicacionRepartoDto { NumeroSeguimiento = "S", Latitud = 40, Longitud = -3, Ubicacion = "Aquí" };
        var result = await ctrl.NotificarUbicacionReparto(dto);
        result.Should().BeOfType<AcceptedResult>();
        _notif.Verify(n => n.NotificarCambioUbicacion("S", "Aquí", "RepartidorEnRuta", It.IsAny<string>(), 40, -3), Times.Once);
    }

    [Fact]
    public async Task NotificarUbicacion_SinUbicacion_GeneraTextoConCoordenadas()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync(new Envio { NumeroSeguimiento = "S" });
        var dto = new TrackingUbicacionRepartoDto { NumeroSeguimiento = "S", Latitud = 40.5, Longitud = -3.2 };
        var result = await ctrl.NotificarUbicacionReparto(dto);
        result.Should().BeOfType<AcceptedResult>();
    }

    // ===== NotificarEventoEntregaReparto =====

    [Fact]
    public async Task NotificarEventoEntrega_SinKey_Forbid()
    {
        var ctrl = CreateCtrl();
        var result = await ctrl.NotificarEventoEntregaReparto(new TrackingEventoEntregaDto { NumeroSeguimiento = "S", EstadoEntrega = "ENTREGADO" });
        (result as ObjectResult)!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task NotificarEventoEntrega_ModelStateInvalido_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        ctrl.ModelState.AddModelError("x", "e");
        var result = await ctrl.NotificarEventoEntregaReparto(new TrackingEventoEntregaDto());
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task NotificarEventoEntrega_NotFound()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync((Envio?)null);
        var result = await ctrl.NotificarEventoEntregaReparto(new TrackingEventoEntregaDto { NumeroSeguimiento = "S", EstadoEntrega = "ENTREGADO" });
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task NotificarEventoEntrega_EstadoInvalido_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync(new Envio { NumeroSeguimiento = "S" });
        var result = await ctrl.NotificarEventoEntregaReparto(new TrackingEventoEntregaDto { NumeroSeguimiento = "S", EstadoEntrega = "BASURA" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory]
    [InlineData("ENTREGADO", EstadoInterno.EntregadoEnDomicilio)]
    [InlineData("ENTREGADOPUNTOALTERNATIVO", EstadoInterno.EntregadoEnOficina)]
    [InlineData("DIRECCIONINCORRECTA", EstadoInterno.IncidenciaDireccionIncorrecta)]
    [InlineData("RECHAZADO", EstadoInterno.IncidenciaDestinatarioRechaza)]
    [InlineData("DEVUELTOAOFICINA", EstadoInterno.EnDevolucionAlRemitente)]
    [InlineData("ENCAMINO", EstadoInterno.EnReparto)]
    [InlineData("PENDIENTE", EstadoInterno.AsignadoARuta)]
    public async Task NotificarEventoEntrega_EstadosValidos_Aplican(string estado, EstadoInterno esperado)
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var envio = new Envio { NumeroSeguimiento = "S" };
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync(envio);
        var result = await ctrl.NotificarEventoEntregaReparto(new TrackingEventoEntregaDto
        {
            NumeroSeguimiento = "S",
            EstadoEntrega = estado,
            Observaciones = "obs"
        });
        result.Should().BeOfType<AcceptedResult>();
        envio.EstadoInternoActual.Should().Be(esperado);
    }

    [Fact]
    public async Task NotificarEventoEntrega_AusenteIntento1_PrimerIntento()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var envio = new Envio { NumeroSeguimiento = "S" };
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync(envio);
        var result = await ctrl.NotificarEventoEntregaReparto(new TrackingEventoEntregaDto
        {
            NumeroSeguimiento = "S", EstadoEntrega = "AUSENTE", NumeroIntento = 1
        });
        result.Should().BeOfType<AcceptedResult>();
        envio.EstadoInternoActual.Should().Be(EstadoInterno.PrimerIntentoFallido);
    }

    [Fact]
    public async Task NotificarEventoEntrega_AusenteIntento2_SegundoIntento()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var envio = new Envio { NumeroSeguimiento = "S" };
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync(envio);
        var result = await ctrl.NotificarEventoEntregaReparto(new TrackingEventoEntregaDto
        {
            NumeroSeguimiento = "S", EstadoEntrega = "AUSENTE", NumeroIntento = 2,
            Latitud = 40, Longitud = -3
        });
        result.Should().BeOfType<AcceptedResult>();
        envio.EstadoInternoActual.Should().Be(EstadoInterno.SegundoIntentoFallido);
    }

    // ===== NotificarEstadoScan =====

    [Fact]
    public async Task NotificarEstadoScan_SinKey_Forbid()
    {
        var ctrl = CreateCtrl();
        var result = await ctrl.NotificarEstadoScan(new TrackingScanEstadoDto { NumeroSeguimiento = "S", EstadoInterno = "EnReparto" });
        (result as ObjectResult)!.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task NotificarEstadoScan_ModelStateInvalido_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        ctrl.ModelState.AddModelError("x", "e");
        var result = await ctrl.NotificarEstadoScan(new TrackingScanEstadoDto());
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task NotificarEstadoScan_SinCodigos_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var result = await ctrl.NotificarEstadoScan(new TrackingScanEstadoDto { EstadoInterno = "EnReparto" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task NotificarEstadoScan_EstadoInvalido_BadRequest()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var result = await ctrl.NotificarEstadoScan(new TrackingScanEstadoDto { NumeroSeguimiento = "S", EstadoInterno = "Inventado" });
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task NotificarEstadoScan_NotFound()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync((Envio?)null);
        _repo.Setup(r => r.GetByExpedicionAsync(It.IsAny<string>())).ReturnsAsync((Envio?)null);
        var result = await ctrl.NotificarEstadoScan(new TrackingScanEstadoDto { NumeroSeguimiento = "S", EstadoInterno = "EnReparto" });
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task NotificarEstadoScan_PorSeguimiento_Ok()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var envio = new Envio { NumeroSeguimiento = "S", NumeroExpedicion = "E" };
        _repo.Setup(r => r.GetByTrackingAsync("S")).ReturnsAsync(envio);
        var result = await ctrl.NotificarEstadoScan(new TrackingScanEstadoDto { NumeroSeguimiento = "S", EstadoInterno = "EnReparto", Descripcion = "hola" });
        result.Should().BeOfType<AcceptedResult>();
        envio.EstadoInternoActual.Should().Be(EstadoInterno.EnReparto);
    }

    [Fact]
    public async Task NotificarEstadoScan_PorExpedicion_Ok()
    {
        var ctrl = CreateCtrl(serviceKey: ServiceKey);
        var envio = new Envio { NumeroSeguimiento = "S", NumeroExpedicion = "E" };
        _repo.Setup(r => r.GetByExpedicionAsync("E")).ReturnsAsync(envio);
        var result = await ctrl.NotificarEstadoScan(new TrackingScanEstadoDto { NumeroExpedicion = "E", EstadoInterno = "EnReparto" });
        result.Should().BeOfType<AcceptedResult>();
    }
}
