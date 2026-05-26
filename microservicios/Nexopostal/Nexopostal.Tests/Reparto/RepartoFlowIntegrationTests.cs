using FluentAssertions;
using Nexopostal.Reparto.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Nexopostal.Tests.Reparto;

/// <summary>
/// Tests de integración E2E para el flujo completo de reparto:
/// Crear repartidor → Crear ruta → Agregar entrega → Iniciar ruta.
/// </summary>
public class RepartoFlowIntegrationTests : IClassFixture<CustomRepartoWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    public RepartoFlowIntegrationTests(CustomRepartoWebApplicationFactory factory)
    {
        RepartoTestAuthHandler.DefaultRole = "Admin";
        RepartoTestAuthHandler.DefaultIdentityUserId = "flow-admin-id";
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
    }

    // ═══════════════════════════════════════════
    //  Flujo: Crear repartidor → Crear ruta
    // ═══════════════════════════════════════════

    [Fact]
    public async Task FlujoReparto_CrearRepartidorYCrearRuta_DeberiaFuncionarCorrectamente()
    {
        // 1. Crear repartidor
        var crearDto = new CrearRepartidorDto
        {
            IdentityUserId = "flow-repartidor-identity-id",
            NombreCompleto = "Repartidor Flujo",
            CodigoEmpleado = "FLOW-001",
            Rol = "Repartidor",
            TipoVehiculo = "Furgoneta",
            OficinaJsonId = 1,
            OficinaNombre = "Oficina Central Flow"
        };

        var crearResp = await _client.PostAsJsonAsync("/api/reparto/repartidores", crearDto);
        crearResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var repartidorJson = await crearResp.Content.ReadAsStringAsync();
        var repartidor = JsonSerializer.Deserialize<JsonElement>(repartidorJson, _jsonOpts);
        var repartidorId = repartidor.GetProperty("id").GetInt32();
        repartidorId.Should().BeGreaterThan(0);

        // 2. Crear ruta para ese repartidor
        var crearRutaDto = new CrearRutaRepartoDto
        {
            RepartidorId = repartidorId,
            FechaReparto = DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd"),
            OficinaOrigenJsonId = 1,
            OficinaOrigenNombre = "Oficina Central"
        };

        var crearRutaResp = await _client.PostAsJsonAsync("/api/reparto/rutas", crearRutaDto);
        crearRutaResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var rutaJson = await crearRutaResp.Content.ReadAsStringAsync();
        var ruta = JsonSerializer.Deserialize<JsonElement>(rutaJson, _jsonOpts);
        var rutaId = ruta.GetProperty("id").GetInt32();
        rutaId.Should().BeGreaterThan(0);

        // 3. Verificar que la ruta existe y está en estado Planificada
        var getRutaResp = await _client.GetAsync($"/api/reparto/rutas/{rutaId}");
        getRutaResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var getRutaJson = await getRutaResp.Content.ReadAsStringAsync();
        getRutaJson.Should().Contain("Planificada");
    }

    // ═══════════════════════════════════════════
    //  Dashboard
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerDashboard_UsuarioAdmin_DeberiaRetornar200()
    {
        var response = await _client.GetAsync("/api/reparto/dashboard");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
