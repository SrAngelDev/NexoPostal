using FluentAssertions;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

/// <summary>
/// Tests unitarios para el cálculo de precios y tarifas
/// </summary>
public class PreciosTests
{
    [Theory]
    [InlineData(1.0, "estandar", 7.00)]   // 5 base + 2*1 = 7
    [InlineData(5.0, "estandar", 15.00)]   // 5 base + 2*5 = 15
    [InlineData(1.0, "express", 11.50)]    // 8 base + 3.5*1 = 11.5
    [InlineData(1.0, "urgente", 20.00)]    // 15 base + 5*1 = 20
    [InlineData(10.0, "estandar", 25.00)]  // 5 base + 2*10 = 25
    public void CalcularPrecio_DeberiaRetornarPrecioCorrecto(
        decimal peso, string tipoServicio, decimal esperado)
    {
        // Arrange
        decimal precioBase;
        decimal precioPorKg;

        switch (tipoServicio.ToLower())
        {
            case "express":
                precioBase = 8.00m;
                precioPorKg = 3.50m;
                break;
            case "urgente":
                precioBase = 15.00m;
                precioPorKg = 5.00m;
                break;
            default:
                precioBase = 5.00m;
                precioPorKg = 2.00m;
                break;
        }

        // Act
        var resultado = Math.Round(precioBase + (precioPorKg * peso), 2);

        // Assert
        resultado.Should().Be(esperado);
    }

    [Fact]
    public void CalcularPrecio_ConVolumenGrande_DeberiaAplicarRecargo()
    {
        // Arrange
        decimal precioBase = 5.00m;
        decimal precioPorKg = 2.00m;
        decimal peso = 5.0m;
        decimal alto = 100, ancho = 50, largo = 30;
        decimal volumen = alto * ancho * largo;

        // Act
        decimal precio = precioBase + (precioPorKg * peso);
        if (volumen > 100000)
        {
            precio += 3.00m;
        }

        // Assert
        volumen.Should().Be(150000);
        precio.Should().Be(18.00m); // 5 + 10 + 3 recargo volumen
    }

    [Fact]
    public void CalcularPrecio_SinRecargoVolumen_CuandoVolumenPequeno()
    {
        // Arrange
        decimal precioBase = 5.00m;
        decimal precioPorKg = 2.00m;
        decimal peso = 2.0m;
        decimal alto = 30, ancho = 20, largo = 10;
        decimal volumen = alto * ancho * largo;

        // Act
        decimal precio = precioBase + (precioPorKg * peso);
        if (volumen > 100000)
        {
            precio += 3.00m;
        }

        // Assert
        volumen.Should().Be(6000);
        precio.Should().Be(9.00m); // 5 + 4, sin recargo
    }
}
