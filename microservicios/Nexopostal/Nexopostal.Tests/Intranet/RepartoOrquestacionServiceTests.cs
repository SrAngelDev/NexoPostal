using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class RepartoOrquestacionServiceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest;
        public string LastBody = string.Empty;
        public Func<HttpRequestMessage, HttpResponseMessage> Responder = _ => new HttpResponseMessage(HttpStatusCode.OK);
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            if (request.Content != null)
                LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
            return Responder(request);
        }
    }

    private static RepartoOrquestacionService Crear(StubHandler h, IConfiguration? cfg = null)
    {
        var http = new HttpClient(h) { BaseAddress = new Uri("http://reparto/") };
        cfg ??= new ConfigurationBuilder().Build();
        return new RepartoOrquestacionService(http, cfg, NullLogger<RepartoOrquestacionService>.Instance);
    }

    private static AdmisionPaqueteDto BaseAdmision() => new()
    {
        NumeroExpedicion = "NXI-001",
        NumeroSeguimiento = "NXP-001",
        CodigoPostalDestino = "28001",
        DireccionEntrega = "C/Sol 1",
        CiudadDestino = "Madrid",
        NombreDestinatario = "Ada",
        TelefonoDestinatario = "+34600",
        EsUrgente = true
    };

    private static ResolverCtaResponseDto BaseCta() => new()
    {
        CtaId = 7,
        CtaCodigo = "CTA-MAD",
        Provincia = "Madrid"
    };

    [Fact]
    public async Task AutoAsignar_Exito_MapeaCamposYAplicaHeaderServiceKey()
    {
        var respuesta = new
        {
            success = true,
            idempotente = false,
            creadaRuta = true,
            rutaId = 1,
            rutaCodigo = "R-1",
            repartidorId = 2,
            repartidorNombre = "Bob",
            entregaId = 3,
            message = "ok"
        };
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(respuesta) } };
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RepartoSettings:ServiceKey"] = "K-1"
        }).Build();
        var svc = Crear(h, cfg);

        var r = await svc.AutoAsignarEntregaDesdeAdmisionAsync(BaseAdmision(), BaseCta());

        r.Success.Should().BeTrue();
        r.RutaCodigo.Should().Be("R-1");
        r.RepartidorNombre.Should().Be("Bob");
        h.LastRequest!.Headers.GetValues("X-Service-Key").Single().Should().Be("K-1");
        h.LastRequest.RequestUri!.AbsolutePath.Should().EndWith("/api/reparto/interno/admision/auto-asignar");
    }

    [Fact]
    public async Task AutoAsignar_ErrorHttp_DevuelveSuccessFalse()
    {
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("nope") } };
        var svc = Crear(h);
        var r = await svc.AutoAsignarEntregaDesdeAdmisionAsync(BaseAdmision(), BaseCta());
        r.Success.Should().BeFalse();
        r.Message.Should().Contain("502");
    }

    [Fact]
    public async Task AutoAsignar_PayloadNulo_DevuelveSuccessFalse()
    {
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json") } };
        var svc = Crear(h);
        var r = await svc.AutoAsignarEntregaDesdeAdmisionAsync(BaseAdmision(), BaseCta());
        r.Success.Should().BeFalse();
        r.Message.Should().Contain("payload");
    }

    [Fact]
    public async Task AutoAsignar_Excepcion_DevuelveErrorAmigable()
    {
        var h = new StubHandler { Responder = _ => throw new HttpRequestException("boom") };
        var svc = Crear(h);
        var r = await svc.AutoAsignarEntregaDesdeAdmisionAsync(BaseAdmision(), BaseCta());
        r.Success.Should().BeFalse();
        r.Message.Should().Contain("Reparto");
    }

    [Fact]
    public async Task AutoAsignar_SinNumeroSeguimiento_UsaExpedicion()
    {
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true, message = "ok" }) } };
        var svc = Crear(h);
        var dto = BaseAdmision();
        dto.NumeroSeguimiento = " ";
        await svc.AutoAsignarEntregaDesdeAdmisionAsync(dto, BaseCta());
        h.LastBody.Should().Contain("\"numeroSeguimiento\":\"NXI-001\"");
    }

    [Fact]
    public async Task AutoAsignar_SinCiudadDestino_UsaProvinciaCta()
    {
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true, message = "ok" }) } };
        var svc = Crear(h);
        var dto = BaseAdmision();
        dto.CiudadDestino = null;
        var cta = BaseCta();
        cta.Provincia = "ProvinciaCTA";
        await svc.AutoAsignarEntregaDesdeAdmisionAsync(dto, cta);
        h.LastBody.Should().Contain("\"ciudadDestino\":\"ProvinciaCTA\"");
    }

    [Fact]
    public async Task AutoAsignar_SinNombreDestinatario_UsaDestinatarioOFallback()
    {
        var h = new StubHandler { Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { success = true, message = "ok" }) } };
        var svc = Crear(h);
        var dto = BaseAdmision();
        dto.NombreDestinatario = null;
        dto.Destinatario = "DestFallback";
        await svc.AutoAsignarEntregaDesdeAdmisionAsync(dto, BaseCta());
        h.LastBody.Should().Contain("DestFallback");

        var dto2 = BaseAdmision();
        dto2.NombreDestinatario = null;
        dto2.Destinatario = null;
        await svc.AutoAsignarEntregaDesdeAdmisionAsync(dto2, BaseCta());
        h.LastBody.Should().Contain("Destinatario no informado");
    }
}
