using FluentAssertions;
using Nexopostal.Intranet.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace Nexopostal.Tests.Intranet;

/// <summary>
/// Tests de integración para ScanController.
/// </summary>
public class ScanControllerIntegrationTests : IClassFixture<CustomIntranetWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ScanControllerIntegrationTests(CustomIntranetWebApplicationFactory factory)
    {
        IntranetTestAuthHandler.DefaultRole = "OperarioCTA";
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");
    }

    // ═══════════════════════════════════════════
    //  Sin autenticación → 401
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_SinAutenticacion_DeberiaRetornar401()
    {
        // Cliente sin header de autorización
        var clientSinAuth = new CustomIntranetWebApplicationFactory().CreateClient();

        var request = new ScanRequestDto
        {
            ModoOperacion = ModosEscaneo.RecepcionCta,
            CodigoEscaneado = "EXP-TEST-001",
            OperarioNombre = "Operario Test",
            CtaId = 1
        };

        var response = await clientSinAuth.PostAsJsonAsync("/api/Scan/procesar", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ═══════════════════════════════════════════
    //  Modo inválido → 400
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_ModoInvalido_DeberiaRetornar400()
    {
        var request = new
        {
            ModoOperacion = "MODO_INVALIDO_XYZ",
            CodigoEscaneado = "EXP-TEST-001",
            OperarioId = 1,
            CtaId = 1
        };

        var response = await _client.PostAsJsonAsync("/api/Scan/procesar", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═══════════════════════════════════════════
    //  Código vacío → 400
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_CodigoVacio_DeberiaRetornar400()
    {
        var request = new ScanRequestDto
        {
            ModoOperacion = ModosEscaneo.RecepcionCta,
            CodigoEscaneado = "",
            OperarioNombre = "Operario Test",
            CtaId = 1
        };

        var response = await _client.PostAsJsonAsync("/api/Scan/procesar", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ═══════════════════════════════════════════
    //  Modo de oficina con rol OperarioCTA → 403
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ProcesarEscaneo_ModoOficinaConRolCta_DeberiaRetornar403()
    {
        var request = new ScanRequestDto
        {
            ModoOperacion = ModosEscaneo.RecepcionOficina, // Modo solo para OperarioOficina
            CodigoEscaneado = "EXP-TEST-001",
            OperarioNombre = "Operario Test",
            CtaId = 1
        };

        var response = await _client.PostAsJsonAsync("/api/Scan/procesar", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ═══════════════════════════════════════════
    //  ObtenerModos con usuario autenticado → 200
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ObtenerModos_UsuarioAutenticado_DeberiaRetornar200ConListaDeModos()
    {
        var response = await _client.GetAsync("/api/Scan/modos");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrEmpty();
    }
}
