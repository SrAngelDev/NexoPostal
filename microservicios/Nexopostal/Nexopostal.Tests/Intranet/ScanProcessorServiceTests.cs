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
/// Tests unitarios para ScanProcessorService.
/// Cubre todos los modos de operación del flujo logístico,
/// validaciones de entrada, bifurcaciones por TipoEntrega y ProcesarLote.
/// </summary>
public class ScanProcessorServiceTests
{
    // ─── Mocks ───
    private readonly Mock<IMovimientoPaqueteRepository> _movimientoRepo = new();
    private readonly Mock<IHistorialService> _historialService = new();
    private readonly Mock<IClasificacionService> _clasificacionService = new();
    private readonly Mock<IMovimientoService> _movimientoService = new();
    private readonly Mock<INotificacionService> _notificacionService = new();
    private readonly Mock<ICiudadanoEnvioLookupService> _ciudadanoLookup = new();
    private readonly Mock<ICiudadanoEstadoNotifierService> _ciudadanoNotifier = new();
    private readonly Mock<IRepartoBandejaService> _repartoBandeja = new();
    private readonly Mock<IAsignacionPaqueteRepository> _asignacionRepo = new();
    private readonly Mock<IOperarioCtaRepository> _operarioRepo = new();
    private readonly Mock<IOperarioOficinaRepository> _operarioOficinaRepo = new();
    private readonly Mock<IOficinaPostalService> _oficinaService = new();

