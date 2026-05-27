using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Nexopostal.Gateway.Middleware;
using Xunit;

namespace Nexopostal.Tests.Gateway;

public class ErrorPropagationHandlerTests
{
    private sealed class StubInnerHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);
        public HttpRequestMessage? Last { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Last = request;
            return Task.FromResult(Response);
        }
    }

    private sealed class TestableHandler : ErrorPropagationHandler
    {
        public TestableHandler(IHttpContextAccessor a, HttpMessageHandler inner) : base(a)
        {
            InnerHandler = inner;
        }
        public Task<HttpResponseMessage> InvokeAsync(HttpRequestMessage req) =>
            SendAsync(req, CancellationToken.None);
    }

    [Fact]
    public async Task RespuestaExitosa_NoMarcaItems()
    {
        var inner = new StubInnerHandler { Response = new HttpResponseMessage(HttpStatusCode.OK) };
        var ctx = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        var h = new TestableHandler(accessor, inner);

        var res = await h.InvokeAsync(new HttpRequestMessage(HttpMethod.Get, "http://x/"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        ctx.Items.Should().NotContainKey("GatewayRealStatus");
    }

    [Fact]
    public async Task RespuestaError_MarcaItemsYDevuelve200()
    {
        var resp = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("{\"code\":\"X\"}", System.Text.Encoding.UTF8, "application/json")
        };
        var inner = new StubInnerHandler { Response = resp };
        var ctx = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        var h = new TestableHandler(accessor, inner);

        var res = await h.InvokeAsync(new HttpRequestMessage(HttpMethod.Get, "http://x/"));

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        ctx.Items["GatewayRealStatus"].Should().Be(404);
        ((string)ctx.Items["GatewayRealBody"]!).Should().Contain("X");
        ctx.Items["GatewayRealContentType"].Should().Be("application/json; charset=utf-8");
    }

    [Fact]
    public async Task ReenviaAuthorizationHeader()
    {
        var inner = new StubInnerHandler();
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["Authorization"] = "Bearer abc";
        var accessor = new HttpContextAccessor { HttpContext = ctx };
        var h = new TestableHandler(accessor, inner);

        await h.InvokeAsync(new HttpRequestMessage(HttpMethod.Get, "http://x/"));

        inner.Last!.Headers.Authorization!.ToString().Should().Be("Bearer abc");
    }

    [Fact]
    public async Task SinHttpContext_NoCrashea()
    {
        var inner = new StubInnerHandler();
        var accessor = new HttpContextAccessor { HttpContext = null };
        var h = new TestableHandler(accessor, inner);

        var res = await h.InvokeAsync(new HttpRequestMessage(HttpMethod.Get, "http://x/"));
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
