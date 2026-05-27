using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nexopostal.Gateway.Controllers;
using Xunit;

namespace Nexopostal.Tests.Gateway;

/// <summary>
/// Stub handler para HttpClient. Captura request y devuelve respuesta configurada.
/// </summary>
internal sealed class GatewayStubHandler : HttpMessageHandler
{
    public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK)
    {
        Content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json")
    };
    public List<HttpRequestMessage> Requests { get; } = new();
    public string? LastBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (request.Content != null)
            LastBody = await request.Content.ReadAsStringAsync(cancellationToken);
        return Response;
    }
}

internal static class GatewayTestHelpers
{
    public static IConfiguration EmptyConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

    public static void Wire(ControllerBase controller, string method, string? query = null, string? body = null)
    {
        var http = new DefaultHttpContext();
        http.Request.Method = method;
        if (query != null) http.Request.QueryString = new QueryString(query);
        if (body != null)
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(body));
            http.Request.Body = stream;
            http.Request.ContentType = "application/json";
        }
        http.Request.Headers["Authorization"] = "Bearer testtoken";
        controller.ControllerContext = new ControllerContext { HttpContext = http };
    }
}

[CollectionDefinition("GatewayProxy", DisableParallelization = true)]
public class GatewayProxyCollection { }

[Collection("GatewayProxy")]
public class AdminUsersProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly AdminUsersProxyController _ctrl;

    public AdminUsersProxyControllerTests()
    {
        AdminUsersProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new AdminUsersProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task Listar_GET_PreservaQueryString()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET", "?rol=Admin");
        var r = (ContentResult)await _ctrl.Listar();
        r.StatusCode.Should().Be(200);
        _stub.Requests[0].RequestUri!.ToString().Should().EndWith("api/admin-usuarios?rol=Admin");
        _stub.Requests[0].Method.Should().Be(HttpMethod.Get);
        _stub.Requests[0].Headers.Authorization!.ToString().Should().Be("Bearer testtoken");
    }

    [Fact]
    public async Task Detalle_GET_PorId()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET");
        await _ctrl.Detalle("abc");
        _stub.Requests[0].RequestUri!.ToString().Should().EndWith("api/admin-usuarios/abc");
    }

    [Fact]
    public async Task Crear_POST_EnviaBody()
    {
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{\"x\":1}");
        await _ctrl.Crear();
        _stub.Requests[0].Method.Should().Be(HttpMethod.Post);
        _stub.LastBody.Should().Be("{\"x\":1}");
    }

    [Fact]
    public async Task Editar_PUT()
    {
        GatewayTestHelpers.Wire(_ctrl, "PUT", body: "{}");
        await _ctrl.Editar("u1");
        _stub.Requests[0].RequestUri!.ToString().Should().EndWith("api/admin-usuarios/u1");
        _stub.Requests[0].Method.Should().Be(HttpMethod.Put);
    }

    [Fact]
    public async Task Eliminar_DELETE_SinBody()
    {
        GatewayTestHelpers.Wire(_ctrl, "DELETE");
        await _ctrl.Eliminar("u1");
        _stub.Requests[0].Method.Should().Be(HttpMethod.Delete);
        _stub.LastBody.Should().BeNull();
    }

    [Fact]
    public async Task Restaurar_POST()
    {
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "");
        await _ctrl.Restaurar("u1");
        _stub.Requests[0].RequestUri!.ToString().Should().EndWith("u1/restaurar");
    }

    [Fact]
    public async Task EndpointsRestantes_LlamanProxy()
    {
        GatewayTestHelpers.Wire(_ctrl, "PUT", body: "{}");
        await _ctrl.CambiarRol("u1");
        await _ctrl.Bloquear("u1");
        await _ctrl.Desbloquear("u1");
        await _ctrl.ActualizarCta("u1");
        await _ctrl.ActualizarOficina("u1");
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{}");
        await _ctrl.ResetPassword("u1");
        GatewayTestHelpers.Wire(_ctrl, "GET");
        await _ctrl.DetalleOperativo("u1");
        await _ctrl.ObtenerOficina("u1");
        _stub.Requests.Should().HaveCount(8);
    }

    [Fact]
    public async Task RespuestaError_DevuelveStatusReal()
    {
        _stub.Response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("nope", Encoding.UTF8, "text/plain")
        };
        GatewayTestHelpers.Wire(_ctrl, "GET");
        var r = (ContentResult)await _ctrl.Detalle("zzz");
        r.StatusCode.Should().Be(404);
        r.Content.Should().Be("nope");
    }
}

