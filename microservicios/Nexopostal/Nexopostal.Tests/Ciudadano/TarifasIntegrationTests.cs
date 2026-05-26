using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Nexopostal.Ciudadano.Controllers;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

/// <summary>
/// Pruebas de integración para <see cref="TarifasController"/> utilizando
/// <see cref="CustomWebApplicationFactory{TProgram}"/>.
/// </summary>
public class TarifasIntegrationTests : IClassFixture<CustomWebApplicationFactory<Nexopostal.Ciudadano.Program>>
{
    private readonly HttpClient _client;

    public TarifasIntegrationTests(CustomWebApplicationFactory<Nexopostal.Ciudadano.Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Consultar_ConParametrosPorDefecto_DeberiaRetornarOkConTarifas()
    {
        // Act
        var response = await _client.GetAsync("/api/Tarifas/consultar");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<TarifasResponseDto>();
        content.Should().NotBeNull();
        content!.Zona.Should().Be("Península"); // Por defecto sin CP = Península
        content.Tarifas.Should().HaveCount(2);
        content.Tarifas.Should().Contain(t => t.Nombre == "Estandar");
        content.Tarifas.Should().Contain(t => t.Nombre == "Premium");
    }

    [Fact]
    public async Task Consultar_ConCodigoPostalCanarias_DeberiaResolverCanariasYMultiplicar()
    {
        // Act
        var response = await _client.GetAsync("/api/Tarifas/consultar?peso=1&codigoPostalOrigen=28001&codigoPostalDestino=35000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadFromJsonAsync<TarifasResponseDto>();
        content.Should().NotBeNull();
        content!.Zona.Should().Be("Canarias");
        content.Tarifas[0].PrecioBase.Should().Be(8.63m); // 5.95 * 1.45
        content.Tarifas[0].PrecioTotal.Should().Be(10.44m); // Con 21% IVA
    }

    [Fact]
    public async Task Calcular_PeticionValidaPenínsula_DeberiaRetornarCalculoCorrecto()
    {
        // Arrange
        var request = new CalcularPrecioRequestDto
        {
            Peso = 2.5m,
            CodigoPostalOrigen = "28001",
            CodigoPostalDestino = "08001", // Península
            TipoTarifa = "Premium"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Tarifas/calcular", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<CalculoPrecioDto>();
        content.Should().NotBeNull();
        content!.Zona.Should().Be("Península");
        content.TipoTarifa.Should().Be("Premium");
        content.PesoFacturable.Should().Be(2.5m);
        
        // 2.5kg entra en la banda de 5kg -> DefaultPeninsulaPremium[2] = 13.95m
        content.PrecioBase.Should().Be(13.95m);
        content.Recargo.Should().Be(0m);
        content.Iva.Should().Be(2.93m); // 13.95 * 0.21 = 2.9295 -> 2.93m
        content.PrecioTotal.Should().Be(16.88m); // 13.95 + 2.93 = 16.88m
    }

    [Fact]
    public async Task Calcular_PeticionInvalida_DeberiaRetornarBadRequest()
    {
        // Arrange - Peso fuera del rango permitido (> 30kg)
        var request = new CalcularPrecioRequestDto
        {
            Peso = 45m,
            CodigoPostalOrigen = "28001",
            CodigoPostalDestino = "08001",
            TipoTarifa = "Estandar"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/Tarifas/calcular", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
