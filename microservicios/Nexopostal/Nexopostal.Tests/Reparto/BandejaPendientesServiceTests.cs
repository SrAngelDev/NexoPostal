using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Hubs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Services;
using Xunit;

namespace Nexopostal.Tests.Reparto;

public class BandejaPendientesServiceTests
{
    private static (BandejaPendientesService svc, RepartoDbContext db, Mock<IRepartoService> reparto) Crear()
    {
        var options = new DbContextOptionsBuilder<RepartoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new RepartoDbContext(options);
        var reparto = new Mock<IRepartoService>();
        var hub = new Mock<IHubContext<RepartoHub>>();
        var svc = new BandejaPendientesService(db, reparto.Object, hub.Object, NullLogger<BandejaPendientesService>.Instance);
        return (svc, db, reparto);
    }

    [Fact]
    public async Task RegistrarPaqueteAsync_SinExpedicion_DevuelveSuccessFalse()
    {
        var (svc, _, _) = Crear();
        var r = await svc.RegistrarPaqueteAsync(new RegistrarPaqueteBandejaRequestDto { NumeroExpedicion = "" });
        r.Success.Should().BeFalse();
    }

    [Fact]
    public async Task RegistrarPaqueteAsync_Nuevo_CreaYDevuelveIdempotenteFalse()
    {
        var (svc, db, _) = Crear();
        var r = await svc.RegistrarPaqueteAsync(new RegistrarPaqueteBandejaRequestDto
        {
            NumeroExpedicion = "nxi-001",
            CtaId = 1,
            CtaCodigo = "CTA-MAD",
            NombreDestinatario = "Ada",
            DireccionEntrega = "C/ Sol 1",
            CodigoPostalDestino = "28013",
            EsUrgente = true
        });
        r.Success.Should().BeTrue();
        r.Idempotente.Should().BeFalse();
        var fila = await db.PaquetesPendientesReparto.SingleAsync();
        fila.NumeroExpedicion.Should().Be("NXI-001");
        fila.NumeroSeguimiento.Should().Be("NXI-001"); // fallback
        fila.EsUrgente.Should().BeTrue();
    }

    [Fact]
    public async Task RegistrarPaqueteAsync_YaExisteSinAsignar_RefrescaYDevuelveIdempotente()
    {
        var (svc, db, _) = Crear();
        db.PaquetesPendientesReparto.Add(new PaquetePendienteReparto
        {
            NumeroExpedicion = "NXI-002",
            NumeroSeguimiento = "NXP-002",
            CtaId = 1,
            CtaCodigo = "OLD",
            DireccionEntrega = "vieja",
            NombreDestinatario = "Pepe",
            CodigoPostalDestino = "28001"
        });
        await db.SaveChangesAsync();

        var r = await svc.RegistrarPaqueteAsync(new RegistrarPaqueteBandejaRequestDto
        {
            NumeroExpedicion = "NXI-002",
            CtaId = 2,
            CtaCodigo = "NEW",
            DireccionEntrega = "nueva",
            EsUrgente = true,
            Observaciones = "obs"
        });
        r.Success.Should().BeTrue();
        r.Idempotente.Should().BeTrue();
        var fila = await db.PaquetesPendientesReparto.SingleAsync();
        fila.CtaCodigo.Should().Be("NEW");
        fila.DireccionEntrega.Should().Be("nueva");
        fila.EsUrgente.Should().BeTrue();
        fila.Observaciones.Should().Be("obs");
    }

    [Fact]
    public async Task RegistrarPaqueteAsync_YaExisteAsignado_NoModifica()
    {
        var (svc, db, _) = Crear();
        db.PaquetesPendientesReparto.Add(new PaquetePendienteReparto
        {
            NumeroExpedicion = "NXI-003",
            NumeroSeguimiento = "NXP-003",
            CtaId = 1,
            CtaCodigo = "OLD",
            DireccionEntrega = "x",
            NombreDestinatario = "p",
            CodigoPostalDestino = "28001",
            AsignadoARutaId = 99
        });
        await db.SaveChangesAsync();

        var r = await svc.RegistrarPaqueteAsync(new RegistrarPaqueteBandejaRequestDto
        {
            NumeroExpedicion = "nxi-003",
            CtaId = 2,
            CtaCodigo = "NEW",
            DireccionEntrega = "nueva"
        });
        r.Idempotente.Should().BeTrue();
        r.Message.Should().Contain("ruta");
        var fila = await db.PaquetesPendientesReparto.SingleAsync();
        fila.CtaCodigo.Should().Be("OLD");
        fila.DireccionEntrega.Should().Be("x");
    }

