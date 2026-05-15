using FluentAssertions;
using Nexopostal.Ciudadano.Services;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

/// <summary>
/// Tests unitarios para el cálculo de precios y tarifas
/// </summary>
public class PreciosTests
{
    [Fact]
    public void Calcular_DeberiaUsarPesoVolumetricoSiEsMayor()
    {
        var service = new TarifasService();

        var resultado = service.Calcular(new TarifaCalculoInput(
            5m,
            60m,
            40m,
            40m,
            "28001",
            "28080",
            "Estandar"));

        resultado.PesoFacturable.Should().Be(16m);
        resultado.PrecioBase.Should().Be(18.95m);
    }

    [Fact]
    public void Calcular_DeberiaAplicarRecargoCuandoSumaSupera210()
    {
        var service = new TarifasService();

        var resultado = service.Calcular(new TarifaCalculoInput(
            1m,
            100m,
            80m,
            40m,
            "28001",
            "28080",
            "Estandar"));

        resultado.AplicaRecargo.Should().BeTrue();
        resultado.Recargo.Should().Be(2.08m);
        resultado.PrecioTotal.Should().Be(8.03m);
    }

    [Fact]
    public void Consultar_DeberiaAplicarMultiplicadorZona()
    {
        var service = new TarifasService();

        var resultado = service.Consultar(new TarifaConsultaInput(
            1m,
            null,
            null,
            null,
            "28001",
            "35000"));

        resultado.Zona.Should().Be("Canarias");
        resultado.Tarifas[0].PrecioTotal.Should().Be(8.63m);
    }

    [Fact]
    public void Consultar_DeberiaIncluirPremium()
    {
        var service = new TarifasService();

        var resultado = service.Consultar(new TarifaConsultaInput(
            1m,
            null,
            null,
            null,
            "28001",
            "28080"));

        resultado.Tarifas.Should().Contain(t => t.Nombre == "Premium");
        resultado.Tarifas.Should().Contain(t => t.Nombre == "Estandar");
    }
}
