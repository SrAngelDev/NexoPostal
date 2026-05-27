using FluentAssertions;
using Nexopostal.Ciudadano.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

/// <summary>
/// Prueba E2E a nivel de API backend que valida el flujo de creación y trazabilidad de envíos.
/// </summary>
public class CiudadanoFlowIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CiudadanoFlowIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
        // Configurar cabecera de autenticación por defecto usando el esquema de test
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
    }

    [Fact]
    public async Task FlujoCompleto_CreacionYSeguimientoDeEnvio_DeberiaFuncionarCorrectamente()
    {
        // 1. Arrange - Definir un Dto de envío válido
        var crearEnvioDto = new CrearEnvioDto
        {
            Peso = 2.0m,
            Dimensiones = "20x20x20",
            NombreRemitente = "Remitente Pruebas",
            Origen = "Calle Gran Vía 1, Madrid",
            CodigoPostalOrigen = "28013",
            NombreDestinatario = "Destinatario Pruebas",
            Destino = "Avenida Diagonal 100, Barcelona",
            CodigoPostalDestino = "08019",
            OficinaOrigenId = 1,
            TipoEntrega = "Domicilio",
            Observaciones = "Cuidado frágil"
        };

        // 2. Act - Crear el envío
        var responseCrear = await _client.PostAsJsonAsync("/api/Envios/crear", crearEnvioDto);

        // Assert Crear
        responseCrear.StatusCode.Should().Be(HttpStatusCode.Created);
        var creadoDto = await responseCrear.Content.ReadFromJsonAsync<EnvioCreadoDto>();
        creadoDto.Should().NotBeNull();
        creadoDto!.NumeroSeguimiento.Should().NotBeNullOrEmpty();
        creadoDto.NumeroExpedicion.Should().NotBeNullOrEmpty();
        creadoDto.EstadoActual.Should().Be("Admitido");

        // 3. Act - Consultar el tracking público
        var trackingUrl = $"/api/Envios/track/{creadoDto.NumeroSeguimiento}";
        var responseTrack = await _client.GetAsync(trackingUrl);

        // Assert Tracking
        responseTrack.StatusCode.Should().Be(HttpStatusCode.OK);
        var trackingDto = await responseTrack.Content.ReadFromJsonAsync<EnvioTrackingDto>();
        trackingDto.Should().NotBeNull();
        trackingDto!.NumeroSeguimiento.Should().Be(creadoDto.NumeroSeguimiento);
        trackingDto.EstadoActual.Should().Be("Admitido");

        // 4. Act - Obtener los envíos del usuario autenticado
        var responseMisEnvios = await _client.GetAsync("/api/Envios/mis-envios");

        // Assert Mis Envíos
        responseMisEnvios.StatusCode.Should().Be(HttpStatusCode.OK);
        var misEnvios = await responseMisEnvios.Content.ReadFromJsonAsync<IEnumerable<EnvioResumenDto>>();
        misEnvios.Should().NotBeNull();
        misEnvios.Should().Contain(e => e.NumeroSeguimiento == creadoDto.NumeroSeguimiento);
    }
}
