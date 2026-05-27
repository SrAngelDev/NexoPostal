using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;
using Xunit;

namespace Nexopostal.Tests.Reparto;

public class ReintentoEntregaServiceTests
{
    private readonly Mock<IEntregaPaqueteRepository> _entregaRepo = new();
    private readonly Mock<IRutaRepartoRepository> _rutaRepo = new();

    private ReintentoEntregaService Create() =>
        new(_entregaRepo.Object, _rutaRepo.Object, NullLogger<ReintentoEntregaService>.Instance);

    private static EntregaPaquete Entrega(int id, EstadoEntrega estado, int intento = 1, int diasAtras = 0)
        => new()
        {
            Id = id,
            RutaRepartoId = 1,
            NumeroExpedicion = "EXP-1",
            NumeroSeguimiento = "NP-1",
            DireccionEntrega = "Calle 1",
            CodigoPostal = "28001",
            Ciudad = "Madrid",
            NombreDestinatario = "Test",
            Estado = estado,
            NumeroIntento = intento,
            FechaCreacion = DateTime.UtcNow.AddDays(-diasAtras)
        };

    [Fact]
    public async Task DeterminarAccion_EntregaNoExiste_DevuelveDevolver()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((EntregaPaquete?)null);
        (await Create().DeterminarAccion(99)).Should().Be("Devolver");
    }

    [Fact]
    public async Task DeterminarAccion_MasDe5Dias_DevuelveDevolver()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Entrega(1, EstadoEntrega.Ausente, 1, diasAtras: 7));
        (await Create().DeterminarAccion(1)).Should().Be("Devolver");
    }

    [Fact]
    public async Task DeterminarAccion_Intento1_DevuelveReintentar()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Entrega(1, EstadoEntrega.Ausente, 1, diasAtras: 0));
        (await Create().DeterminarAccion(1)).Should().Be("Reintentar");
    }

    [Fact]
    public async Task DeterminarAccion_Intento2_DevuelveDepositarOficina()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Entrega(1, EstadoEntrega.Ausente, 2, diasAtras: 1));
        (await Create().DeterminarAccion(1)).Should().Be("DepositarOficina");
    }

    [Fact]
    public async Task DeterminarAccion_Intento3_DevuelveDevolver()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Entrega(1, EstadoEntrega.Ausente, 3, diasAtras: 1));
        (await Create().DeterminarAccion(1)).Should().Be("Devolver");
    }

    [Fact]
    public async Task ProgramarReintento_NoExiste_DevuelveFalse()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((EntregaPaquete?)null);
        (await Create().ProgramarReintento(1, "m")).Should().BeFalse();
    }

    [Theory]
    [InlineData(EstadoEntrega.Entregado)]
    [InlineData(EstadoEntrega.Pendiente)]
    [InlineData(EstadoEntrega.EnCamino)]
    public async Task ProgramarReintento_EstadoNoFallido_DevuelveFalse(EstadoEntrega estado)
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Entrega(1, estado));
        (await Create().ProgramarReintento(1, "m")).Should().BeFalse();
    }

    [Fact]
    public async Task ProgramarReintento_Intento1Ausente_CreaNuevoIntento()
    {
        var origen = Entrega(1, EstadoEntrega.Ausente, intento: 1, diasAtras: 0);
        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(origen);
        EntregaPaquete? creado = null;
        _entregaRepo.Setup(r => r.CreateAsync(It.IsAny<EntregaPaquete>()))
                    .Callback<EntregaPaquete>(e => creado = e)
                    .ReturnsAsync((EntregaPaquete e) => e);

        var ok = await Create().ProgramarReintento(1, "Ausente");
        ok.Should().BeTrue();
        creado.Should().NotBeNull();
        creado!.NumeroIntento.Should().Be(2);
        creado.Estado.Should().Be(EstadoEntrega.Pendiente);
        creado.Observaciones.Should().Contain("Ausente");
    }

    [Fact]
    public async Task ProgramarReintento_Intento2_NoReintenta()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Entrega(1, EstadoEntrega.Ausente, intento: 2, diasAtras: 0));
        (await Create().ProgramarReintento(1, "m")).Should().BeFalse();
        _entregaRepo.Verify(r => r.CreateAsync(It.IsAny<EntregaPaquete>()), Times.Never);
    }

    [Fact]
    public async Task CancelarReintentos_NoExiste_DevuelveFalse()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((EntregaPaquete?)null);
        (await Create().CancelarReintentos(99)).Should().BeFalse();
    }

    [Fact]
    public async Task CancelarReintentos_NoPendiente_DevuelveFalse()
    {
        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(Entrega(1, EstadoEntrega.Entregado));
        (await Create().CancelarReintentos(1)).Should().BeFalse();
    }

    [Fact]
    public async Task CancelarReintentos_Pendiente_MarcaDevueltoYActualiza()
    {
        var entrega = Entrega(1, EstadoEntrega.Pendiente);
        _entregaRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entrega);
        (await Create().CancelarReintentos(1)).Should().BeTrue();
        entrega.Estado.Should().Be(EstadoEntrega.DevueltoAOficina);
        entrega.Observaciones.Should().Contain("cancelado");
        _entregaRepo.Verify(r => r.UpdateAsync(entrega), Times.Once);
    }

    [Fact]
    public async Task ObtenerEntregasParaReintento_SinRutasHoy_DevuelveListaVacia()
    {
        _rutaRepo.Setup(r => r.GetByFechaAsync(It.IsAny<DateOnly>(), null)).ReturnsAsync(new List<RutaReparto>());
        var resultado = await Create().ObtenerEntregasParaReintento();
        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task ObtenerEntregasParaReintento_FiltraSoloEstadosFallidosYIntentoBajo()
    {
        var rutas = new List<RutaReparto> { new() { Id = 10 } };
        _rutaRepo.Setup(r => r.GetByFechaAsync(It.IsAny<DateOnly>(), null)).ReturnsAsync(rutas);

        var entregas = new List<EntregaPaquete>
        {
            Entrega(1, EstadoEntrega.Ausente, intento: 1),
            Entrega(2, EstadoEntrega.DireccionIncorrecta, intento: 2),
            Entrega(3, EstadoEntrega.Rechazado, intento: 3),
            Entrega(4, EstadoEntrega.Entregado, intento: 1),
            Entrega(5, EstadoEntrega.Pendiente, intento: 1)
        };
        _entregaRepo.Setup(r => r.GetByRutaIdsAsync(It.IsAny<List<int>>())).ReturnsAsync(entregas);

        var resultado = await Create().ObtenerEntregasParaReintento();
        resultado.Should().HaveCount(2);
        resultado.Select(e => e.Id).Should().BeEquivalentTo(new[] { 1, 2 });
    }
}
