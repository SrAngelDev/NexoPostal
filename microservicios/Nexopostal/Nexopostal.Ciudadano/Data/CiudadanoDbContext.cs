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
    }
}
