using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Intranet.Data;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class OficinaRepositoryTests
{
    private static (OficinaRepository repo, IntranetDbContext db) Crear()
    {
        var opt = new DbContextOptionsBuilder<IntranetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new IntranetDbContext(opt);
        return (new OficinaRepository(db), db);
    }

    [Fact]
    public async Task GetAllAsync_PorDefectoExcluyeInactivas()
    {
        var (repo, db) = Crear();
        db.OficinasPostales.AddRange(
            new OficinaPostal { Id = 1, Activo = true, Nombre = "A" },
            new OficinaPostal { Id = 2, Activo = false, Nombre = "B" });
        await db.SaveChangesAsync();

        var r = await repo.GetAllAsync();
        r.Should().ContainSingle();
        r[0].Id.Should().Be(1);
    }

    [Fact]
    public async Task GetAllAsync_IncluirInactivas_DevuelveTodas()
    {
        var (repo, db) = Crear();
        db.OficinasPostales.AddRange(
            new OficinaPostal { Id = 1, Activo = true },
            new OficinaPostal { Id = 2, Activo = false });
        await db.SaveChangesAsync();
        (await repo.GetAllAsync(incluirInactivas: true)).Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_Existe_Devuelve()
    {
        var (repo, db) = Crear();
        db.OficinasPostales.Add(new OficinaPostal { Id = 1, Nombre = "X" });
        await db.SaveChangesAsync();
        (await repo.GetByIdAsync(1))!.Nombre.Should().Be("X");
    }

    [Fact]
    public async Task ExistsAsync_DevuelveBool()
    {
        var (repo, db) = Crear();
        db.OficinasPostales.Add(new OficinaPostal { Id = 1 });
        await db.SaveChangesAsync();
        (await repo.ExistsAsync(1)).Should().BeTrue();
        (await repo.ExistsAsync(99)).Should().BeFalse();
    }

    [Fact]
    public async Task NextIdAsync_Vacio_Devuelve1001()
    {
        var (repo, _) = Crear();
        (await repo.NextIdAsync()).Should().Be(1001);
    }

    [Fact]
    public async Task NextIdAsync_ConRegistros_DevuelveMaxMas1()
    {
        var (repo, db) = Crear();
        db.OficinasPostales.AddRange(
            new OficinaPostal { Id = 1500 },
            new OficinaPostal { Id = 1700 });
        await db.SaveChangesAsync();
        (await repo.NextIdAsync()).Should().Be(1701);
    }

    [Fact]
    public async Task CreateAsync_Persiste()
    {
        var (repo, db) = Crear();
        var o = await repo.CreateAsync(new OficinaPostal { Id = 1, Nombre = "Nueva" });
        o.Nombre.Should().Be("Nueva");
        (await db.OficinasPostales.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_Persiste()
    {
        var (repo, db) = Crear();
        var o = new OficinaPostal { Id = 1, Nombre = "Old" };
        db.OficinasPostales.Add(o);
        await db.SaveChangesAsync();
        o.Nombre = "New";
        await repo.UpdateAsync(o);
        (await db.OficinasPostales.SingleAsync()).Nombre.Should().Be("New");
    }

    [Fact]
    public async Task CountOperariosActivosAsync_FiltraInactivos()
    {
        var (repo, db) = Crear();
        db.OperariosOficina.AddRange(
            new OperarioOficina { OficinaJsonId = 1, Activo = true, IdentityUserId = "a" },
            new OperarioOficina { OficinaJsonId = 1, Activo = false, IdentityUserId = "b" },
            new OperarioOficina { OficinaJsonId = 2, Activo = true, IdentityUserId = "c" });
        await db.SaveChangesAsync();
        (await repo.CountOperariosActivosAsync(1)).Should().Be(1);
    }
}
