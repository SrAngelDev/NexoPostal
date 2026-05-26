using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
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
        var service = new TarifasService(null!, NullLogger<TarifasService>.Instance);

        var resultado = service.Calcular(new TarifaCalculoInput(
            5m,
            60m,
            40m,
            40m,
            "28001",
            "08001", // Origen Madrid, Destino Barcelona -> Península
            "Estandar"));

        resultado.PesoFacturable.Should().Be(16m);
        resultado.PrecioBase.Should().Be(18.95m);
    }

    [Fact]
    public void Calcular_DeberiaAplicarRecargoCuandoSumaSupera210()
    {
        var service = new TarifasService(null!, NullLogger<TarifasService>.Instance);

        var resultado = service.Calcular(new TarifaCalculoInput(
            1m,
            202m, // Lado mayor / suma dimensiones > 210
            5m,
            5m,
            "28001",
            "08001", // Origen Madrid, Destino Barcelona -> Península
            "Estandar"));

        resultado.AplicaRecargo.Should().BeTrue();
        resultado.Recargo.Should().Be(2.08m); // 35% de 5.95 (1kg band) = 2.08
        resultado.PrecioTotal.Should().Be(9.72m); // Subtotal 8.03 + 21% IVA (1.69) = 9.72
    }

    [Fact]
    public void Consultar_DeberiaAplicarMultiplicadorZona()
    {
        var service = new TarifasService(null!, NullLogger<TarifasService>.Instance);

        var resultado = service.Consultar(new TarifaConsultaInput(
            1m,
            null,
            null,
            null,
            "28001",
            "35000"));

        resultado.Zona.Should().Be("Canarias");
        resultado.Tarifas[0].PrecioBase.Should().Be(8.63m); // 5.95 * 1.45 multiplier = 8.63
        resultado.Tarifas[0].PrecioTotal.Should().Be(10.44m); // 8.63 + 21% IVA = 10.44
    }

    [Fact]
    public void Consultar_DeberiaIncluirPremium()
    {
        var service = new TarifasService(null!, NullLogger<TarifasService>.Instance);

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