    [Fact]
    public async Task ListarPendientesAsync_FiltraPorCtaYExcluyeAsignados_PorDefecto()
    {
        var (svc, db, _) = Crear();
        db.PaquetesPendientesReparto.AddRange(
            new PaquetePendienteReparto { NumeroExpedicion = "A", CtaId = 1, EsUrgente = false, FechaRegistro = DateTime.UtcNow.AddMinutes(-5) },
            new PaquetePendienteReparto { NumeroExpedicion = "B", CtaId = 1, EsUrgente = true, FechaRegistro = DateTime.UtcNow },
            new PaquetePendienteReparto { NumeroExpedicion = "C", CtaId = 1, AsignadoARutaId = 99 },
            new PaquetePendienteReparto { NumeroExpedicion = "D", CtaId = 2 });
        await db.SaveChangesAsync();

        var r = await svc.ListarPendientesAsync(1);
        r.Should().HaveCount(2);
        r.First().NumeroExpedicion.Should().Be("B"); // urgente primero
    }

    [Fact]
    public async Task ListarPendientesAsync_IncluyeAsignados()
    {
        var (svc, db, _) = Crear();
        db.PaquetesPendientesReparto.AddRange(
            new PaquetePendienteReparto { NumeroExpedicion = "A", CtaId = 1 },
            new PaquetePendienteReparto { NumeroExpedicion = "B", CtaId = 1, AsignadoARutaId = 99 });
        await db.SaveChangesAsync();
        (await svc.ListarPendientesAsync(1, incluirAsignados: true)).Should().HaveCount(2);
    }

    [Fact]
    public async Task ListarPendientesAsync_SinFiltroCta_DevuelveTodos()
    {
        var (svc, db, _) = Crear();
        db.PaquetesPendientesReparto.AddRange(
            new PaquetePendienteReparto { NumeroExpedicion = "A", CtaId = 1 },
            new PaquetePendienteReparto { NumeroExpedicion = "B", CtaId = 2 });
        await db.SaveChangesAsync();
        (await svc.ListarPendientesAsync(null)).Should().HaveCount(2);
    }

    [Fact]
    public async Task AsignarARutaAsync_NoExiste_DevuelveError()
    {
        var (svc, _, _) = Crear();
        var (p, e, err) = await svc.AsignarARutaAsync(999, new AsignarPendienteARutaDto { RutaRepartoId = 1 }, "u");
        p.Should().BeNull();
        e.Should().BeNull();
        err.Should().Contain("No existe");
    }

    [Fact]
    public async Task AsignarARutaAsync_YaAsignado_DevuelveError()
    {
        var (svc, db, _) = Crear();
        db.PaquetesPendientesReparto.Add(new PaquetePendienteReparto { Id = 1, NumeroExpedicion = "A", AsignadoARutaId = 5 });
        await db.SaveChangesAsync();
        var (_, _, err) = await svc.AsignarARutaAsync(1, new AsignarPendienteARutaDto { RutaRepartoId = 9 }, "u");
        err.Should().Contain("ya está asignado");
    }

    [Fact]
    public async Task AsignarARutaAsync_RepartoNoCreaEntrega_DevuelveError()
    {
        var (svc, db, reparto) = Crear();
        db.PaquetesPendientesReparto.Add(new PaquetePendienteReparto { Id = 1, NumeroExpedicion = "A" });
        await db.SaveChangesAsync();
        reparto.Setup(r => r.AgregarEntregaARuta(It.IsAny<int>(), It.IsAny<AgregarEntregaDto>()))
               .ReturnsAsync((EntregaPaqueteDto?)null);

        var (_, _, err) = await svc.AsignarARutaAsync(1, new AsignarPendienteARutaDto { RutaRepartoId = 9 }, "u");
        err.Should().Contain("crear la entrega");
    }

    [Fact]
    public async Task AsignarARutaAsync_Exito_MarcaPendienteYDevuelveEntrega()
    {
        var (svc, db, reparto) = Crear();
        db.PaquetesPendientesReparto.Add(new PaquetePendienteReparto
        {
            Id = 1,
            NumeroExpedicion = "NXI-1",
            NumeroSeguimiento = "NXP-1",
            CodigoPostalDestino = "28001",
            CiudadDestino = "Madrid",
            DireccionEntrega = "C/Sol",
            NombreDestinatario = "Ada"
        });
        await db.SaveChangesAsync();

        var entregaDto = new EntregaPaqueteDto { Id = 42, NumeroExpedicion = "NXI-1" };
        reparto.Setup(r => r.AgregarEntregaARuta(7, It.IsAny<AgregarEntregaDto>()))
               .ReturnsAsync(entregaDto);

        var (p, e, err) = await svc.AsignarARutaAsync(1, new AsignarPendienteARutaDto { RutaRepartoId = 7 }, "user-1");
        err.Should().BeNull();
        e.Should().Be(entregaDto);
        p!.Id.Should().Be(1);

        var fila = await db.PaquetesPendientesReparto.SingleAsync();
        fila.AsignadoARutaId.Should().Be(7);
        fila.EntregaPaqueteId.Should().Be(42);
        fila.AsignadoPorIdentityUserId.Should().Be("user-1");
        fila.FechaAsignacion.Should().NotBeNull();
    }
}
