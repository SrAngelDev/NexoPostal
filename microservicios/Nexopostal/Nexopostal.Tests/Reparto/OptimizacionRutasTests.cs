using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;
using Xunit;

namespace Nexopostal.Tests.Reparto;

/// <summary>
/// Tests unitarios para el servicio de optimización de rutas
/// </summary>
public class OptimizacionRutasTests
{
    private readonly IOptimizacionRutasService _service;

    public OptimizacionRutasTests()
    {
        var repartidorRepo = new Mock<IRepartidorRepository>();
        var logger = new Mock<ILogger<OptimizacionRutasService>>();
        _service = new OptimizacionRutasService(repartidorRepo.Object, logger.Object);
    }

    [Fact]
    public async Task GenerarRutaOptima_ConEntregas_DeberiaRetornarRutaOrdenada()
    {
        // Arrange
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { EntregaId = 1, Latitud = 40.4168, Longitud = -3.7038, DireccionEntrega = "Madrid Centro" },
            new() { EntregaId = 2, Latitud = 40.4530, Longitud = -3.6883, DireccionEntrega = "Chamartín" },
            new() { EntregaId = 3, Latitud = 40.4000, Longitud = -3.7100, DireccionEntrega = "Embajadores" }
        };

        // Act
        var resultado = await _service.GenerarRutaOptima(1, entregas);

        // Assert
        resultado.Should().NotBeNull();
        resultado.EntregasOrdenadas.Should().HaveCount(3);
        resultado.DistanciaTotalKm.Should().BeGreaterThan(0);
        resultado.TiempoEstimadoMinutos.Should().BeGreaterThan(0);
        resultado.Algoritmo.Should().Be("NearestNeighbor");
    }

    [Fact]
    public async Task GenerarRutaOptima_ConUrgentes_DeberiaPonerlosAlPrincipio()
    {
        // Arrange
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { EntregaId = 1, Latitud = 40.4168, Longitud = -3.7038, EsUrgente = false },
            new() { EntregaId = 2, Latitud = 40.4530, Longitud = -3.6883, EsUrgente = true },
            new() { EntregaId = 3, Latitud = 40.4000, Longitud = -3.7100, EsUrgente = false }
        };

        // Act
        var resultado = await _service.GenerarRutaOptima(1, entregas);

        // Assert
        resultado.EntregasOrdenadas.First().EsUrgente.Should().BeTrue();
    }

    [Fact]
    public async Task CalcularDistanciaTotal_EntreDosPuntos_DeberiaSerPositiva()
    {
        // Arrange: Madrid - Barcelona ~500km
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { EntregaId = 1, Latitud = 40.4168, Longitud = -3.7038 },
            new() { EntregaId = 2, Latitud = 41.3851, Longitud = 2.1734 }
        };

        // Act
        var distancia = await _service.CalcularDistanciaTotal(entregas);

        // Assert
        distancia.Should().BeInRange(400, 700); // ~500km approx with Haversine
    }

    [Fact]
    public async Task GenerarRutaOptima_SinEntregas_DeberiaRetornarVacio()
    {
        // Act
        var resultado = await _service.GenerarRutaOptima(1, new List<EntregaParaOptimizar>());

        // Assert
        resultado.EntregasOrdenadas.Should().BeEmpty();
        resultado.DistanciaTotalKm.Should().Be(0);
    }
}
