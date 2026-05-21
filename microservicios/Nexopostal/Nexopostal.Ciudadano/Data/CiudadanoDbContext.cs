using Microsoft.EntityFrameworkCore;
using Nexopostal.Ciudadano.Models;

namespace Nexopostal.Ciudadano.Data;

/// <summary>
/// Contexto de base de datos para el módulo Ciudadano
/// Gestiona envíos, perfiles y direcciones favoritas
/// </summary>
public class CiudadanoDbContext : DbContext
{
    public CiudadanoDbContext(DbContextOptions<CiudadanoDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Envio> Envios { get; set; }
    public DbSet<ClientePerfil> ClientePerfiles { get; set; }
    public DbSet<DireccionFavorita> DireccionesFavoritas { get; set; }
    public DbSet<Oficina> Oficinas { get; set; }
    public DbSet<TarifaBanda> TarifasBandas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de la entidad Envio
        modelBuilder.Entity<Envio>(entity =>
        {
            entity.HasKey(e => e.NumeroSeguimiento);
            
            entity.Property(e => e.NumeroSeguimiento)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.NumeroExpedicion)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.PesoKg)
                .HasPrecision(10, 2);

            entity.Property(e => e.CosteCalculado)
                .HasPrecision(10, 2);

            // Índice único en NumeroExpedicion (código interno)
            entity.HasIndex(e => e.NumeroExpedicion)
                .IsUnique()
                .HasDatabaseName("IX_Envios_NumeroExpedicion");

            entity.HasIndex(e => e.IdentityUserId)
                .HasDatabaseName("IX_Envios_IdentityUserId");

            entity.HasIndex(e => e.CodigoPostalDestino)
                .HasDatabaseName("IX_Envios_CodigoPostalDestino");

            entity.HasIndex(e => e.FechaCreacion)
                .HasDatabaseName("IX_Envios_FechaCreacion");

            // Índice en EstadoInternoActual para consultas de operarios
            entity.HasIndex(e => e.EstadoInternoActual)
                .HasDatabaseName("IX_Envios_EstadoInternoActual");
        });

        // Configuración de la entidad ClientePerfil
        modelBuilder.Entity<ClientePerfil>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IdentityUserId)
                .HasMaxLength(450)
                .IsRequired();

            // Índice único para evitar perfiles duplicados por usuario
            entity.HasIndex(e => e.IdentityUserId)
                .IsUnique()
                .HasDatabaseName("IX_ClientePerfil_IdentityUserId");

            // Relación uno a muchos con DireccionFavorita
            entity.HasMany(e => e.Agenda)
                .WithOne(d => d.ClientePerfil)
                .HasForeignKey(d => d.ClientePerfilId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configuración de la entidad DireccionFavorita
        modelBuilder.Entity<DireccionFavorita>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.ClientePerfilId)
                .HasDatabaseName("IX_DireccionFavorita_ClientePerfilId");
        });

        // Configuración + seed de TarifaBanda (4 series × 6 bandas = 24 filas)
        modelBuilder.Entity<TarifaBanda>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.Serie, e.OrdenBanda }).IsUnique()
                .HasDatabaseName("IX_TarifaBanda_Serie_Orden");

            var bandas = new (decimal PesoHasta, decimal Local, decimal LocalPremium, decimal Pen, decimal PenPremium)[]
            {
                (1m,   4.50m,  6.50m,  5.95m,  8.95m),
                (2m,   5.25m,  7.75m,  6.95m, 10.50m),
                (5m,   6.95m, 10.50m,  8.95m, 13.95m),
                (10m,  9.95m, 14.95m, 12.95m, 19.95m),
                (20m, 14.95m, 21.95m, 18.95m, 28.95m),
                (30m, 19.95m, 29.95m, 25.95m, 38.95m)
            };

            var seed = new List<TarifaBanda>();
            var idCounter = 1;
            var fechaSeed = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
            for (var i = 0; i < bandas.Length; i++)
            {
                var b = bandas[i];
                seed.Add(new TarifaBanda { Id = idCounter++, Serie = TarifaSerie.LocalEstandar,     OrdenBanda = i, PesoHastaKg = b.PesoHasta, PrecioBase = b.Local,        FechaModificacion = fechaSeed });
                seed.Add(new TarifaBanda { Id = idCounter++, Serie = TarifaSerie.LocalPremium,      OrdenBanda = i, PesoHastaKg = b.PesoHasta, PrecioBase = b.LocalPremium, FechaModificacion = fechaSeed });
                seed.Add(new TarifaBanda { Id = idCounter++, Serie = TarifaSerie.PeninsulaEstandar, OrdenBanda = i, PesoHastaKg = b.PesoHasta, PrecioBase = b.Pen,          FechaModificacion = fechaSeed });
                seed.Add(new TarifaBanda { Id = idCounter++, Serie = TarifaSerie.PeninsulaPremium,  OrdenBanda = i, PesoHastaKg = b.PesoHasta, PrecioBase = b.PenPremium,   FechaModificacion = fechaSeed });
            }

            entity.HasData(seed);
        });
    }
}
