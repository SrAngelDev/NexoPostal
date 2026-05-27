using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Services;
using Xunit;

namespace Nexopostal.Tests.Intranet;

[Collection("OficinasJsonIntranet")]
public class OficinasJsonServiceTests : IDisposable
{
    private readonly string _root;
    private readonly ServiceProvider _sp;

    public OficinasJsonServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "nxp-intranet-of-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_root, "Data"));
        File.WriteAllText(Path.Combine(_root, "Data", "oficinas.json"), Fixture);

        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<IntranetDbContext>(o => o.UseInMemoryDatabase(dbName));
        _sp = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _sp.Dispose();
        try { Directory.Delete(_root, true); } catch { /* ignore */ }
    }

    private OficinasJsonService Create()
    {
        var env = new Mock<IWebHostEnvironment>();
        env.SetupGet(e => e.ContentRootPath).Returns(_root);
        var svc = new OficinasJsonService(
            NullLogger<OficinasJsonService>.Instance,
            _sp.GetRequiredService<IServiceScopeFactory>(),
            env.Object);
        svc.Invalidar();
        return svc;
    }

    private void SeedDb(params OficinaPostal[] oficinas)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IntranetDbContext>();
        db.OficinasPostales.AddRange(oficinas);
        db.SaveChanges();
    }

    private const string Fixture = """
    {
      "@graph": [
        {
          "id": "1",
          "title": "Oficina Sol",
          "schedule": "Lu-Vi: 09:00-14:00",
          "services": "Envíos",
          "address": { "locality": "Madrid", "postal-code": "28013", "street-address": "Sol 1" },
          "location": { "latitude": 40.4168, "longitude": -3.7038 }
        },
        {
          "id": "2",
          "title": "Oficina Chamberí",
          "address": { "locality": "Madrid", "postal-code": "28010", "street-address": "Génova 10" },
          "location": { "latitude": 40.4280, "longitude": -3.6970 }
        },
        {
          "id": "3",
          "title": "Oficina Gracia",
          "address": { "locality": "Barcelona", "postal-code": "08012", "street-address": "Gracia 50" },
          "location": { "latitude": 41.4040, "longitude": 2.1530 }
        }
      ]
    }
    """;

    private static OficinaPostal NewOp(int id, string cp, string ciudad = "Madrid", double? lat = null, double? lon = null, bool activo = true)
        => new()
        {
            Id = id,
            Nombre = "Of-" + id,
            Direccion = "Calle " + id,
            CodigoPostal = cp,
            Ciudad = ciudad,
            Activo = activo,
            Latitud = lat,
            Longitud = lon,
            Horario = "Lu-Vi: 09:00-14:00",
            Servicios = "Envíos"
        };

    [Fact]
    public void ObtenerTodas_BdConDatos_DevuelveDeBd()
    {
        SeedDb(NewOp(1001, "28001"), NewOp(1002, "08001", "Barcelona"));
        var todas = Create().ObtenerTodas();
        todas.Should().HaveCount(2);
        todas.Select(o => o.CodigoPostal).Should().Contain("28001");
    }

    [Fact]
    public void ObtenerTodas_BdConSoloInactivos_UsaFallbackJson()
    {
        SeedDb(NewOp(1001, "28001", activo: false));
        var todas = Create().ObtenerTodas();
        todas.Should().HaveCount(3);
    }

    [Fact]
    public void ObtenerTodas_BdVacia_UsaFallbackJson()
    {
        var todas = Create().ObtenerTodas();
        todas.Should().HaveCount(3);
        todas.First(o => o.CodigoPostal == "28013").Nombre.Should().Be("Oficina Sol");
    }

    [Fact]
    public void CargarDesdeJsonFile_LeeJsonDirecto()
    {
        var r = Create().CargarDesdeJsonFile();
        r.Should().HaveCount(3);
        var sol = r.First(o => o.Nombre == "Oficina Sol");
        sol.Latitud.Should().BeApproximately(40.4168, 0.001);
        sol.Servicios.Should().Be("Envíos");
    }

    [Fact]
    public void CargarDesdeJsonFile_SinSchedule_AplicaDefault()
    {
        var chamberi = Create().CargarDesdeJsonFile().First(o => o.Nombre == "Oficina Chamberí");
        chamberi.Horario.Should().Be("Lu-Vi: 09:00-14:00");
        chamberi.Servicios.Should().Be("");
    }

    [Fact]
    public void BuscarPorCodigoPostal_Exacto_DevuelveUna()
    {
        var r = Create().BuscarPorCodigoPostal("28013");
        r.Should().ContainSingle(o => o.Nombre == "Oficina Sol");
    }

    [Fact]
    public void BuscarPorCodigoPostal_SinExacto_BuscaPrefijo3()
    {
        var r = Create().BuscarPorCodigoPostal("28099");
        r.Select(o => o.CodigoPostal).Should().AllSatisfy(c => c.Should().StartWith("280"));
    }

    [Fact]
    public void BuscarPorCodigoPostal_PrefijoCorto()
    {
        var r = Create().BuscarPorCodigoPostal("08");
        r.Should().ContainSingle(o => o.CodigoPostal == "08012");
    }

    [Fact]
    public void BuscarPorTexto_PorCiudad()
    {
        Create().BuscarPorTexto("Barcelona").Should().HaveCount(1);
    }

    [Fact]
    public void BuscarPorTexto_NoEncuentra()
    {
        Create().BuscarPorTexto("ZZZ").Should().BeEmpty();
    }

    [Fact]
    public void ObtenerPorId_Existe()
    {
        Create().ObtenerPorId(1)!.Nombre.Should().Be("Oficina Sol");
    }

    [Fact]
    public void ObtenerPorId_NoExiste_DevuelveNull()
    {
        Create().ObtenerPorId(999).Should().BeNull();
    }

    [Fact]
    public void ResolverOficinaMasCercana_PorCp_Exacto()
    {
        Create().ResolverOficinaMasCercana("28013")!.Nombre.Should().Be("Oficina Sol");
    }

    [Fact]
    public void ResolverOficinaMasCercana_PorCp_Prefijo3()
    {
        var r = Create().ResolverOficinaMasCercana("28099");
        r.Should().NotBeNull();
        r!.CodigoPostal.Should().StartWith("280");
    }

    [Fact]
    public void ResolverOficinaMasCercana_PorCp_Prefijo2()
    {
        var r = Create().ResolverOficinaMasCercana("08999");
        r.Should().NotBeNull();
        r!.CodigoPostal.Should().StartWith("08");
    }

    [Fact]
    public void ResolverOficinaMasCercana_PorCp_NoEncuentra()
    {
        Create().ResolverOficinaMasCercana("99999").Should().BeNull();
    }

    [Fact]
    public void ResolverOficinaMasCercana_ConLatLon_EligeMasCercana()
    {
        // Madrid (cerca de Sol/Chamberí) y Barcelona; pedir CP 28999 con coords cerca de Sol
        var r = Create().ResolverOficinaMasCercana("28999", 40.4168, -3.7038);
        r!.Nombre.Should().Be("Oficina Sol");
    }

    [Fact]
    public void ResolverOficinaMasCercana_ConLatLon_NoEncuentra()
    {
        Create().ResolverOficinaMasCercana("99999", 0, 0).Should().BeNull();
    }

    [Fact]
    public void CalcularDistanciaKm_MismoPunto_Cero()
    {
        OficinasJsonService.CalcularDistanciaKm(40, -3, 40, -3).Should().Be(0);
    }

    [Fact]
    public void CalcularDistanciaKm_MadridBarcelona_Aprox500km()
    {
        var d = OficinasJsonService.CalcularDistanciaKm(40.4168, -3.7038, 41.3851, 2.1734);
        d.Should().BeInRange(490, 520);
    }

    [Fact]
    public void Invalidar_LimpiaCacheYRecargaEnSiguiente()
    {
        var s = Create();
        s.ObtenerTodas().Should().NotBeEmpty();
        s.Invalidar();
        s.ObtenerTodas().Should().NotBeEmpty();
    }
}
