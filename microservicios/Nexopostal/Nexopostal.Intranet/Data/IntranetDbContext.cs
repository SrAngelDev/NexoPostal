using Microsoft.EntityFrameworkCore;
using Nexopostal.Intranet.Models;

namespace Nexopostal.Intranet.Data;

/// <summary>
/// Contexto de base de datos para el módulo Intranet.
/// Gestiona la red logística de NexoPostal: CTAs, operarios,
/// asignaciones de paquetes, movimientos troncales e incidencias.
/// </summary>
public class IntranetDbContext : DbContext
{
    public IntranetDbContext(DbContextOptions<IntranetDbContext> options)
        : base(options)
    {
    }

    // ===== DbSets =====
    public DbSet<CentroTratamiento> CentrosTratamiento { get; set; }
    public DbSet<RutaCta> RutasCta { get; set; }
    public DbSet<OperarioCta> OperariosCta { get; set; }
    public DbSet<AsignacionPaquete> AsignacionesPaquetes { get; set; }
    public DbSet<MovimientoPaquete> MovimientosPaquetes { get; set; }
    public DbSet<Incidencia> Incidencias { get; set; }
    public DbSet<HistorialEstado> HistorialEstados { get; set; }
    public DbSet<OperarioOficina> OperariosOficina { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== CentroTratamiento =====
        modelBuilder.Entity<CentroTratamiento>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Codigo)
                .HasMaxLength(10)
                .IsRequired();

            entity.HasIndex(e => e.Codigo)
                .IsUnique()
                .HasDatabaseName("IX_CentrosTratamiento_Codigo");

