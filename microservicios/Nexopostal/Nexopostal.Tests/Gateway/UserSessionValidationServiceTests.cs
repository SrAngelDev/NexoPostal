using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Nexopostal.Gateway.Services;
using System.Net;
using System.Text;
using Xunit;

namespace Nexopostal.Tests.Gateway;

/// <summary>
/// Tests unitarios para UserSessionValidationService.
/// Usa un mock de HttpMessageHandler para simular las respuestas del microservicio Auth.
/// </summary>
public class UserSessionValidationServiceTests
{
    private static IConfiguration BuildConfig(string? serviceKey = null)
    {
        var dict = new Dictionary<string, string?>();
        if (serviceKey != null)
            dict["InterServiceSettings:ServiceKey"] = serviceKey;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    private static UserSessionValidationService BuildService(HttpResponseMessage responseToReturn)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(responseToReturn);

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://nexopostal-auth/")
        };

        return new UserSessionValidationService(httpClient, BuildConfig(), NullLogger<UserSessionValidationService>.Instance);
    }

    private static UserSessionValidationService BuildServiceThatThrows()
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Red no disponible"));

        var httpClient = new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("http://nexopostal-auth/")
        };

        return new UserSessionValidationService(httpClient, BuildConfig(), NullLogger<UserSessionValidationService>.Instance);
    }

    // ═══════════════════════════════════════════
    //  userId vacío → Blocked
    // ═══════════════════════════════════════════

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task ValidateAsync_UserIdVacio_DeberiaRetornarBlocked(string? userId)
    {
        var service = BuildService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"activo\":true}", Encoding.UTF8, "application/json")
        });

        var result = await service.ValidateAsync(userId!, CancellationToken.None);

        result.Should().Be(SessionValidationStatus.Blocked);
    }

    // ═══════════════════════════════════════════
    //  Respuesta 200 con activo:true → Active
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_Respuesta200Activo_DeberiaRetornarActive()
    {
        var service = BuildService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"activo\":true}", Encoding.UTF8, "application/json")
        });

        var result = await service.ValidateAsync("valid-user-id", CancellationToken.None);

        result.Should().Be(SessionValidationStatus.Active);
    }

    // ═══════════════════════════════════════════
    //  Respuesta 200 con activo:false → Blocked
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_Respuesta200Bloqueado_DeberiaRetornarBlocked()
    {
        var service = BuildService(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"activo\":false}", Encoding.UTF8, "application/json")
        });

        var result = await service.ValidateAsync("blocked-user-id", CancellationToken.None);

        result.Should().Be(SessionValidationStatus.Blocked);
    }

    // ═══════════════════════════════════════════
    //  Excepción de red → Unknown
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_ExcepcionDeRed_DeberiaRetornarUnknown()
    {
        var service = BuildServiceThatThrows();

        var result = await service.ValidateAsync("some-user-id", CancellationToken.None);

        result.Should().Be(SessionValidationStatus.Unknown);
    }

    // ═══════════════════════════════════════════
    //  Respuesta 403 (Gateway sin permiso) → Unknown
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_Respuesta403_DeberiaRetornarUnknown()
    {
        var service = BuildService(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("{\"error\":\"forbidden\"}", Encoding.UTF8, "application/json")
        });

        var result = await service.ValidateAsync("some-user-id", CancellationToken.None);

        result.Should().Be(SessionValidationStatus.Unknown);
    }

    // ═══════════════════════════════════════════
    //  Respuesta 500 → Unknown
    // ═══════════════════════════════════════════

    [Fact]
    public async Task ValidateAsync_Respuesta500_DeberiaRetornarUnknown()
    {
        var service = BuildService(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var result = await service.ValidateAsync("some-user-id", CancellationToken.None);

        result.Should().Be(SessionValidationStatus.Unknown);
    }
}
