using FluentAssertions;
using Nexopostal.Gateway.Models;
using Xunit;

namespace Nexopostal.Tests.Gateway;

public class GatewayApiExceptionTests
{
    [Fact]
    public void Constructor_CapturaCampos()
    {
        var ex = new GatewayApiException(422, "{\"e\":1}", "application/json");
        ex.StatusCode.Should().Be(422);
        ex.ResponseBody.Should().Be("{\"e\":1}");
        ex.ContentType.Should().Be("application/json");
        ex.Message.Should().Contain("422");
    }

    [Fact]
    public void Constructor_ContentTypeNull_AceptaNull()
    {
        var ex = new GatewayApiException(500, "boom", null);
        ex.ContentType.Should().BeNull();
        ex.StatusCode.Should().Be(500);
    }
}
