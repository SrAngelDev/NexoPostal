using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Data;

public class AuthDbContext : IdentityDbContext<ApplicationUser>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

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

