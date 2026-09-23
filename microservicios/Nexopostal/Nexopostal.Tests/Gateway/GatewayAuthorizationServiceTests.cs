using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Nexopostal.Gateway.Services;
using Xunit;
namespace Nexopostal.Tests.Gateway;
public class GatewayAuthorizationServiceTests
{
    [Theory]
    [InlineData("auth", "login", true)]
    [InlineData("AUTH", "REGISTER", true)]
    [InlineData("auth", "refresh", true)]
    [InlineData("auth", "solicitar-reset", true)]
    [InlineData("auth", "reset-password", true)]
    [InlineData("envios", "cotizar", true)]
    [InlineData("envios", "track", true)]
    [InlineData("pagos", "webhook", true)]
    [InlineData("tarifas", "consultar", true)]
    [InlineData("oficinas", "listar", true)]
    [InlineData("auth", "me", false)]
    [InlineData("envios", "crear", false)]
    [InlineData("reparto", "rutas", false)]
    [InlineData("admin", "usuarios", false)]
    [InlineData("unknown", "login", false)]
    public void PublicContractsAreExplicit(string api, string route, bool expected) =>
        Assert.Equal(expected, GatewayAuthorizationService.IsPublic(api, route));

    [Theory]
    [InlineData(null, "UNAUTHORIZED")]
    [InlineData("INVALID_TOKEN", "UNAUTHORIZED")]
    [InlineData("USER_BLOCKED", "USER_BLOCKED")]
    public void PreservesAuthenticationErrorCodes(string? error, string expected)
    {
        var context = new DefaultHttpContext();
        context.Items["GatewayAuthErrorCode"] = error;
        var body = JsonSerializer.SerializeToElement(GatewayAuthorizationService.UnauthorizedResponse(context));
        Assert.Equal(expected, body.GetProperty("code").GetString());
    }
}
