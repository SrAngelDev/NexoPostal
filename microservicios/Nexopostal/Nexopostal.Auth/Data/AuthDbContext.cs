using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Data;

/// <summary>
/// Contexto de Entity Framework para autenticación e identidad.
/// Centraliza la configuración extra que NexoPostal añade sobre Identity.
/// </summary>
public class AuthDbContext : IdentityDbContext<ApplicationUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Ajusta el modelo de usuario para que los datos propios del negocio tengan
    /// límites y conversiones coherentes en base de datos.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.NombreCompleto)
                .HasMaxLength(255)
                .IsRequired();
            
            entity.Property(u => u.CodigoEmpleado)
                .HasMaxLength(50);

            entity.Property(u => u.Rol)
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasDefaultValue(Models.Rol.Cliente);
        });
    }
}

