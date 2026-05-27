using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class CiudadanoEnvioAltaServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest;
        public Func<HttpRequestMessage, HttpResponseMessage> Responder = _ => new HttpResponseMessage(HttpStatusCode.OK);
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(Responder(request));
        }
    }

    private static CiudadanoEnvioAltaService Crear(StubHandler h)
    {
        var http = new HttpClient(h) { BaseAddress = new Uri("http://test/") };
        return new CiudadanoEnvioAltaService(http, NullLogger<CiudadanoEnvioAltaService>.Instance);
    }

    [Fact]
    public async Task CrearAsync_Exito_AnadeHeaderOficinaYDevuelveDto()
    {
        var dto = new EnvioAltaResultadoDto { NumeroExpedicion = "NXI-1", NumeroSeguimiento = "NXP-1" };
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(dto) } };
        var svc = Crear(h);

        var r = await svc.CrearAsync(new AltaEnvioOficinaIntranetDto(), 42);

        r!.NumeroExpedicion.Should().Be("NXI-1");
        h.LastRequest!.Headers.GetValues("X-Oficina-Origen-Id").Single().Should().Be("42");
        h.LastRequest.Method.Should().Be(HttpMethod.Post);
    }

    [Fact]
    public async Task CrearAsync_RespuestaError_DevuelveNull()
    {
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("bad") } };
        var svc = Crear(h);
        (await svc.CrearAsync(new AltaEnvioOficinaIntranetDto(), 1)).Should().BeNull();
    }

    [Fact]
    public async Task CrearAsync_Excepcion_DevuelveNull()
    {
        var h = new StubHandler { Responder = _ => throw new HttpRequestException("boom") };
        var svc = Crear(h);
        (await svc.CrearAsync(new AltaEnvioOficinaIntranetDto(), 1)).Should().BeNull();
    }
}
