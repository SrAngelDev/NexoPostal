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
/// Tests unitarios para AsignacionService.
/// </summary>
public class AsignacionServiceTests
{
    private readonly Mock<IAsignacionPaqueteRepository> _asignacionRepo = new();
    private readonly Mock<IOperarioCtaRepository> _operarioRepo = new();
    private readonly Mock<IOperarioOficinaRepository> _operarioOficinaRepo = new();
    private readonly Mock<INotificacionService> _notificacionService = new();

    private AsignacionService BuildService() => new AsignacionService(
        _asignacionRepo.Object,
        _operarioRepo.Object,
        _operarioOficinaRepo.Object,
        _notificacionService.Object,
        NullLogger<AsignacionService>.Instance);

    // ═══════════════════════════════════════════
    //  CrearAsignacion
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CrearAsignacion_TipoTareaInvalido_DeberiaLanzarArgumentException()
    {
        var service = BuildService();
        var dto = new CrearAsignacionDto
        {
            NumeroExpedicion = "EXP-001",
            OperarioAsignadoId = 1,
            TipoTarea = "TIPO_INVALIDO",
            EsUrgente = false
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CrearAsignacion(dto, 99, 1));
    }

    [Fact]
    public async Task CrearAsignacion_OperarioDeOtroCta_DeberiaLanzarInvalidOperationException()
    {
        // El operario asignado pertenece al CTA 2, pero se está creando en CTA 1
        _operarioRepo.Setup(r => r.GetByIdAsync(5))
            .ReturnsAsync(new OperarioCta { Id = 5, CentroTratamientoId = 2, NombreCompleto = "Operario Otro CTA" });

        var service = BuildService();
        var dto = new CrearAsignacionDto
        {
            NumeroExpedicion = "EXP-001",
            OperarioAsignadoId = 5,
            TipoTarea = "Recepcion",
            EsUrgente = false
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CrearAsignacion(dto, 99, 1));
    }

    [Fact]
    public async Task CrearAsignacion_DatosValidos_DeberiaCrearAsignacion()
    {
        var cta = new CentroTratamiento { Id = 1, Codigo = "CTA-MAD" };
        var operarioAsignado = new OperarioCta { Id = 5, CentroTratamientoId = 1, NombreCompleto = "Operario Test" };
        var logistico = new OperarioCta { Id = 99, CentroTratamientoId = 1, NombreCompleto = "Logístico Test", CentroTratamiento = cta };
        var asignacionCreada = new AsignacionPaquete { Id = 10, NumeroExpedicion = "EXP-001", OperarioAsignadoId = 5, CtaId = 1, TipoTarea = TipoTarea.Recepcion };

        _operarioRepo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(operarioAsignado);
        _asignacionRepo.Setup(r => r.CreateAsync(It.IsAny<AsignacionPaquete>()))
            .ReturnsAsync((AsignacionPaquete a) => { a.Id = 10; return a; });
        _operarioRepo.Setup(r => r.GetWithCtaAsync(99)).ReturnsAsync(logistico);
        _notificacionService.Setup(n => n.NotificarTareaAsignada(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _asignacionRepo.Setup(r => r.GetDetailAsync(10)).ReturnsAsync(asignacionCreada);

        var service = BuildService();
        var dto = new CrearAsignacionDto
        {
            NumeroExpedicion = "EXP-001",
            OperarioAsignadoId = 5,
            TipoTarea = "Recepcion",
            EsUrgente = false
        };

        var result = await service.CrearAsignacion(dto, 99, 1);

        result.Should().NotBeNull();
        _asignacionRepo.Verify(r => r.CreateAsync(It.Is<AsignacionPaquete>(a =>
            a.NumeroExpedicion == "EXP-001" && a.TipoTarea == TipoTarea.Recepcion)), Times.Once);
        _notificacionService.Verify(n => n.NotificarTareaAsignada(
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string>()), Times.Once);
    }

    // ═══════════════════════════════════════════
    //  ObtenerTareasPendientes
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerTareasPendientes_DeberiaRetornarListaCorrecta()
    {
        var asignaciones = new List<AsignacionPaquete>
        {
            new() { Id = 1, NumeroExpedicion = "EXP-001", EstadoTarea = EstadoTarea.Pendiente },
            new() { Id = 2, NumeroExpedicion = "EXP-002", EstadoTarea = EstadoTarea.Pendiente }
        };
        _asignacionRepo.Setup(r => r.GetByOperarioAsync(5, EstadoTarea.Pendiente)).ReturnsAsync(asignaciones);

        var service = BuildService();
        var result = await service.ObtenerTareasPendientes(5);

        result.Should().HaveCount(2);
    }

    // ═══════════════════════════════════════════
    //  IniciarTarea
    // ═══════════════════════════════════════════

    [Fact]
    public async Task IniciarTarea_TareaNoExiste_DeberiaRetornarNull()
    {
        _asignacionRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((AsignacionPaquete?)null);

        var service = BuildService();
        var result = await service.IniciarTarea(999, 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task IniciarTarea_TareaDeOtroOperario_DeberiaLanzarInvalidOperationException()
    {
        _asignacionRepo.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new AsignacionPaquete { Id = 10, OperarioAsignadoId = 5, EstadoTarea = EstadoTarea.Pendiente });

        var service = BuildService();

        // Operario 7 intenta iniciar tarea asignada a operario 5
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.IniciarTarea(10, 7));
    }

    [Fact]
    public async Task IniciarTarea_EstadoNoEsPendiente_DeberiaLanzarInvalidOperationException()
    {
        _asignacionRepo.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new AsignacionPaquete { Id = 10, OperarioAsignadoId = 5, EstadoTarea = EstadoTarea.Completada });

        var service = BuildService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.IniciarTarea(10, 5));
    }

    // ═══════════════════════════════════════════
    //  CompletarTarea
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CompletarTarea_EstadoNoEsEnProgreso_DeberiaLanzarInvalidOperationException()
    {
        _asignacionRepo.Setup(r => r.GetByIdAsync(10))
            .ReturnsAsync(new AsignacionPaquete { Id = 10, OperarioAsignadoId = 5, EstadoTarea = EstadoTarea.Pendiente });

        var service = BuildService();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompletarTarea(10, 5));
    }
}
