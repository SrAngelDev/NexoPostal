using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class CiudadanoEnvioLookupServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Responder = _ => new HttpResponseMessage(HttpStatusCode.NotFound);
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Responder(request));
        }
    }

    private static CiudadanoEnvioLookupService Crear(StubHandler handler, IMemoryCache? cache = null)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://test/") };
        return new CiudadanoEnvioLookupService(http, cache ?? new MemoryCache(new MemoryCacheOptions()), NullLogger<CiudadanoEnvioLookupService>.Instance);
    }

    [Fact]
    public async Task ObtenerAsync_ExpedicionVacia_DevuelveNull()
    {
        var svc = Crear(new StubHandler());
        (await svc.ObtenerAsync("")).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerAsync_NotFound_DevuelveNull()
    {
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.NotFound) };
        var svc = Crear(h);
        (await svc.ObtenerAsync("NXI-1")).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerAsync_OtroErrorHttp_DevuelveNull()
    {
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.InternalServerError) };
        var svc = Crear(h);
        (await svc.ObtenerAsync("NXI-2")).Should().BeNull();
    }

    [Fact]
    public async Task ObtenerAsync_Exito_DevuelveDtoYCachea()
    {
        var dto = new EnvioInternoServiceLookupDto { NumeroExpedicion = "NXI-3", NumeroSeguimiento = "S" };
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(dto) } };
        var svc = Crear(h);
        var r1 = await svc.ObtenerAsync("NXI-3");
        var r2 = await svc.ObtenerAsync("NXI-3");
        r1!.NumeroExpedicion.Should().Be("NXI-3");
        r2.Should().NotBeNull();
        h.Calls.Should().Be(1, "la segunda llamada debe servirse desde cache");
    }

    [Fact]
    public async Task ObtenerAsync_ExcepcionHttp_DevuelveNull()
    {
        var h = new StubHandler { Responder = _ => throw new HttpRequestException("boom") };
        var svc = Crear(h);
        (await svc.ObtenerAsync("NXI-4")).Should().BeNull();
    }

    [Fact]
    public async Task Invalidar_VaciaCacheEspecifico()
    {
        var dto = new EnvioInternoServiceLookupDto { NumeroExpedicion = "NXI-5" };
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(dto) } };
        var svc = Crear(h);
        await svc.ObtenerAsync("NXI-5");
        svc.Invalidar("NXI-5");
        await svc.ObtenerAsync("NXI-5");
        h.Calls.Should().Be(2);
    }

    [Fact]
    public void Invalidar_StringVacio_NoFalla()
    {
        var svc = Crear(new StubHandler());
        svc.Invoking(s => s.Invalidar("")).Should().NotThrow();
    }
}
