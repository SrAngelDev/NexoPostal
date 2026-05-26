using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.DTOs;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

/// <summary>
/// Mock de autenticación para simular usuario autenticado en las pruebas de integración.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id-123"),
            new Claim("sub", "test-user-id-123"),
            new Claim(ClaimTypes.Name, "test@nexopostal.com"),
            new Claim(ClaimTypes.Role, "Cliente")
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, "Test");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Factoría de servidor web que inyecta el Mock de Autenticación.
/// </summary>
public class FlowTestWebApplicationFactory : WebApplicationFactory<Nexopostal.Ciudadano.Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // 1. Quitar descriptores de Npgsql
            var npgsqlDescriptors = services
                .Where(d =>
                    (d.ServiceType?.FullName?.Contains("Npgsql") == true) ||
                    (d.ImplementationType?.FullName?.Contains("Npgsql") == true) ||
                    (d.ImplementationFactory?.Method.DeclaringType?.FullName?.Contains("Npgsql") == true))
                .ToList();
            foreach (var d in npgsqlDescriptors) services.Remove(d);

            var dbDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<CiudadanoDbContext>));
            if (dbDescriptor != null) services.Remove(dbDescriptor);

            // IDbContextOptionsConfiguration<CiudadanoDbContext> guarda la lambda UseNpgsql
            var optionsConfigDescriptors = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration") &&
                    d.ServiceType.GenericTypeArguments.Length == 1 &&
                    d.ServiceType.GenericTypeArguments[0] == typeof(CiudadanoDbContext))
                .ToList();
            foreach (var d in optionsConfigDescriptors) services.Remove(d);

            // 2. Agregar base de datos en memoria
            services.AddDbContext<CiudadanoDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryFlowDbForTesting");
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // 3. Registrar el TestAuthHandler
            services.AddAuthentication(defaultScheme: "Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
        });
    }
}

/// <summary>
/// Prueba E2E a nivel de API backend que valida el flujo de creación y trazabilidad de envíos.
/// </summary>
public class CiudadanoFlowIntegrationTests : IClassFixture<FlowTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CiudadanoFlowIntegrationTests(FlowTestWebApplicationFactory factory)
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