[Collection("GatewayProxy")]
public class AsignacionesProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly AsignacionesProxyController _ctrl;

    public AsignacionesProxyControllerTests()
    {
        AsignacionesProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new AsignacionesProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task Buscar_PreservaQueryYAuth()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET", "?codigo=NXI-1");
        var r = (ContentResult)await _ctrl.Buscar();
        r.StatusCode.Should().Be(200);
        _stub.Requests[0].RequestUri!.ToString().Should().EndWith("api/asignaciones/buscar?codigo=NXI-1");
    }
}

[Collection("GatewayProxy")]
public class ClientesAdminProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly ClientesAdminProxyController _ctrl;

    public ClientesAdminProxyControllerTests()
    {
        ClientesAdminProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new ClientesAdminProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task Listar_FuerzaRolCliente_PreservaResto()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET", "?q=ana&rol=Admin");
        await _ctrl.Listar();
        var url = _stub.Requests[0].RequestUri!.ToString();
        url.Should().Contain("rol=Cliente");
        url.Should().Contain("q=ana");
        url.Should().NotContain("rol=Admin");
    }

    [Fact]
    public async Task Detalle_Bloquear_Desbloquear_ResetPassword_PerfilCompleto()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET");
        await _ctrl.Detalle("c1");
        await _ctrl.PerfilCompleto("c1");
        GatewayTestHelpers.Wire(_ctrl, "PUT", body: "{}");
        await _ctrl.Bloquear("c1");
        await _ctrl.Desbloquear("c1");
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{}");
        await _ctrl.ResetPassword("c1");
        _stub.Requests.Should().HaveCount(5);
    }
}

[Collection("GatewayProxy")]
public class CtasAdminProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly CtasAdminProxyController _ctrl;
    public CtasAdminProxyControllerTests()
    {
        CtasAdminProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new CtasAdminProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task TodosLosEndpoints()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET", "?activo=true");
        await _ctrl.Listar();
        await _ctrl.Detalle(1);
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{\"n\":1}");
        await _ctrl.Crear();
        await _ctrl.Reactivar(2);
        GatewayTestHelpers.Wire(_ctrl, "PUT", body: "{}");
        await _ctrl.Editar(3);
        GatewayTestHelpers.Wire(_ctrl, "DELETE");
        await _ctrl.Desactivar(4);
        _stub.Requests.Should().HaveCount(6);
    }
}

[Collection("GatewayProxy")]
public class EnviosAdminProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly EnviosAdminProxyController _ctrl;
    public EnviosAdminProxyControllerTests()
    {
        EnviosAdminProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new EnviosAdminProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task TodosLosEndpoints()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET");
        await _ctrl.Listar();
        await _ctrl.Obtener("NXP-1");
        GatewayTestHelpers.Wire(_ctrl, "PUT", body: "{}");
        await _ctrl.CambiarEstado("NXP-1");
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{}");
        await _ctrl.Anular("NXP-1");
        await _ctrl.Reabrir("NXP-1");
        _stub.Requests.Should().HaveCount(5);
    }
}

[Collection("GatewayProxy")]
public class FileProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly FileProxyController _ctrl;
    public FileProxyControllerTests()
    {
        FileProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new FileProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task DescargarEtiqueta_OK_DevuelveFile()
    {
        var bytes = new byte[] { 1, 2, 3 };
        _stub.Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(bytes)
            {
                Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf") }
            }
        };
        GatewayTestHelpers.Wire(_ctrl, "GET");
        var r = await _ctrl.DescargarEtiqueta("NXP-1");
        r.Should().BeOfType<FileContentResult>();
        ((FileContentResult)r).FileContents.Should().Equal(bytes);
    }

    [Fact]
    public async Task DescargarFactura_Error_DevuelveStatusErrorYBody()
    {
        _stub.Response = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            Content = new StringContent("missing")
        };
        GatewayTestHelpers.Wire(_ctrl, "GET");
        var r = await _ctrl.DescargarFactura("NXP-9");
        r.Should().BeOfType<ObjectResult>();
        ((ObjectResult)r).StatusCode.Should().Be(404);
    }
}

[Collection("GatewayProxy")]
public class NotificacionesProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly NotificacionesProxyController _ctrl;
    public NotificacionesProxyControllerTests()
    {
        NotificacionesProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new NotificacionesProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task Broadcast_POST_EnviaBody()
    {
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{\"msg\":\"hi\"}");
        var r = (ContentResult)await _ctrl.Broadcast();
        r.StatusCode.Should().Be(200);
        _stub.LastBody.Should().Contain("hi");
    }
}

[Collection("GatewayProxy")]
public class OficinasProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly OficinasProxyController _ctrl;
    public OficinasProxyControllerTests()
    {
        OficinasProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new OficinasProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task Listar_Y_Buscar_PreservanQuery()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET", "?activa=true");
        await _ctrl.Listar();
        await _ctrl.Buscar();
        _stub.Requests[0].RequestUri!.ToString().Should().EndWith("api/oficinas?activa=true");
        _stub.Requests[1].RequestUri!.ToString().Should().EndWith("api/oficinas/buscar?activa=true");
    }
}

