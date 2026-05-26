using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using NexoPostal.Auth.DTOs;
using Xunit;

namespace Nexopostal.Tests.Auth;

/// <summary>
/// Pruebas de integración para <see cref="NexoPostal.Auth.Controllers.AuthController"/> utilizando
/// <see cref="CustomAuthWebApplicationFactory{TProgram}"/>.
/// </summary>
public class AuthIntegrationTests : IClassFixture<CustomAuthWebApplicationFactory<NexoPostal.Auth.Program>>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(CustomAuthWebApplicationFactory<NexoPostal.Auth.Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_ConDatosValidos_DeberiaCrearUsuarioYRetornarOk()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "integracion@test.com",
            Password = "SecurePassword123!",
            NombreCompleto = "Usuario Integracion"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", registerDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ConUsuarioInexistente_DeberiaRetornarUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "noexiste@test.com",
            Password = "WrongPassword123!"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SolicitarReset_DeberiaRetornarOkSiempre()
    {
        // Arrange
        var request = new SolicitarResetPasswordDto
        {
            Email = "cualquiera@test.com",
            FrontendUrl = "http://localhost:4200"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/solicitar-reset", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();
    }
}
