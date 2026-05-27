using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Ciudadano.Services;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

public class OficinasJsonServiceTests : IDisposable
{
    private readonly string _root;

    public OficinasJsonServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nxp-oficinas-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Data"));
        File.WriteAllText(Path.Combine(_root, "Data", "oficinas.json"), Fixture);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    private OficinasJsonService Create()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(_root);
        return new OficinasJsonService(NullLogger<OficinasJsonService>.Instance, env.Object);
    }

    private const string Fixture = """
    {
      "@graph": [
        {
          "id": "1",
          "title": "Oficina Sol",
          "schedule": "Lu-Vi: 09:00-14:00",
          "services": "Envíos, Recogidas",
          "address": { "locality": "Madrid", "postal-code": "28013", "street-address": "Puerta del Sol 1" },
          "location": { "latitude": 40.4168, "longitude": -3.7038 }
        },
        {
          "id": "2",
          "title": "Oficina Chamberí",
          "address": { "locality": "Madrid", "postal-code": "28010", "street-address": "Calle Génova 10" },
          "location": { "latitude": 40.4280, "longitude": -3.6970 }
        },
        {
          "id": "3",
          "title": "Oficina Gracia",
          "address": { "locality": "Barcelona", "postal-code": "08012", "street-address": "Carrer Gran de Gracia 50" },
          "location": { "latitude": 41.4040, "longitude": 2.1530 }
        },
        {
          "id": "X",
          "title": "Oficina Sin Numero",
          "address": { "locality": "Sevilla", "postal-code": "41001", "street-address": "Av. Reyes Católicos" },
          "location": { "latitude": 37.3886, "longitude": -5.9823 }
        }
      ]
    }
    """;

    [Fact]
    public void ObtenerTodas_DevuelveListaCargada()
    {
        var s = Create();
        s.ObtenerTodas().Should().HaveCount(4);
    }

    [Fact]
    public void ObtenerTodas_SegundaLlamada_UsaCache()
    {
        var s = Create();
        var a = s.ObtenerTodas();
        var b = s.ObtenerTodas();
        b.Should().BeSameAs(a);
    }

    [Fact]
    public void ObtenerTodas_MapeaCamposCorrectamente()
    {
        var primera = Create().ObtenerTodas().First(o => o.Nombre == "Oficina Sol");
        primera.Id.Should().Be(1);
        primera.Direccion.Should().Be("Puerta del Sol 1");
        primera.CodigoPostal.Should().Be("28013");
        primera.Ciudad.Should().Be("Madrid");
        primera.Provincia.Should().Be("Madrid");
        primera.Horario.Should().Be("Lu-Vi: 09:00-14:00");
        primera.Servicios.Should().Be("Envíos, Recogidas");
        primera.Activa.Should().BeTrue();
        primera.Latitud.Should().BeApproximately(40.4168, 0.001);
        primera.Telefono.Should().Be("912 197 197");
    }

    [Fact]
    public void ObtenerTodas_SinSchedule_AplicaDefault()
    {
        var sin = Create().ObtenerTodas().First(o => o.Nombre == "Oficina Chamberí");
        sin.Horario.Should().Be("Lu-Vi: 09:00-14:00");
        sin.Servicios.Should().Be("");
    }

    [Fact]
    public void ObtenerTodas_IdNoNumerico_UsaCero()
    {
        Create().ObtenerTodas().First(o => o.Nombre == "Oficina Sin Numero").Id.Should().Be(0);
    }

    [Fact]
    public void BuscarPorCodigoPostal_Exacto_DevuelveUna()
    {
        var r = Create().BuscarPorCodigoPostal("28013");
        r.Should().HaveCount(1);
        r[0].Nombre.Should().Be("Oficina Sol");
    }

    [Fact]
    public void BuscarPorCodigoPostal_SinExacto_BuscaPorPrefijo()
    {
        var r = Create().BuscarPorCodigoPostal("28099");
        r.Select(o => o.CodigoPostal).Should().AllSatisfy(c => c.Should().StartWith("280"));
        r.Should().HaveCountGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void BuscarPorCodigoPostal_PrefijoCorto_FuncionaConCadenaCorta()
    {
        var r = Create().BuscarPorCodigoPostal("28");
        r.Should().HaveCount(2);
        r.Select(o => o.CodigoPostal).Should().AllSatisfy(c => c.Should().StartWith("28"));
    }

    [Fact]
    public void BuscarPorTexto_PorCiudad_DevuelveOficinasDeEsaCiudad()
    {
        var r = Create().BuscarPorTexto("Barcelona");
        r.Should().HaveCount(1);
        r[0].Ciudad.Should().Be("Barcelona");
    }

    [Fact]
    public void BuscarPorTexto_PorNombre_Encuentra()
    {
        Create().BuscarPorTexto("Sol").Should().ContainSingle(o => o.Nombre == "Oficina Sol");
    }

    [Fact]
    public void BuscarPorTexto_PorDireccion_Encuentra()
    {
        Create().BuscarPorTexto("Génova").Should().NotBeEmpty();
    }

    [Fact]
    public void BuscarPorTexto_NoEncuentra_DevuelveVacio()
    {
        Create().BuscarPorTexto("ZZZZZ").Should().BeEmpty();
    }
}
