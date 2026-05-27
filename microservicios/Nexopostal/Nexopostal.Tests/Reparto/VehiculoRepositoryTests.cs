using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Xunit;

namespace Nexopostal.Tests.Reparto;

public class VehiculoRepositoryTests
{
    private static (VehiculoRepository repo, RepartoDbContext db) Crear()
    {
        var opt = new DbContextOptionsBuilder<RepartoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new RepartoDbContext(opt);
        return (new VehiculoRepository(db), db);
    }

    private static Vehiculo NuevoVeh(string mat, bool activo = true, int? oficina = null, int? repId = null) =>
        new() { Matricula = mat, Activo = activo, OficinaJsonId = oficina, RepartidorAsignadoId = repId };

    [Fact]
    public async Task GetAllAsync_PorDefectoExcluyeInactivos()
    {
        var (repo, db) = Crear();
        db.Vehiculos.AddRange(NuevoVeh("ZZZ-1", true), NuevoVeh("AAA-1", false));
        await db.SaveChangesAsync();
        var r = await repo.GetAllAsync();
        r.Should().ContainSingle();
        r[0].Matricula.Should().Be("ZZZ-1");
    }

    [Fact]
    public async Task GetAllAsync_IncluirInactivos_OrdenaPorMatricula()
    {
        var (repo, db) = Crear();
        db.Vehiculos.AddRange(NuevoVeh("ZZZ", true), NuevoVeh("AAA", false));
        await db.SaveChangesAsync();
        var r = await repo.GetAllAsync(incluirInactivos: true);
        r.Should().HaveCount(2);
        r[0].Matricula.Should().Be("AAA");
    }

    [Fact]
    public async Task GetAllAsync_FiltraPorOficina()
    {
        var (repo, db) = Crear();
        db.Vehiculos.AddRange(NuevoVeh("A", true, oficina: 1), NuevoVeh("B", true, oficina: 2));
        await db.SaveChangesAsync();
        (await repo.GetAllAsync(oficinaJsonId: 1)).Should().ContainSingle().Which.Matricula.Should().Be("A");
    }

    [Fact]
    public async Task GetAllAsync_FiltraPorRepartidor()
    {
        var (repo, db) = Crear();
        db.Vehiculos.AddRange(NuevoVeh("A", true, repId: 7), NuevoVeh("B", true, repId: 8));
        await db.SaveChangesAsync();
        (await repo.GetAllAsync(repartidorId: 7)).Should().ContainSingle().Which.Matricula.Should().Be("A");
    }

    [Fact]
    public async Task GetByIdAsync_DevuelveOrNull()
    {
        var (repo, db) = Crear();
        var v = NuevoVeh("A");
        db.Vehiculos.Add(v);
        await db.SaveChangesAsync();
        (await repo.GetByIdAsync(v.Id))!.Matricula.Should().Be("A");
        (await repo.GetByIdAsync(9999)).Should().BeNull();
    }

    [Fact]
    public async Task GetByMatriculaAsync_DevuelveOrNull()
    {
        var (repo, db) = Crear();
        db.Vehiculos.Add(NuevoVeh("M-1"));
        await db.SaveChangesAsync();
        (await repo.GetByMatriculaAsync("M-1"))!.Matricula.Should().Be("M-1");
        (await repo.GetByMatriculaAsync("X")).Should().BeNull();
    }

    [Fact]
    public async Task GetByRepartidorAsync_SoloActivos()
    {
        var (repo, db) = Crear();
        db.Vehiculos.AddRange(
            NuevoVeh("A", activo: false, repId: 5),
            NuevoVeh("B", activo: true, repId: 5));
        await db.SaveChangesAsync();
        var r = await repo.GetByRepartidorAsync(5);
        r!.Matricula.Should().Be("B");
    }

    [Fact]
    public async Task MatriculaExistsAsync_Y_ConExclusion()
    {
        var (repo, db) = Crear();
        var v = NuevoVeh("XYZ");
        db.Vehiculos.Add(v);
        await db.SaveChangesAsync();
        (await repo.MatriculaExistsAsync("XYZ")).Should().BeTrue();
        (await repo.MatriculaExistsAsync("XYZ", excluyendoId: v.Id)).Should().BeFalse();
        (await repo.MatriculaExistsAsync("OTRO")).Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_Persiste()
    {
        var (repo, db) = Crear();
        var v = await repo.CreateAsync(NuevoVeh("NEW"));
        v.Id.Should().BeGreaterThan(0);
        (await db.Vehiculos.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_Persiste()
    {
        var (repo, db) = Crear();
        var v = NuevoVeh("OLD");
        db.Vehiculos.Add(v);
        await db.SaveChangesAsync();
        v.Marca = "Iveco";
        await repo.UpdateAsync(v);
        (await db.Vehiculos.SingleAsync()).Marca.Should().Be("Iveco");
    }
}
