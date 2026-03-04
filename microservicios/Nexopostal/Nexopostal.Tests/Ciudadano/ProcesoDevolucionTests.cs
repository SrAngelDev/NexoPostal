using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Nexopostal.Ciudadano.Services;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

/// <summary>
/// Tests unitarios para el servicio de proceso de devoluciones
/// </summary>
public class ProcesoDevolucionTests
{
    private readonly Mock<IEnvioRepository> _envioRepoMock;
    private readonly IProcesoDevolucionService _service;

    public ProcesoDevolucionTests()
    {
        _envioRepoMock = new Mock<IEnvioRepository>();
        var logger = new Mock<ILogger<ProcesoDevolucionService>>();
        _service = new ProcesoDevolucionService(_envioRepoMock.Object, logger.Object);
    }

    [Fact]
    public async Task CalcularReembolso_DeberiaRetornar80PorcientoDelCoste()
    {
        // Arrange
        var envio = new Envio
        {
            NumeroSeguimiento = "NX123TEST",
            CosteCalculado = 10.00m
        };
        _envioRepoMock.Setup(r => r.GetByTrackingAsync("NX123TEST"))
            .ReturnsAsync(envio);

        // Act
        var reembolso = await _service.CalcularReembolso("NX123TEST");

        // Assert
        reembolso.Should().Be(8.00m); // 80% de 10€
    }

    [Fact]
    public async Task CalcularReembolso_ConCosteMinimo_DeberiaDescontarMinimo2Euros()
    {
        // Arrange
        var envio = new Envio
        {
            NumeroSeguimiento = "NX456TEST",
            CosteCalculado = 3.00m
        };
        _envioRepoMock.Setup(r => r.GetByTrackingAsync("NX456TEST"))
            .ReturnsAsync(envio);

        // Act
        var reembolso = await _service.CalcularReembolso("NX456TEST");

        // Assert
        reembolso.Should().Be(1.00m); // 3€ - 2€ comisión mínima = 1€
    }

    [Fact]
    public async Task IniciarDevolucion_ConEnvioInexistente_DeberiaRetornarFalse()
    {
        // Arrange
        _envioRepoMock.Setup(r => r.GetByTrackingAsync("NOEXISTE"))
            .ReturnsAsync((Envio?)null);

        // Act
        var resultado = await _service.IniciarDevolucion("NOEXISTE", "Motivo test");

        // Assert
        resultado.Should().BeFalse();
    }

    [Fact]
    public async Task IniciarDevolucion_ConEnvioValido_DeberiaRetornarTrue()
    {
        // Arrange
        var envio = new Envio
        {
            NumeroSeguimiento = "NX789TEST",
            EstadoActual = EstadoEnvio.EnReparto,
            EstadoInternoActual = EstadoInterno.EnReparto
        };
        _envioRepoMock.Setup(r => r.GetByTrackingAsync("NX789TEST"))
            .ReturnsAsync(envio);

        // Act
        var resultado = await _service.IniciarDevolucion("NX789TEST", "Cliente solicita devolución");

        // Assert
        resultado.Should().BeTrue();
        _envioRepoMock.Verify(r => r.UpdateAsync(It.IsAny<Envio>()), Times.Once);
    }
}