            entity.HasIndex(e => e.Area)
                .HasDatabaseName("IX_CentrosTratamiento_Area");
        });

        // ===== RutaCta =====
        modelBuilder.Entity<RutaCta>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PrefijoCp)
                .HasMaxLength(2)
                .IsRequired();

            entity.HasIndex(e => e.PrefijoCp)
                .IsUnique()
                .HasDatabaseName("IX_RutasCta_PrefijoCp");

            entity.HasOne(e => e.Cta)
                .WithMany(c => c.RutasAsignadas)
                .HasForeignKey(e => e.CtaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== OperarioCta =====
        modelBuilder.Entity<OperarioCta>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IdentityUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.HasIndex(e => e.IdentityUserId)
                .HasDatabaseName("IX_OperariosCta_IdentityUserId");

            entity.HasIndex(e => new { e.IdentityUserId, e.CentroTratamientoId })
                .IsUnique()
                .HasDatabaseName("IX_OperariosCta_Identity_Cta");

            entity.HasIndex(e => e.CodigoEmpleado)
                .HasDatabaseName("IX_OperariosCta_CodigoEmpleado");

            entity.HasIndex(e => e.CentroTratamientoId)
                .HasDatabaseName("IX_OperariosCta_CentroTratamientoId");

            entity.HasOne(e => e.CentroTratamiento)
                .WithMany(c => c.Operarios)
                .HasForeignKey(e => e.CentroTratamientoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== AsignacionPaquete =====
        modelBuilder.Entity<AsignacionPaquete>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NumeroExpedicion)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.NumeroExpedicion)
                .HasDatabaseName("IX_AsignacionesPaquetes_NumeroExpedicion");

            entity.HasIndex(e => e.EstadoTarea)
                .HasDatabaseName("IX_AsignacionesPaquetes_EstadoTarea");

            entity.HasIndex(e => new { e.CtaId, e.EstadoTarea })
                .HasDatabaseName("IX_AsignacionesPaquetes_Cta_Estado");

            entity.HasIndex(e => new { e.OperarioAsignadoId, e.EstadoTarea })
                .HasDatabaseName("IX_AsignacionesPaquetes_Operario_Estado");

            entity.HasOne(e => e.OperarioAsignado)
                .WithMany(o => o.AsignacionesRecibidas)
                .HasForeignKey(e => e.OperarioAsignadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.AsignadoPor)
                .WithMany(o => o.AsignacionesCreadas)
                .HasForeignKey(e => e.AsignadoPorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Cta)
                .WithMany(c => c.Asignaciones)
                .HasForeignKey(e => e.CtaId)
                .OnDelete(DeleteBehavior.Restrict);

            // FK opcional al operario de oficina (tareas de oficina postal)
            entity.HasOne(e => e.OperarioOficinaAsignado)
                .WithMany()
                .HasForeignKey(e => e.OperarioOficinaAsignadoId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.OperarioOficinaAsignadoId, e.EstadoTarea })
                .HasDatabaseName("IX_AsignacionesPaquetes_OperarioOficina_Estado");

            entity.HasIndex(e => new { e.OficinaJsonId, e.EstadoTarea })
                .HasDatabaseName("IX_AsignacionesPaquetes_Oficina_Estado");
        });

        // ===== MovimientoPaquete =====
        modelBuilder.Entity<MovimientoPaquete>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NumeroExpedicion)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.NumeroExpedicion)
                .HasDatabaseName("IX_MovimientosPaquetes_NumeroExpedicion");

            entity.HasIndex(e => e.Estado)
                .HasDatabaseName("IX_MovimientosPaquetes_Estado");

            entity.HasIndex(e => new { e.CtaOrigenId, e.Estado })
                .HasDatabaseName("IX_MovimientosPaquetes_CtaOrigen_Estado");

            entity.HasIndex(e => new { e.CtaDestinoId, e.Estado })
                .HasDatabaseName("IX_MovimientosPaquetes_CtaDestino_Estado");

            entity.HasOne(e => e.CtaOrigen)
                .WithMany(c => c.MovimientosOrigen)
                .HasForeignKey(e => e.CtaOrigenId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.CtaDestino)
                .WithMany(c => c.MovimientosDestino)
                .HasForeignKey(e => e.CtaDestinoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== Incidencia =====
        modelBuilder.Entity<Incidencia>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NumeroExpedicion)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.NumeroExpedicion)
                .HasDatabaseName("IX_Incidencias_NumeroExpedicion");

            entity.HasIndex(e => e.Estado)
                .HasDatabaseName("IX_Incidencias_Estado");

            entity.HasIndex(e => new { e.CtaId, e.Estado })
                .HasDatabaseName("IX_Incidencias_Cta_Estado");

            entity.HasOne(e => e.Cta)
                .WithMany(c => c.Incidencias)
                .HasForeignKey(e => e.CtaId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ReportadaPor)
                .WithMany(o => o.IncidenciasReportadas)
                .HasForeignKey(e => e.ReportadaPorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== HistorialEstado =====
        modelBuilder.Entity<HistorialEstado>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.NumeroExpedicion)
                .HasMaxLength(20)
                .IsRequired();

            entity.HasIndex(e => e.NumeroExpedicion)
                .HasDatabaseName("IX_HistorialEstados_NumeroExpedicion");

            entity.HasIndex(e => e.NumeroSeguimiento)
                .HasDatabaseName("IX_HistorialEstados_NumeroSeguimiento");

            entity.HasIndex(e => e.FechaEvento)
                .HasDatabaseName("IX_HistorialEstados_FechaEvento");

            entity.HasIndex(e => new { e.NumeroExpedicion, e.FechaEvento })
                .HasDatabaseName("IX_HistorialEstados_Expedicion_Fecha");

            entity.HasOne(e => e.Operario)
                .WithMany()
                .HasForeignKey(e => e.OperarioId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== OperarioOficina =====
        // Las oficinas NO están en BD; vienen del JSON estático (Data/oficinas.json).
        // OficinaJsonId es una referencia lógica, no una FK de EF.
        modelBuilder.Entity<OperarioOficina>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.IdentityUserId)
                .HasMaxLength(450)
                .IsRequired();

            entity.HasIndex(e => e.IdentityUserId)
                .HasDatabaseName("IX_OperariosOficina_IdentityUserId");

            entity.HasIndex(e => new { e.IdentityUserId, e.OficinaJsonId })
                .IsUnique()
                .HasDatabaseName("IX_OperariosOficina_Identity_Oficina");

            entity.HasIndex(e => e.OficinaJsonId)
                .HasDatabaseName("IX_OperariosOficina_OficinaJsonId");
        });
    }
}