[Collection("GatewayProxy")]
public class OficinasAdminProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly OficinasAdminProxyController _ctrl;
    public OficinasAdminProxyControllerTests()
    {
        OficinasAdminProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new OficinasAdminProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task TodosLosEndpoints()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET");
        await _ctrl.Listar();
        await _ctrl.Obtener(1);
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{}");
        await _ctrl.Crear();
        await _ctrl.Reactivar(1);
        GatewayTestHelpers.Wire(_ctrl, "PUT", body: "{}");
        await _ctrl.Editar(1);
        GatewayTestHelpers.Wire(_ctrl, "DELETE");
        await _ctrl.Desactivar(1);
        _stub.Requests.Should().HaveCount(6);
    }
}

[Collection("GatewayProxy")]
public class RepartidoresAdminProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly RepartidoresAdminProxyController _ctrl;
    public RepartidoresAdminProxyControllerTests()
    {
        RepartidoresAdminProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new RepartidoresAdminProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task TodosLosEndpoints()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET");
        await _ctrl.Listar();
        await _ctrl.PorIdentity("guid-1");
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{}");
        await _ctrl.Crear();
        await _ctrl.Reactivar(1);
        GatewayTestHelpers.Wire(_ctrl, "PUT", body: "{}");
        await _ctrl.Editar(1);
        GatewayTestHelpers.Wire(_ctrl, "DELETE");
        await _ctrl.Desactivar(1);
        _stub.Requests.Should().HaveCount(6);
    }
}

[Collection("GatewayProxy")]
public class RepartoEntregasProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly RepartoEntregasProxyController _ctrl;
    public RepartoEntregasProxyControllerTests()
    {
        RepartoEntregasProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new RepartoEntregasProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task Get_PreservaQueryString()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET", "?rutaId=7");
        var r = (ContentResult)await _ctrl.GetEntregas();
        r.StatusCode.Should().Be(200);
        _stub.Requests[0].RequestUri!.ToString().Should().EndWith("api/reparto/entregas?rutaId=7");
    }
}

[Collection("GatewayProxy")]
public class TarifasProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly TarifasProxyController _ctrl;
    public TarifasProxyControllerTests()
    {
        TarifasProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new TarifasProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task Consultar_GET_PreservaQuery()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET", "?cp=28013");
        await _ctrl.Consultar();
        _stub.Requests[0].RequestUri!.ToString().Should().Contain("api/tarifas/consultar?cp=28013");
    }

    [Fact]
    public async Task Calcular_POST_EnviaBodyConContentType()
    {
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{\"peso\":1}");
        await _ctrl.Calcular();
        _stub.LastBody.Should().Be("{\"peso\":1}");
        _stub.Requests[0].Content!.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}

[Collection("GatewayProxy")]
public class TarifasAdminProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly TarifasAdminProxyController _ctrl;
    public TarifasAdminProxyControllerTests()
    {
        TarifasAdminProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new TarifasAdminProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task TodosLosEndpoints()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET");
        await _ctrl.Listar();
        await _ctrl.Obtener(1);
        GatewayTestHelpers.Wire(_ctrl, "PUT", body: "{}");
        await _ctrl.Editar(1);
        await _ctrl.EditarBulk();
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{}");
        await _ctrl.Reset();
        _stub.Requests.Should().HaveCount(5);
    }
}

[Collection("GatewayProxy")]
public class VehiculosAdminProxyControllerTests
{
    private readonly GatewayStubHandler _stub = new();
    private readonly VehiculosAdminProxyController _ctrl;
    public VehiculosAdminProxyControllerTests()
    {
        VehiculosAdminProxyController._httpClient = new HttpClient(_stub);
        _ctrl = new VehiculosAdminProxyController(GatewayTestHelpers.EmptyConfig());
    }

    [Fact]
    public async Task TodosLosEndpoints()
    {
        GatewayTestHelpers.Wire(_ctrl, "GET");
        await _ctrl.Listar();
        await _ctrl.Obtener(1);
        GatewayTestHelpers.Wire(_ctrl, "POST", body: "{}");
        await _ctrl.Crear();
        await _ctrl.Reactivar(1);
        await _ctrl.Asignar(1);
        await _ctrl.Importar();
        GatewayTestHelpers.Wire(_ctrl, "PUT", body: "{}");
        await _ctrl.Editar(1);
        GatewayTestHelpers.Wire(_ctrl, "DELETE");
        await _ctrl.Desactivar(1);
        _stub.Requests.Should().HaveCount(8);
    }
}
