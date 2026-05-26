using FluentAssertions;
using Nexopostal.Reparto.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Nexopostal.Tests.Reparto;

/// <summary>
/// Tests de integración para RepartoController.
/// </summary>
public class RepartoControllerIntegrationTests : IClassFixture<CustomRepartoWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomRepartoWebApplicationFactory _factory;

    public RepartoControllerIntegrationTests(CustomRepartoWebApplicationFactory factory)
    {
        _factory = factory;
        RepartoTestAuthHandler.DefaultRole = "Admin";
        RepartoTestAuthHandler.DefaultIdentityUserId = "test-reparto-admin-id";
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
    }

    // ═══════════════════════════════════════════
    //  Sin autenticación → 401
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerRepartidores_SinAutenticacion_DeberiaRetornar401()
    {
        var clientSinAuth = _factory.CreateClient();

        var response = await clientSinAuth.GetAsync("/api/reparto/repartidores");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════
    //  Obtener repartidores → 200
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerRepartidores_UsuarioAdmin_DeberiaRetornar200()
    {
        var response = await _client.GetAsync("/api/reparto/repartidores");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("["); // JSON array
    }

    // ═══════════════════════════════════════════
    //  Mi perfil (Repartidor) → 404 con DB vacía
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerMiPerfil_RepartidorAutenticadoSinPerfil_DeberiaRetornar404()
    {
        RepartoTestAuthHandler.DefaultRole = "Repartidor";
        RepartoTestAuthHandler.DefaultIdentityUserId = "repartidor-sin-perfil-id";
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.GetAsync("/api/reparto/mi-perfil");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════════════════════════════════
    //  Obtener rutas → 200
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerRutas_UsuarioAdmin_DeberiaRetornar200()
    {
        var response = await _client.GetAsync("/api/reparto/rutas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ObtenerRutaPorId_IdInexistente_DeberiaRetornar404()
    {
        var response = await _client.GetAsync("/api/reparto/rutas/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ═══════════════════════════════════════════
    //  Crear repartidor → 201
    // ═══════════════════════════════════════════

    [Fact]
    public async Task CrearRepartidor_DatosValidos_DeberiaRetornar201()
    {
        var dto = new CrearRepartidorDto
        {
            IdentityUserId = "new-repartidor-test-id",
            NombreCompleto = "Repartidor Prueba",
            CodigoEmpleado = "EMP-TEST-001",
            Rol = "Repartidor",
            TipoVehiculo = "Furgoneta",
            OficinaJsonId = 1,
            OficinaNombre = "Oficina Central"
        };

        var response = await _client.PostAsJsonAsync("/api/reparto/repartidores", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
