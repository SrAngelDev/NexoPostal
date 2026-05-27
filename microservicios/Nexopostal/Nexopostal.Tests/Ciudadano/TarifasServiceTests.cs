using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Nexopostal.Ciudadano.Services;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

public class TarifasServiceTests
{
    private static TarifasService Create()
    {
        // ScopeFactory que devuelve un scope cuyo provider no contiene CiudadanoDbContext
        // → EnsureLoaded captura la excepción y cae al fallback de defaults.
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var svc = new TarifasService(scopeFactory, NullLogger<TarifasService>.Instance);
        svc.Invalidar();
        return svc;
    }

    // ---------- ParseDimensiones ----------

    [Theory]
    [InlineData("30x20x15", 30.0, 20.0, 15.0)]
    [InlineData("30,5 x 20 x 15", 30.5, 20.0, 15.0)]
    [InlineData("30 20 15", 30.0, 20.0, 15.0)]
    public void ParseDimensiones_ConFormatoValido_DevuelveValores(string raw, double l, double a, double h)
    {
        var svc = Create();
        var (largo, ancho, alto) = svc.ParseDimensiones(raw);
        largo.Should().Be((decimal)l);
        ancho.Should().Be((decimal)a);
        alto.Should().Be((decimal)h);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("solo 10 numeros")]
    public void ParseDimensiones_VacioOInsuficiente_DevuelveNulls(string? raw)
    {
        var svc = Create();
        var (largo, ancho, alto) = svc.ParseDimensiones(raw);
        largo.Should().BeNull();
        ancho.Should().BeNull();
        alto.Should().BeNull();
    }

    // ---------- ResolverZona (indirecto vía Consultar) ----------

    [Theory]
    [InlineData("28001", "28045", "Local")]      // mismo prefijo 28
    [InlineData("28001", "08001", "Península")]   // distintos prefijos peninsulares
    [InlineData("28001", "07001", "Baleares")]    // Baleares
    [InlineData("28001", "35001", "Canarias")]    // Canarias (35)
    [InlineData("28001", "38001", "Canarias")]    // Canarias (38)
    [InlineData("28001", "51001", "Ceuta/Melilla")] // Ceuta
    [InlineData("28001", "52001", "Ceuta/Melilla")] // Melilla
    [InlineData("07001", "07020", "Baleares")]    // misma zona Baleares (no Local pq tiene prioridad)
    [InlineData(null, null, "Península")]          // CPs vacíos → Península
    public void Consultar_DetectaZonaCorrecta(string? origen, string? destino, string esperada)
    {
        var svc = Create();
        var resultado = svc.Consultar(new TarifaConsultaInput(1m, null, null, null, origen, destino));
        resultado.Zona.Should().Be(esperada);
    }

    // ---------- Peso volumétrico y recargos ----------

    [Fact]
    public void Consultar_SinDimensiones_NoAplicaRecargo()
    {
        var svc = Create();
        var r = svc.Consultar(new TarifaConsultaInput(2m, null, null, null, "28001", "28045"));
        r.AplicaRecargo.Should().BeFalse();
        r.PesoVolumetrico.Should().Be(0m);
        r.PesoFacturable.Should().Be(2m);
    }

    [Fact]
    public void Consultar_ConDimensionesPequenas_NoRecargo()
    {
        var svc = Create();
        var r = svc.Consultar(new TarifaConsultaInput(1m, 30m, 20m, 15m, "28001", "28045"));
        r.AplicaRecargo.Should().BeFalse();
        // volumen 30*20*15=9000 / 6000 = 1.5 kg volumétrico → mayor que real
        r.PesoVolumetrico.Should().Be(1.5m);
        r.PesoFacturable.Should().Be(1.5m);
    }

    [Fact]
    public void Consultar_SumaDimensionesMayorA210_AplicaRecargo()
    {
        var svc = Create();
        var r = svc.Consultar(new TarifaConsultaInput(1m, 80m, 70m, 65m, "28001", "28045"));
        r.AplicaRecargo.Should().BeTrue();
        r.RecargoPorcentaje.Should().Be(0.35m);
    }

    [Fact]
    public void Consultar_LadoMayorA170_AplicaRecargo()
    {
        var svc = Create();
        var r = svc.Consultar(new TarifaConsultaInput(1m, 180m, 20m, 5m, "28001", "28045"));
        r.AplicaRecargo.Should().BeTrue();
    }

    [Fact]
    public void Consultar_PesoCero_SeNormalizaA01()
    {
        var svc = Create();
        var r = svc.Consultar(new TarifaConsultaInput(0m, null, null, null, "28001", "28045"));
        r.PesoReal.Should().Be(0.1m);
    }

    // ---------- Calcular ----------

