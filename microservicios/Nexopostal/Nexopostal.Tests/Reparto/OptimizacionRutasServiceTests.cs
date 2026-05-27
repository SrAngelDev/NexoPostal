using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;
using Xunit;

namespace Nexopostal.Tests.Reparto;

public class OptimizacionRutasServiceTests
{
    private static OptimizacionRutasService Create()
    {
        var repo = new Mock<IRepartidorRepository>();
        return new OptimizacionRutasService(repo.Object, NullLogger<OptimizacionRutasService>.Instance);
    }

    [Fact]
    public async Task GenerarRutaOptima_SinEntregas_DevuelveRutaVacia()
    {
        var svc = Create();
        var ruta = await svc.GenerarRutaOptima(1, new List<EntregaParaOptimizar>());
        ruta.RepartidorId.Should().Be(1);
        ruta.EntregasOrdenadas.Should().BeEmpty();
        ruta.DistanciaTotalKm.Should().Be(0);
        ruta.TiempoEstimadoMinutos.Should().Be(0);
    }

    [Fact]
    public async Task GenerarRutaOptima_UnaEntrega_DevuelveEsaEntrega()
    {
        var svc = Create();
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { EntregaId = 1, Latitud = 40.4168, Longitud = -3.7038, EsUrgente = false }
        };
        var ruta = await svc.GenerarRutaOptima(7, entregas);
        ruta.EntregasOrdenadas.Should().HaveCount(1);
        ruta.EntregasOrdenadas[0].OrdenActual.Should().Be(1);
    }

    [Fact]
    public async Task GenerarRutaOptima_PrioizaUrgentesSobreNormales()
    {
        var svc = Create();
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { EntregaId = 1, Latitud = 40.40, Longitud = -3.70, EsUrgente = false, NumeroExpedicion = "N1" },
            new() { EntregaId = 2, Latitud = 40.50, Longitud = -3.80, EsUrgente = true,  NumeroExpedicion = "U1" },
            new() { EntregaId = 3, Latitud = 40.45, Longitud = -3.75, EsUrgente = false, NumeroExpedicion = "N2" },
            new() { EntregaId = 4, Latitud = 40.42, Longitud = -3.72, EsUrgente = true,  NumeroExpedicion = "U2" }
        };
        var ruta = await svc.GenerarRutaOptima(1, entregas);
        ruta.EntregasOrdenadas.Should().HaveCount(4);
        // Las dos primeras deben ser urgentes
        ruta.EntregasOrdenadas[0].EsUrgente.Should().BeTrue();
        ruta.EntregasOrdenadas[1].EsUrgente.Should().BeTrue();
        ruta.EntregasOrdenadas[2].EsUrgente.Should().BeFalse();
        ruta.EntregasOrdenadas[3].EsUrgente.Should().BeFalse();
        // OrdenActual debe asignarse 1..N
        for (int i = 0; i < 4; i++)
            ruta.EntregasOrdenadas[i].OrdenActual.Should().Be(i + 1);
    }

    [Fact]
    public async Task GenerarRutaOptima_TiempoYDistanciaPositivos()
    {
        var svc = Create();
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { EntregaId = 1, Latitud = 40.40, Longitud = -3.70 },
            new() { EntregaId = 2, Latitud = 40.50, Longitud = -3.80 },
            new() { EntregaId = 3, Latitud = 40.45, Longitud = -3.75 }
        };
        var ruta = await svc.GenerarRutaOptima(1, entregas);
        ruta.DistanciaTotalKm.Should().BeGreaterThan(0);
        ruta.TiempoEstimadoMinutos.Should().BeGreaterThan(0);
        ruta.Algoritmo.Should().Be("NearestNeighbor");
    }

    [Fact]
    public async Task CalcularDistanciaTotal_ConMenosDe2_DevuelveCero()
    {
        var svc = Create();
        (await svc.CalcularDistanciaTotal(new List<EntregaParaOptimizar>())).Should().Be(0);
        (await svc.CalcularDistanciaTotal(new List<EntregaParaOptimizar>
        {
            new() { Latitud = 40.40, Longitud = -3.70 }
        })).Should().Be(0);
    }

    [Fact]
    public async Task CalcularDistanciaTotal_DosPuntosConocidos_AproximadaCorrecta()
    {
        var svc = Create();
        // Madrid (40.4168, -3.7038) a Barcelona (41.3851, 2.1734) ~ 504 km
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { Latitud = 40.4168, Longitud = -3.7038 },
            new() { Latitud = 41.3851, Longitud = 2.1734 }
        };
        var d = await svc.CalcularDistanciaTotal(entregas);
        d.Should().BeInRange(490, 520);
    }

    [Fact]
    public async Task ReordenarEntregas_DesdeOrigen_OrdenaPorCercania()
    {
        var svc = Create();
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { EntregaId = 1, Latitud = 41.0, Longitud = 0.0 },  // lejos
            new() { EntregaId = 2, Latitud = 40.5, Longitud = 0.0 },  // medio
            new() { EntregaId = 3, Latitud = 40.1, Longitud = 0.0 }   // cerca
        };
        var resultado = await svc.ReordenarEntregas(entregas, 40.0, 0.0);
        resultado[0].EntregaId.Should().Be(3); // la más cercana
        resultado[2].EntregaId.Should().Be(1); // la más lejana
    }

    [Fact]
    public async Task ReordenarEntregas_ListaConUnElemento_NoSeReordena()
    {
        var svc = Create();
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { EntregaId = 99 }
        };
        var resultado = await svc.ReordenarEntregas(entregas, 0, 0);
        resultado.Should().HaveCount(1);
        resultado[0].EntregaId.Should().Be(99);
    }

    [Fact]
    public async Task ReordenarEntregas_UrgentesPrimero()
    {
        var svc = Create();
        var entregas = new List<EntregaParaOptimizar>
        {
            new() { EntregaId = 1, Latitud = 40.1, Longitud = 0.0, EsUrgente = false },
            new() { EntregaId = 2, Latitud = 50.0, Longitud = 0.0, EsUrgente = true  }
        };
        var resultado = await svc.ReordenarEntregas(entregas, 40.0, 0.0);
        resultado[0].EntregaId.Should().Be(2); // urgente, aunque más lejos
    }
}
