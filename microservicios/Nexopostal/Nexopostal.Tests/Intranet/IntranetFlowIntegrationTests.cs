using FluentAssertions;
using Nexopostal.Intranet.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Nexopostal.Tests.Intranet;

/// <summary>
/// Tests de integración E2E para el flujo de historial/trazabilidad en Intranet.
/// Valida endpoints de HistorialController y CtasController.
/// </summary>
public class IntranetFlowIntegrationTests : IClassFixture<CustomIntranetWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomIntranetWebApplicationFactory _factory;

    public IntranetFlowIntegrationTests(CustomIntranetWebApplicationFactory factory)
    {
        _factory = factory;
        IntranetTestAuthHandler.DefaultRole = "Admin";
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
    }

    // ═══════════════════════════════════════════
    //  HistorialController — Tracking público (sin auth)
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerTrackingPublico_NumeroSeguimientoValido_DeberiaRetornar200()
    {
        var response = await _client.GetAsync("/api/Historial/tracking/NX000TEST0001ES");

        // Sin datos en DB de test, debe retornar 200 con lista vacía (endpoint AllowAnonymous)
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ObtenerTrackingPublico_SinAutenticacion_DeberiaRetornar200()
    {
        // Endpoint [AllowAnonymous] — debe funcionar sin token
        var clientSinAuth = _factory.CreateClient();

        var response = await clientSinAuth.GetAsync("/api/Historial/tracking/NX000TEST9999ES");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ═══════════════════════════════════════════
    //  HistorialController — Historial interno (requiere auth)
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerHistorialInterno_SinAutenticacion_DeberiaRetornar401()
    {
        var clientSinAuth = _factory.CreateClient();

        var response = await clientSinAuth.GetAsync("/api/Historial/interno/NXI-TEST-001");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ObtenerHistorialInterno_UsuarioAutenticado_DeberiaRetornar200()
    {
        var response = await _client.GetAsync("/api/Historial/interno/NXI-TEST-001");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ═══════════════════════════════════════════
    //  CtasController — Consulta de CTAs
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerCtas_SinAutenticacion_DeberiaRetornar401()
    {
        var clientSinAuth = _factory.CreateClient();

        var response = await clientSinAuth.GetAsync("/api/Ctas");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ObtenerCtas_UsuarioAutenticado_DeberiaRetornar200()
    {
        var response = await _client.GetAsync("/api/Ctas");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("["); // JSON array
    }

    [Fact]
    public async Task ObtenerCtaDetalle_IdInexistente_DeberiaRetornar404()
    {
        var response = await _client.GetAsync("/api/Ctas/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResolverCtaPorCp_CodigoPostalValido_DeberiaRetornar200O404()
    {
        var response = await _client.GetAsync("/api/Ctas/resolver/28001");

        // Con DB vacía puede ser 404; lo importante es que no sea 500 ni 401
        ((int)response.StatusCode).Should().BeOneOf(200, 404);
    }

    // ═══════════════════════════════════════════
    //  HistorialController — Registrar evento (Admin/OperarioCTA)
    // ═══════════════════════════════════════════

    [Fact]
    public async Task RegistrarEvento_UsuarioAdminConDatosValidos_DeberiaRetornar201()
    {
        var dto = new CrearHistorialEventoDto
        {
            NumeroExpedicion = "NXI-TEST-EVENTO-001",
            NumeroSeguimiento = "NX000EVENTO001ES",
            Estado = "EnAdmision",
            Descripcion = "Paquete recibido en CTA-MAD para prueba",
            VisibleParaCliente = true
        };

        var response = await _client.PostAsJsonAsync("/api/Historial", dto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
