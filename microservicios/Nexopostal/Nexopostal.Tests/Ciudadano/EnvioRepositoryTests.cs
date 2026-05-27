using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Ciudadano.Data;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

public class EnvioRepositoryTests
{
    private static (EnvioRepository repo, CiudadanoDbContext db) Crear()
    {
        var opts = new DbContextOptionsBuilder<CiudadanoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new CiudadanoDbContext(opts);
        return (new EnvioRepository(db), db);
    }

    private static Envio NuevoEnvio(
        string tracking = "NXP-001ES",
        string expedicion = "NXI-001",
        string userId = "u1",
        string? stripe = null,
        EstadoEnvio estado = EstadoEnvio.Admitido,
        EstadoInterno estadoInterno = EstadoInterno.PendienteRecogida,
        string cpDestino = "28001",
        bool pagado = true) => new()
    {
        NumeroSeguimiento = tracking,
        NumeroExpedicion = expedicion,
        IdentityUserId = userId,
        StripeSessionId = stripe,
        EstadoActual = estado,
        EstadoInternoActual = estadoInterno,
        CodigoPostalDestino = cpDestino,
        CodigoPostalOrigen = "08001",
        NombreRemitente = "R",
        EmailRemitente = "r@x.es",
        NombreDestinatario = "D",
        TipoTarifa = "Estandar",
        CosteCalculado = 5m,
        FechaCreacion = DateTime.UtcNow,
        Pagado = pagado
    };

    // ── GetByTrackingAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetByTrackingAsync_Existe_Devuelve()
    {
        var (repo, db) = Crear();
        db.Envios.Add(NuevoEnvio("NXP-A"));
        await db.SaveChangesAsync();

        var r = await repo.GetByTrackingAsync("NXP-A");
        r.Should().NotBeNull();
        r!.NumeroSeguimiento.Should().Be("NXP-A");
    }

    [Fact]
    public async Task GetByTrackingAsync_NoExiste_Null()
    {
        var (repo, _) = Crear();
        (await repo.GetByTrackingAsync("NOPE")).Should().BeNull();
    }

    // ── GetByExpedicionAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetByExpedicionAsync_Existe_Devuelve()
    {
        var (repo, db) = Crear();
        db.Envios.Add(NuevoEnvio(expedicion: "NXI-XYZ"));
        await db.SaveChangesAsync();

        var r = await repo.GetByExpedicionAsync("NXI-XYZ");
        r.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByExpedicionAsync_NoExiste_Null()
    {
        var (repo, _) = Crear();
        (await repo.GetByExpedicionAsync("NXI-NOPE")).Should().BeNull();
    }

    // ── GetByTrackingAndUserAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetByTrackingAndUserAsync_Coincide_Devuelve()
    {
        var (repo, db) = Crear();
        db.Envios.Add(NuevoEnvio("NXP-B", userId: "u1"));
        await db.SaveChangesAsync();

        var r = await repo.GetByTrackingAndUserAsync("NXP-B", "u1");
        r.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByTrackingAndUserAsync_OtroUsuario_Null()
    {
        var (repo, db) = Crear();
        db.Envios.Add(NuevoEnvio("NXP-B", userId: "u1"));
        await db.SaveChangesAsync();

        (await repo.GetByTrackingAndUserAsync("NXP-B", "u2")).Should().BeNull();
    }

    // ── GetByStripeSessionAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByStripeSessionAsync_Existe_Devuelve()
    {
        var (repo, db) = Crear();
        db.Envios.Add(NuevoEnvio(stripe: "sess_abc"));
        await db.SaveChangesAsync();

        var r = await repo.GetByStripeSessionAsync("sess_abc");
        r.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByStripeSessionAsync_NoExiste_Null()
    {
        var (repo, _) = Crear();
        (await repo.GetByStripeSessionAsync("sess_nope")).Should().BeNull();
    }

    // ── GetByUserAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByUserAsync_FiltraPorUsuarioYExcluyePendientePago()
    {
        var (repo, db) = Crear();
        db.Envios.AddRange(
            NuevoEnvio("A", userId: "u1", estado: EstadoEnvio.Admitido),
            NuevoEnvio("B", userId: "u1", estado: EstadoEnvio.PendientePago),
            NuevoEnvio("C", expedicion: "E3", userId: "u2", estado: EstadoEnvio.Admitido));
        await db.SaveChangesAsync();

        var lista = await repo.GetByUserAsync("u1");
        lista.Should().ContainSingle();
        lista[0].NumeroSeguimiento.Should().Be("A");
    }

    // ── GetByEstadoInternoAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetByEstadoInternoAsync_FiltranCorrectamente()
    {
        var (repo, db) = Crear();
        db.Envios.AddRange(
            NuevoEnvio("A", cpDestino: "28001", estadoInterno: EstadoInterno.EnReparto),
            NuevoEnvio("B", expedicion: "E2", cpDestino: "28002", estadoInterno: EstadoInterno.EnReparto),
            NuevoEnvio("C", expedicion: "E3", cpDestino: "28001", estadoInterno: EstadoInterno.EntregadoEnDomicilio));
        await db.SaveChangesAsync();

        var r1 = await repo.GetByEstadoInternoAsync(EstadoInterno.EnReparto, null);
        r1.Should().HaveCount(2);

        var r2 = await repo.GetByEstadoInternoAsync(EstadoInterno.EnReparto, "28001");
        r2.Should().ContainSingle();

        var r3 = await repo.GetByEstadoInternoAsync(null, "28001");
        // Devuelve los dos con cpDestino=28001 (A y C), excluyendo PendientePago
        r3.Should().HaveCount(2);
    }

    // ── CreateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersisteLaEntidad()
    {
        var (repo, db) = Crear();
        var e = NuevoEnvio("NXP-NEW");

        var creado = await repo.CreateAsync(e);

        creado.NumeroSeguimiento.Should().Be("NXP-NEW");
        (await db.Envios.CountAsync()).Should().Be(1);
    }

    // ── UpdateAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ModificaEntidad()
    {
        var (repo, db) = Crear();
        var e = NuevoEnvio("NXP-UPD");
        db.Envios.Add(e);
        await db.SaveChangesAsync();

        e.EstadoActual = EstadoEnvio.Entregado;
        await repo.UpdateAsync(e);

        var actualizado = await db.Envios.SingleAsync();
        actualizado.EstadoActual.Should().Be(EstadoEnvio.Entregado);
    }

    // ── ExistsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task ExistsAsync_Existe_True()
    {
        var (repo, db) = Crear();
        db.Envios.Add(NuevoEnvio("NXP-EX"));
        await db.SaveChangesAsync();

        (await repo.ExistsAsync("NXP-EX")).Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NoExiste_False()
    {
        var (repo, _) = Crear();
        (await repo.ExistsAsync("NXP-NO")).Should().BeFalse();
    }

    // ── GetAdminListAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetAdminListAsync_SinFiltros_DevuelveTodos()
    {
        var (repo, db) = Crear();
        db.Envios.AddRange(
            NuevoEnvio("A"),
            NuevoEnvio("B", expedicion: "E2"),
            NuevoEnvio("C", expedicion: "E3"));
        await db.SaveChangesAsync();

        var r = await repo.GetAdminListAsync(null, null, null, null, null, null, null);
        r.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAdminListAsync_FiltroEstado_FiltraCorrectamente()
    {
        var (repo, db) = Crear();
        db.Envios.AddRange(
            NuevoEnvio("A", estado: EstadoEnvio.Admitido),
            NuevoEnvio("B", expedicion: "E2", estado: EstadoEnvio.Entregado));
        await db.SaveChangesAsync();

        var r = await repo.GetAdminListAsync(EstadoEnvio.Admitido, null, null, null, null, null, null);
        r.Should().ContainSingle(e => e.NumeroSeguimiento == "A");
    }

    [Fact]
    public async Task GetAdminListAsync_FiltroEstadoInterno_FiltraCorrectamente()
    {
        var (repo, db) = Crear();
        db.Envios.AddRange(
            NuevoEnvio("A", estadoInterno: EstadoInterno.EnReparto),
            NuevoEnvio("B", expedicion: "E2", estadoInterno: EstadoInterno.EntregadoEnDomicilio));
        await db.SaveChangesAsync();

        var r = await repo.GetAdminListAsync(null, EstadoInterno.EnReparto, null, null, null, null, null);
        r.Should().ContainSingle();
    }

    [Fact]
    public async Task GetAdminListAsync_FiltroFechas_FiltraCorrectamente()
    {
        var (repo, db) = Crear();
        var ayer = DateTime.UtcNow.AddDays(-1);
        var manana = DateTime.UtcNow.AddDays(1);
        var hace5dias = DateTime.UtcNow.AddDays(-5);

        var envioReciente = NuevoEnvio("REC");
        envioReciente.FechaCreacion = DateTime.UtcNow;
        var envioViejo = NuevoEnvio("OLD", expedicion: "E2");
        envioViejo.FechaCreacion = hace5dias;
        db.Envios.AddRange(envioReciente, envioViejo);
        await db.SaveChangesAsync();

        var r = await repo.GetAdminListAsync(null, null, ayer, manana, null, null, null);
        r.Should().ContainSingle(e => e.NumeroSeguimiento == "REC");
    }

    [Fact]
    public async Task GetAdminListAsync_FiltroPagado_FiltraCorrectamente()
    {
        var (repo, db) = Crear();
        db.Envios.AddRange(
            NuevoEnvio("A", pagado: true),
            NuevoEnvio("B", expedicion: "E2", pagado: false));
        await db.SaveChangesAsync();

        var r = await repo.GetAdminListAsync(null, null, null, null, null, null, pagado: true);
        r.Should().ContainSingle(e => e.NumeroSeguimiento == "A");
    }

    [Fact]
    public async Task GetAdminListAsync_FiltroCp_FiltraCorrectamente()
    {
        var (repo, db) = Crear();
        db.Envios.AddRange(
            NuevoEnvio("A", cpDestino: "28001"),
            NuevoEnvio("B", expedicion: "E2", cpDestino: "08001"));
        await db.SaveChangesAsync();

        var r = await repo.GetAdminListAsync(null, null, null, null, null, "28001", null);
        r.Should().ContainSingle(e => e.NumeroSeguimiento == "A");
    }

    [Fact]
    public async Task GetAdminListAsync_LimiteMenorQue1_UsaDefault500()
    {
        var (repo, db) = Crear();
        for (int i = 0; i < 10; i++)
            db.Envios.Add(NuevoEnvio($"E{i:D3}", expedicion: $"EXP{i:D3}"));
        await db.SaveChangesAsync();

        // Limit = 0 → debe aplicar el default 500
        var r = await repo.GetAdminListAsync(null, null, null, null, null, null, null, limit: 0);
        r.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetAdminListAsync_LimitePequeno_LimitaResultados()
    {
        var (repo, db) = Crear();
        for (int i = 0; i < 10; i++)
            db.Envios.Add(NuevoEnvio($"E{i:D3}", expedicion: $"EXP{i:D3}"));
        await db.SaveChangesAsync();

        var r = await repo.GetAdminListAsync(null, null, null, null, null, null, null, limit: 3);
        r.Should().HaveCount(3);
    }

    // ── CountByEstadoAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CountByEstadoAsync_CuentaCorrectamente()
    {
        var (repo, db) = Crear();
        db.Envios.AddRange(
            NuevoEnvio("A", estado: EstadoEnvio.Admitido),
            NuevoEnvio("B", expedicion: "E2", estado: EstadoEnvio.Admitido),
            NuevoEnvio("C", expedicion: "E3", estado: EstadoEnvio.Entregado));
        await db.SaveChangesAsync();

        (await repo.CountByEstadoAsync(EstadoEnvio.Admitido)).Should().Be(2);
        (await repo.CountByEstadoAsync(EstadoEnvio.Entregado)).Should().Be(1);
        (await repo.CountByEstadoAsync(EstadoEnvio.EnTransito)).Should().Be(0);
    }
}