    [Theory]
    [InlineData("estandar", "Estandar")]
    [InlineData("Premium", "Premium")]
    [InlineData("PREMIUM", "Premium")]
    [InlineData("foo", "Estandar")]
    [InlineData(null, "Estandar")]
    [InlineData("", "Estandar")]
    public void Calcular_NormalizaTipoTarifa(string? input, string esperado)
    {
        var svc = Create();
        var r = svc.Calcular(new TarifaCalculoInput(1m, null, null, null, "28001", "28045", input));
        r.TipoTarifa.Should().Be(esperado);
    }

    [Fact]
    public void Calcular_LocalEstandar_RetornaPrecioYIvaCorrectos()
    {
        var svc = Create();
        var r = svc.Calcular(new TarifaCalculoInput(0.5m, null, null, null, "28001", "28045", "Estandar"));
        r.Zona.Should().Be("Local");
        r.PrecioBase.Should().BeGreaterThan(0m);
        r.Iva.Should().BeGreaterThan(0m);
        r.PrecioTotal.Should().Be(r.PrecioBase + r.Recargo + r.Iva);
        r.Recargo.Should().Be(0m); // Sin dimensiones → sin recargo
        r.TiempoEntregaEstimado.Should().Be("24-48h");
        r.TiempoEstimadoDias.Should().Be(2);
    }

    [Fact]
    public void Calcular_LocalPremium_TiempoEsMasRapidoQueEstandar()
    {
        var svc = Create();
        var estandar = svc.Calcular(new TarifaCalculoInput(1m, null, null, null, "28001", "28045", "Estandar"));
        var premium = svc.Calcular(new TarifaCalculoInput(1m, null, null, null, "28001", "28045", "Premium"));
        premium.TiempoEstimadoDias.Should().BeLessThan(estandar.TiempoEstimadoDias);
        premium.PrecioBase.Should().BeGreaterThan(estandar.PrecioBase);
    }

    [Fact]
    public void Calcular_PeninsulaVsLocal_LocalEsMasBarata()
    {
        var svc = Create();
        var local = svc.Calcular(new TarifaCalculoInput(1m, null, null, null, "28001", "28045", "Estandar"));
        var peninsula = svc.Calcular(new TarifaCalculoInput(1m, null, null, null, "28001", "08001", "Estandar"));
        local.PrecioBase.Should().BeLessThan(peninsula.PrecioBase);
    }

    [Fact]
    public void Calcular_CanariasMayorMultiplicador_QueBaleares()
    {
        var svc = Create();
        var canarias = svc.Calcular(new TarifaCalculoInput(1m, null, null, null, "28001", "35001", "Estandar"));
        var baleares = svc.Calcular(new TarifaCalculoInput(1m, null, null, null, "28001", "07001", "Estandar"));
        canarias.PrecioBase.Should().BeGreaterThan(baleares.PrecioBase);
        canarias.TiempoEstimadoDias.Should().BeGreaterThan(baleares.TiempoEstimadoDias);
    }

    [Fact]
    public void Calcular_ConRecargoDimensiones_AplicaRecargoEnTotal()
    {
        var svc = Create();
        var r = svc.Calcular(new TarifaCalculoInput(1m, 90m, 80m, 50m, "28001", "28045", "Estandar"));
        r.AplicaRecargo.Should().BeTrue();
        r.Recargo.Should().BeGreaterThan(0m);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1.5)]
    [InlineData(3.0)]
    [InlineData(7.0)]
    [InlineData(15.0)]
    [InlineData(25.0)]
    [InlineData(35.0)]  // > 30 → última banda
    public void Calcular_DiferentesBandasPeso_DevuelvePrecioPositivo(double peso)
    {
        var svc = Create();
        var r = svc.Calcular(new TarifaCalculoInput((decimal)peso, null, null, null, "28001", "08001", "Estandar"));
        r.PrecioBase.Should().BeGreaterThan(0m);
    }

    [Fact]
    public void Consultar_DevuelveDosOpciones()
    {
        var svc = Create();
        var r = svc.Consultar(new TarifaConsultaInput(1m, null, null, null, "28001", "08001"));
        r.Tarifas.Should().HaveCount(2);
        r.Tarifas.Should().Contain(t => t.Nombre == "Estandar");
        r.Tarifas.Should().Contain(t => t.Nombre == "Premium");
    }

    [Fact]
    public void Invalidar_LimpiaCacheSinError()
    {
        var svc = Create();
        var r1 = svc.Calcular(new TarifaCalculoInput(1m, null, null, null, "28001", "28045", "Estandar"));
        svc.Invalidar();
        var r2 = svc.Calcular(new TarifaCalculoInput(1m, null, null, null, "28001", "28045", "Estandar"));
        r2.PrecioTotal.Should().Be(r1.PrecioTotal);
    }
}
