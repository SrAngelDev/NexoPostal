using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

public class ClientePerfilRepositoryTests
{
    private static (ClientePerfilRepository repo, CiudadanoDbContext db) Crear()
    {
        var opt = new DbContextOptionsBuilder<CiudadanoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CiudadanoDbContext(opt);
        return (new ClientePerfilRepository(db), db);
    }

    [Fact]
    public async Task GetByUserIdAsync_NoExiste_DevuelveNull()
    {
        var (repo, _) = Crear();
        (await repo.GetByUserIdAsync("nope")).Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_IncluyeAgenda()
    {
        var (repo, db) = Crear();
        var p = new ClientePerfil { IdentityUserId = "u1", DNI = "X" };
        p.Agenda.Add(new DireccionFavorita { Alias = "Casa" });
        db.ClientePerfiles.Add(p);
        await db.SaveChangesAsync();

        var r = await repo.GetByUserIdAsync("u1");
        r!.DNI.Should().Be("X");
        r.Agenda.Should().ContainSingle();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_Nuevo_LoCrea()
    {
        var (repo, db) = Crear();
        var p = new ClientePerfil { IdentityUserId = "u1", DNI = "111" };
        var creado = await repo.CreateOrUpdateAsync(p);
        creado.IdentityUserId.Should().Be("u1");
        (await db.ClientePerfiles.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task CreateOrUpdateAsync_Existente_Actualiza()
    {
        var (repo, db) = Crear();
        db.ClientePerfiles.Add(new ClientePerfil { IdentityUserId = "u1", DNI = "OLD", Telefono = "1" });
        await db.SaveChangesAsync();

        await repo.CreateOrUpdateAsync(new ClientePerfil
        {
            IdentityUserId = "u1",
            DNI = "NEW",
            Telefono = "2",
            DireccionPredeterminada = "calle nueva"
        });

        var fila = await db.ClientePerfiles.SingleAsync();
        fila.DNI.Should().Be("NEW");
        fila.Telefono.Should().Be("2");
        fila.DireccionPredeterminada.Should().Be("calle nueva");
    }

    [Fact]
    public async Task GetDireccionesAsync_FiltraYOrdenaPorAlias()
    {
        var (repo, db) = Crear();
        db.DireccionesFavoritas.AddRange(
            new DireccionFavorita { Alias = "Trabajo", ClientePerfilId = 1 },
            new DireccionFavorita { Alias = "Casa", ClientePerfilId = 1 },
            new DireccionFavorita { Alias = "Otro", ClientePerfilId = 2 });
        await db.SaveChangesAsync();

        var r = await repo.GetDireccionesAsync(1);
        r.Should().HaveCount(2);
        r[0].Alias.Should().Be("Casa");
        r[1].Alias.Should().Be("Trabajo");
    }

    [Fact]
    public async Task GetDireccionByIdAsync_ClienteIncorrecto_DevuelveNull()
    {
        var (repo, db) = Crear();
        db.DireccionesFavoritas.Add(new DireccionFavorita { Id = 1, ClientePerfilId = 5, Alias = "A" });
        await db.SaveChangesAsync();
        (await repo.GetDireccionByIdAsync(1, 99)).Should().BeNull();
    }

    [Fact]
    public async Task AddDireccionAsync_Persiste()
    {
        var (repo, db) = Crear();
        var d = await repo.AddDireccionAsync(new DireccionFavorita { ClientePerfilId = 1, Alias = "Casa" });
        d.Id.Should().BeGreaterThan(0);
        (await db.DireccionesFavoritas.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateDireccionAsync_Persiste()
    {
        var (repo, db) = Crear();
        var d = new DireccionFavorita { ClientePerfilId = 1, Alias = "Casa" };
        db.DireccionesFavoritas.Add(d);
        await db.SaveChangesAsync();

        d.Alias = "Hogar";
        await repo.UpdateDireccionAsync(d);

        (await db.DireccionesFavoritas.SingleAsync()).Alias.Should().Be("Hogar");
    }

    [Fact]
    public async Task DeleteDireccionAsync_NoExiste_DevuelveFalse()
    {
        var (repo, _) = Crear();
        (await repo.DeleteDireccionAsync(99, 1)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteDireccionAsync_Existe_Elimina()
    {
        var (repo, db) = Crear();
        db.DireccionesFavoritas.Add(new DireccionFavorita { Id = 1, ClientePerfilId = 5 });
        await db.SaveChangesAsync();
        (await repo.DeleteDireccionAsync(1, 5)).Should().BeTrue();
        (await db.DireccionesFavoritas.CountAsync()).Should().Be(0);
    }
}