    public ScanProcessorServiceTests()
    {
        // Historial: ObtenerUltimoEvento retorna null (primer evento), RegistrarEvento OK
        _historialService
            .Setup(h => h.ObtenerUltimoEvento(It.IsAny<string>()))
            .ReturnsAsync((HistorialEventoInternoDto?)null);
        _historialService
            .Setup(h => h.RegistrarEvento(It.IsAny<CrearHistorialEventoDto>()))
            .ReturnsAsync(new HistorialEventoInternoDto { Id = 1, Estado = "TestEstado" });

        // Notificaciones ciudadano
        _ciudadanoNotifier
            .Setup(c => c.NotificarEstadoAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Notificaciones SignalR — configurar todos los métodos que llama el servicio
        _notificacionService
            .Setup(n => n.NotificarPaqueteRecibidoEnCta(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);
        _notificacionService
            .Setup(n => n.NotificarGeneralCta(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _notificacionService
            .Setup(n => n.NotificarPaqueteDisponibleParaReparto(
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(Task.CompletedTask);
        _notificacionService
            .Setup(n => n.NotificarTareaAsignada(
                It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _notificacionService
            .Setup(n => n.NotificarNuevoPaqueteEnOficina(
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        // Asignaciones: sin tarea existente, CreateAsync y Count OK
        _asignacionRepo
            .Setup(a => a.GetByExpedicionTipoCtaAsync(
                It.IsAny<string>(), It.IsAny<TipoTarea>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync((AsignacionPaquete?)null);
        _asignacionRepo
            .Setup(a => a.GetByExpedicionTipoOficinaAsync(
                It.IsAny<string>(), It.IsAny<TipoTarea>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync((AsignacionPaquete?)null);
        _asignacionRepo
            .Setup(a => a.CreateAsync(It.IsAny<AsignacionPaquete>()))
            .ReturnsAsync((AsignacionPaquete a) => a);
        _asignacionRepo
            .Setup(a => a.CountByOperarioAndEstadoAsync(It.IsAny<int>(), It.IsAny<EstadoTarea>()))
            .ReturnsAsync(0);
        _asignacionRepo
            .Setup(a => a.GetByOperarioOficinaAsync(It.IsAny<int>(), It.IsAny<EstadoTarea?>()))
            .ReturnsAsync(new List<AsignacionPaquete>());

        // Operarios: lista vacía → AutoAsignar no asigna pero no lanza excepción
        _operarioRepo
            .Setup(o => o.GetByCtaIdAsync(It.IsAny<int>(), It.IsAny<bool?>()))
            .ReturnsAsync(new List<OperarioCta>());
        _operarioOficinaRepo
            .Setup(o => o.GetByOficinaAsync(It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new List<OperarioOficina>());

        // Movimientos: sin movimientos preexistentes, crear devuelve el mismo objeto con ID
        _movimientoRepo
            .Setup(m => m.GetRecibidoByExpedicionAndCtaDestinoAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((MovimientoPaquete?)null);
        _movimientoRepo
            .Setup(m => m.GetProgramadoByExpedicionAndCtaOrigenAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((MovimientoPaquete?)null);
        _movimientoRepo
            .Setup(m => m.GetEnTransitoByExpedicionAndCtaDestinoAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync((MovimientoPaquete?)null);
        _movimientoRepo
            .Setup(m => m.CreateAsync(It.IsAny<MovimientoPaquete>()))
            .ReturnsAsync((MovimientoPaquete m) => { m.Id = 99; return m; });

        // Lookup Ciudadano: null → TipoEntrega Domicilio por defecto
        _ciudadanoLookup
            .Setup(l => l.ObtenerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnvioInternoServiceLookupDto?)null);

        // Bandeja Reparto: siempre éxito
        _repartoBandeja
            .Setup(r => r.RegistrarPaqueteAsync(It.IsAny<RegistrarPaqueteBandejaIntranetDto>()))
            .ReturnsAsync(new RegistrarBandejaResultDto { Success = true });
    }

    private ScanProcessorService BuildService() => new(
        _movimientoRepo.Object,
        _historialService.Object,
        _clasificacionService.Object,
        _movimientoService.Object,
        _notificacionService.Object,
        _ciudadanoLookup.Object,
        _ciudadanoNotifier.Object,
        _repartoBandeja.Object,
        _asignacionRepo.Object,
        _operarioRepo.Object,
        _operarioOficinaRepo.Object,
        _oficinaService.Object,
        NullLogger<ScanProcessorService>.Instance);

    // ═══════════════════════════════════════════
    //  VALIDACIONES DE ENTRADA
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_CodigoVacio_RetornaExitoFalsoConMensaje()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "   ",
            ModoOperacion = ModosEscaneo.RecepcionCta
        });

        result.Exito.Should().BeFalse();
        result.Mensaje.Should().Contain("Código escaneado vacío");
    }

    [Fact]
    public async Task ProcesarEscaneo_ModoInvalido_RetornaExitoFalsoConMensaje()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000001",
            ModoOperacion = "MODO_NO_EXISTE"
        });

        result.Exito.Should().BeFalse();
        result.Mensaje.Should().Contain("Modo de operación desconocido");
    }

    // ═══════════════════════════════════════════
    //  RECEPCION OFICINA
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_RecepcionOficina_SinOficinaJsonId_RetornaError()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000001",
            ModoOperacion = ModosEscaneo.RecepcionOficina
            // OficinaJsonId omitido
        });

        result.Exito.Should().BeFalse();
        result.Mensaje.Should().Contain("oficina");
    }

    [Fact]
    public async Task ProcesarEscaneo_RecepcionOficina_ConOficinaId_RetornaRecogidoEnOrigen()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000001",
            ModoOperacion = ModosEscaneo.RecepcionOficina,
            OficinaJsonId = 10,
            OficinaNombre = "NexoPostal Madrid"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("RecogidoEnOrigen");
        _historialService.Verify(
            h => h.RegistrarEvento(It.Is<CrearHistorialEventoDto>(d => d.Estado == "RecogidoEnOrigen")),
            Times.Once);
    }

    // ═══════════════════════════════════════════
    //  SALIDA OFICINA A CTA
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_SalidaOficinaACta_SinOficinaJsonId_RetornaError()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000002",
            ModoOperacion = ModosEscaneo.SalidaOficinaACta
        });

        result.Exito.Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarEscaneo_SalidaOficinaACta_ConOficinaId_RetornaEnTransitoACentroOrigen()
    {
        _clasificacionService
            .Setup(c => c.ResolverCtaDestino("28001"))
            .ReturnsAsync(new ResolverCtaResponseDto
            {
                CtaId = 1, CtaCodigo = "CTA-MAD", Provincia = "Madrid", Area = "Centro"
            });

        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000002",
            ModoOperacion = ModosEscaneo.SalidaOficinaACta,
            OficinaJsonId = 10,
            OficinaNombre = "NexoPostal Madrid",
            CodigoPostalOrigen = "28001"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("EnTransitoACentroOrigen");
        result.NotificacionEnviada.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    //  RECEPCION CTA
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_RecepcionCta_SinCtaId_RetornaError()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000003",
            ModoOperacion = ModosEscaneo.RecepcionCta
        });

        result.Exito.Should().BeFalse();
        result.Mensaje.Should().Contain("CTA");
    }

    [Fact]
    public async Task ProcesarEscaneo_RecepcionCta_SinCpDestino_RetornaRecibidoSinTroncal()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000003",
            ModoOperacion = ModosEscaneo.RecepcionCta,
            CtaId = 1,
            CtaCodigo = "CTA-MAD"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("RecibidoEnCentroOrigen");
        result.MovimientoTroncalCreado.Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarEscaneo_RecepcionCta_ConCpDestinoDiferente_CreaMovimientoTroncal()
    {
        _clasificacionService
            .Setup(c => c.ResolverCtaDestino("08001"))
            .ReturnsAsync(new ResolverCtaResponseDto
            {
                CtaId = 2, CtaCodigo = "CTA-BCN", Provincia = "Barcelona", Area = "Este"
            });
        _clasificacionService
            .Setup(c => c.DeterminarTipoTransporte(1, 2, false))
            .ReturnsAsync(TipoTransporte.Terrestre);

        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000003",
            ModoOperacion = ModosEscaneo.RecepcionCta,
            CtaId = 1,
            CtaCodigo = "CTA-MAD",
            CodigoPostalDestino = "08001"
        });

        result.Exito.Should().BeTrue();
        result.MovimientoTroncalCreado.Should().BeTrue();
        _movimientoRepo.Verify(
            m => m.CreateAsync(It.Is<MovimientoPaquete>(x =>
                x.CtaOrigenId == 1 && x.CtaDestinoId == 2 &&
                x.TipoTransporte == TipoTransporte.Terrestre)),
            Times.Once);
    }

    // ═══════════════════════════════════════════
    //  CLASIFICACION
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_Clasificacion_SinCtaId_RetornaError()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000004",
            ModoOperacion = ModosEscaneo.Clasificacion
        });

        result.Exito.Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarEscaneo_Clasificacion_NoUltimaMilla_RetornaClasificadoParaExpedicion()
    {
        // Sin movimiento recibido → no es última milla (setup por defecto retorna null)
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000004",
            ModoOperacion = ModosEscaneo.Clasificacion,
            CtaId = 1,
            CtaCodigo = "CTA-MAD"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("ClasificadoParaExpedicion");
    }

    [Fact]
    public async Task ProcesarEscaneo_Clasificacion_UltimaMillaDomicilio_RetornaAsignadoARuta()
    {
        // Movimiento recibido → es última milla; Ciudadano retorna null → Domicilio
        _movimientoRepo
            .Setup(m => m.GetRecibidoByExpedicionAndCtaDestinoAsync("NXI-ULTMILLA-001", 1))
            .ReturnsAsync(new MovimientoPaquete
            {
                Id = 10, NumeroExpedicion = "NXI-ULTMILLA-001",
                CtaOrigenId = 2, CtaDestinoId = 1
            });

        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-ULTMILLA-001",
            ModoOperacion = ModosEscaneo.Clasificacion,
            CtaId = 1,
            CtaCodigo = "CTA-BCN"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("AsignadoARuta");
    }

    [Fact]
    public async Task ProcesarEscaneo_Clasificacion_UltimaMillaOficina_RetornaPreparadoParaOficinaDestino()
    {
        _movimientoRepo
            .Setup(m => m.GetRecibidoByExpedicionAndCtaDestinoAsync("NXI-ULTMILLA-002", 1))
            .ReturnsAsync(new MovimientoPaquete
            {
                Id = 11, NumeroExpedicion = "NXI-ULTMILLA-002",
                CtaOrigenId = 2, CtaDestinoId = 1
            });
        _ciudadanoLookup
            .Setup(l => l.ObtenerAsync("NXI-ULTMILLA-002", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvioInternoServiceLookupDto { TipoEntrega = "Oficina", OficinaDestinoId = 50 });

        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-ULTMILLA-002",
            ModoOperacion = ModosEscaneo.Clasificacion,
            CtaId = 1,
            CtaCodigo = "CTA-BCN"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("PreparadoParaOficinaDestino");
    }

    // ═══════════════════════════════════════════
    //  DESPACHO TRONCAL
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_DespachoTroncal_SinCtaId_RetornaError()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000006",
            ModoOperacion = ModosEscaneo.DespachoTroncal
        });

        result.Exito.Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarEscaneo_DespachoTroncal_SinMovimientoProgramado_RetornaEnTransito()
    {
        // Sin movimiento programado (default mock retorna null)
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000006",
            ModoOperacion = ModosEscaneo.DespachoTroncal,
            CtaId = 1,
            CtaCodigo = "CTA-MAD"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("EnTransitoHaciaCentroDestino");
    }

    // ═══════════════════════════════════════════
    //  RECEPCION TRONCAL
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_RecepcionTroncal_SinCtaId_RetornaError()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000007",
            ModoOperacion = ModosEscaneo.RecepcionTroncal
        });

        result.Exito.Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarEscaneo_RecepcionTroncal_ConCtaId_SinMovimiento_RetornaRecibidoEnCentroDestino()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000007",
            ModoOperacion = ModosEscaneo.RecepcionTroncal,
            CtaId = 2,
            CtaCodigo = "CTA-BCN"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("RecibidoEnCentroDestino");
        result.NotificacionEnviada.Should().BeTrue();
    }

    // ═══════════════════════════════════════════
    //  DISPONIBLE PARA REPARTO
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_DisponibleParaReparto_SinCtaId_RetornaError()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000008",
            ModoOperacion = ModosEscaneo.DisponibleParaReparto
        });

        result.Exito.Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarEscaneo_DisponibleParaReparto_EnvioTipoOficina_Rechaza()
    {
        _ciudadanoLookup
            .Setup(l => l.ObtenerAsync("NXI-OFIDPRT-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvioInternoServiceLookupDto { TipoEntrega = "Oficina" });

        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-OFIDPRT-001",
            ModoOperacion = ModosEscaneo.DisponibleParaReparto,
            CtaId = 1,
            CtaCodigo = "CTA-MAD"
        });

        result.Exito.Should().BeFalse();
        result.Mensaje.Should().Contain("oficina");
    }

    [Fact]
    public async Task ProcesarEscaneo_DisponibleParaReparto_EntregaDomicilio_RetornaDisponibleParaReparto()
    {
        // Ciudadano retorna null → Domicilio (configurado en ctor)
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-DOM-001",
            ModoOperacion = ModosEscaneo.DisponibleParaReparto,
            CtaId = 1,
            CtaCodigo = "CTA-MAD"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("DisponibleParaReparto");
        result.NotificacionEnviada.Should().BeTrue();
        _notificacionService.Verify(
            n => n.NotificarPaqueteDisponibleParaReparto(1, "CTA-MAD", "NXI-DOM-001", false),
            Times.Once);
    }

    // ═══════════════════════════════════════════
    //  ENTREGA OFICINA DESTINO
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_EntregaOficinaDestino_SinOficinaJsonId_RetornaError()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000009",
            ModoOperacion = ModosEscaneo.EntregaOficinaDestino
        });

        result.Exito.Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarEscaneo_EntregaOficinaDestino_ModalidadDomicilio_RetornaDepositadoEnOficina()
    {
        // null → domicilio → "depositado" (paso intermedio)
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000009",
            ModoOperacion = ModosEscaneo.EntregaOficinaDestino,
            OficinaJsonId = 20,
            OficinaNombre = "NexoPostal BCN"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("DepositadoEnOficina");
        result.Mensaje.Should().NotContain("recogida");
    }

    [Fact]
    public async Task ProcesarEscaneo_EntregaOficinaDestino_ModalidadOficina_MensajeContieneRecogida()
    {
        _ciudadanoLookup
            .Setup(l => l.ObtenerAsync("NXI-OFIDST-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvioInternoServiceLookupDto { TipoEntrega = "Oficina", OficinaDestinoId = 20 });

        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-OFIDST-001",
            ModoOperacion = ModosEscaneo.EntregaOficinaDestino,
            OficinaJsonId = 20,
            OficinaNombre = "NexoPostal BCN"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("DepositadoEnOficina");
        result.Mensaje.Should().Contain("recogida");
    }

    // ═══════════════════════════════════════════
    //  SALIDA A REPARTO
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_SalidaAReparto_SinOficinaJsonId_RetornaError()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00000010",
            ModoOperacion = ModosEscaneo.SalidaAReparto
        });

        result.Exito.Should().BeFalse();
    }

    [Fact]
    public async Task ProcesarEscaneo_SalidaAReparto_EnvioTipoOficina_Rechaza()
    {
        _ciudadanoLookup
            .Setup(l => l.ObtenerAsync("NXI-OFIREP-001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EnvioInternoServiceLookupDto { TipoEntrega = "Oficina" });

        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-OFIREP-001",
            ModoOperacion = ModosEscaneo.SalidaAReparto,
            OficinaJsonId = 10,
            OficinaNombre = "NexoPostal MAD"
        });

        result.Exito.Should().BeFalse();
        result.Mensaje.Should().Contain("oficina");
    }

    [Fact]
    public async Task ProcesarEscaneo_SalidaAReparto_EntregaDomicilio_RetornaEnReparto()
    {
        var svc = BuildService();

        var result = await svc.ProcesarEscaneo(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-DOM-002",
            ModoOperacion = ModosEscaneo.SalidaAReparto,
            OficinaJsonId = 10,
            OficinaNombre = "NexoPostal MAD"
        });

        result.Exito.Should().BeTrue();
        result.EstadoNuevo.Should().Be("EnReparto");
    }

    // ═══════════════════════════════════════════
    //  PROCESADO EN LOTE
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarLote_TresCodigosCtaValidos_RetornaBatchCon3Exitosos()
    {
        var svc = BuildService();

        var batch = new ScanBatchRequestDto
        {
            ModoOperacion = ModosEscaneo.RecepcionCta,
            CtaId = 1,
            CtaCodigo = "CTA-MAD",
            CodigosEscaneados = ["NXI-BATCH-001", "NXI-BATCH-002", "NXI-BATCH-003"]
        };

        var result = await svc.ProcesarLote(batch);

        result.TotalEscaneados.Should().Be(3);
        result.Exitosos.Should().Be(3);
        result.Fallidos.Should().Be(0);
        result.Resultados.Should().HaveCount(3);
    }

    [Fact]
    public async Task ProcesarLote_UnCodigoVacioEntreValidos_ContabilizaUnFallido()
    {
        var svc = BuildService();

        var batch = new ScanBatchRequestDto
        {
            ModoOperacion = ModosEscaneo.RecepcionCta,
            CtaId = 1,
            CtaCodigo = "CTA-MAD",
            CodigosEscaneados = ["NXI-BATCH-001", "  ", "NXI-BATCH-003"]
        };

        var result = await svc.ProcesarLote(batch);

        result.TotalEscaneados.Should().Be(3);
        result.Exitosos.Should().Be(2);
        result.Fallidos.Should().Be(1);
    }
}
