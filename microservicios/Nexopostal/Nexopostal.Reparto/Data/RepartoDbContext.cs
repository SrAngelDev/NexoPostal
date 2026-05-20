using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Models;

namespace Nexopostal.Reparto.Data;

/// <summary>
/// Contexto de base de datos para el módulo de Reparto (última milla).
/// Gestiona repartidores, rutas de reparto y entregas de paquetes.
/// </summary>
public class RepartoDbContext : DbContext
{
    public RepartoDbContext(DbContextOptions<RepartoDbContext> options)
        : base(options)
    {
    }

    public DbSet<Repartidor> Repartidores { get; set; }
    public DbSet<RutaReparto> RutasReparto { get; set; }
    public DbSet<EntregaPaquete> EntregasPaquetes { get; set; }
    public DbSet<UbicacionRepartidor> UbicacionesRepartidores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== Repartidor =====
        modelBuilder.Entity<Repartidor>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IdentityUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.HasIndex(e => e.IdentityUserId)
                .IsUnique()
                .HasDatabaseName("IX_Repartidores_IdentityUserId");

            entity.HasIndex(e => e.CodigoEmpleado)
                .IsUnique()
                .HasDatabaseName("IX_Repartidores_CodigoEmpleado");

            entity.HasIndex(e => e.OficinaJsonId)
                .HasDatabaseName("IX_Repartidores_OficinaJsonId");
        });

        // ===== RutaReparto =====
        modelBuilder.Entity<RutaReparto>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Codigo)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_RutasReparto_Codigo");

            entity.HasIndex(e => e.FechaReparto)
                .HasDatabaseName("IX_RutasReparto_FechaReparto");

            entity.HasIndex(e => new { e.RepartidorId, e.FechaReparto })
                .HasDatabaseName("IX_RutasReparto_Repartidor_Fecha");

            entity.HasIndex(e => e.Estado)
                .HasDatabaseName("IX_RutasReparto_Estado");

            entity.HasOne(e => e.Repartidor)
                .WithMany(r => r.Rutas)
                .HasForeignKey(e => e.RepartidorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== EntregaPaquete =====
        modelBuilder.Entity<EntregaPaquete>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NumeroExpedicion)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.NumeroSeguimiento)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.NumeroExpedicion)
                .HasDatabaseName("IX_EntregasPaquetes_NumeroExpedicion");

            entity.HasIndex(e => e.NumeroSeguimiento)
                .HasDatabaseName("IX_EntregasPaquetes_NumeroSeguimiento");

            entity.HasIndex(e => e.Estado)
                .HasDatabaseName("IX_EntregasPaquetes_Estado");

            entity.HasIndex(e => new { e.RutaRepartoId, e.OrdenEnRuta })
                .HasDatabaseName("IX_EntregasPaquetes_Ruta_Orden");

            entity.HasOne(e => e.RutaReparto)
                .WithMany(r => r.Entregas)
                .HasForeignKey(e => e.RutaRepartoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== UbicacionRepartidor =====
        modelBuilder.Entity<UbicacionRepartidor>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.RepartidorId)
                .IsUnique()
                .HasDatabaseName("IX_UbicacionesRepartidores_RepartidorId");

            entity.HasOne(e => e.Repartidor)
                .WithMany()
                .HasForeignKey(e => e.RepartidorId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
