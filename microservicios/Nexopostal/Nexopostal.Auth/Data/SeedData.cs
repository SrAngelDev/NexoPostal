using Microsoft.AspNetCore.Identity;
using NexoPostal.Auth.Models;

namespace NexoPostal.Auth.Data;

public static class SeedData
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // ===== ADMIN =====
        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "admin-seed-id",
            UserName = "admin@nexopostal.es",
            Email = "admin@nexopostal.es",
            NombreCompleto = "Administrador del Sistema",
            CodigoEmpleado = "ADM001",
            Rol = Rol.Admin,
            EmailConfirmed = true
        }, "Admin123!");

        // ===== OPERARIOS =====
        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "operario-maria-garcia-seed-id",
            UserName = "operario@nexopostal.es",
            Email = "operario@nexopostal.es",
            NombreCompleto = "María García López",
            CodigoEmpleado = "OPE001",
            Rol = Rol.OperarioOficina,
            EmailConfirmed = true
        }, "Operario123!");

        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "operario-logistico-pedro-martinez-seed-id",
            UserName = "operario.cta@nexopostal.es",
            Email = "operario.cta@nexopostal.es",
            NombreCompleto = "Pedro Martínez Ruiz",
            CodigoEmpleado = "OPL001",
            Rol = Rol.OperarioCTA,
            EmailConfirmed = true
        }, "Operario123!");

        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "operario-logistico-sergio-romero-seed-id",
            UserName = "operario.cta2@nexopostal.es",
            Email = "operario.cta2@nexopostal.es",
            NombreCompleto = "Sergio Romero Vega",
            CodigoEmpleado = "OPL002",
            Rol = Rol.OperarioCTA,
            EmailConfirmed = true
        }, "Operario123!");

        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "operario-oficina-diego-herrera-seed-id",
            UserName = "operario2@nexopostal.es",
            Email = "operario2@nexopostal.es",
            NombreCompleto = "Diego Herrera Ortiz",
            CodigoEmpleado = "OPE002",
            Rol = Rol.OperarioOficina,
            EmailConfirmed = true
        }, "Operario123!");

        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "operario-jefe-laura-fernandez-seed-id",
            UserName = "supervisor@nexopostal.es",
            Email = "supervisor@nexopostal.es",
            NombreCompleto = "Laura Fernández Díaz",
            CodigoEmpleado = "OPJ001",
            Rol = Rol.Supervisor,
            EmailConfirmed = true
        }, "Operario123!");

        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "operario-jefe-marta-jimenez-seed-id",
            UserName = "supervisor2@nexopostal.es",
            Email = "supervisor2@nexopostal.es",
            NombreCompleto = "Marta Jiménez Castro",
            CodigoEmpleado = "OPJ002",
            Rol = Rol.Supervisor,
            EmailConfirmed = true
        }, "Operario123!");

        // ===== REPARTIDORES =====
        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "repartidor-carlos-rodriguez-seed-id",
            UserName = "repartidor@nexopostal.es",
            Email = "repartidor@nexopostal.es",
            NombreCompleto = "Carlos Rodríguez Sánchez",
            CodigoEmpleado = "REP001",
            Rol = Rol.Repartidor,
            EmailConfirmed = true
        }, "Repartidor123!");

        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "repartidor-sofia-navarro-seed-id",
            UserName = "repartidor2@nexopostal.es",
            Email = "repartidor2@nexopostal.es",
            NombreCompleto = "Sofía Navarro Gil",
            CodigoEmpleado = "RPL001",
            Rol = Rol.Repartidor,
            EmailConfirmed = true
        }, "Repartidor123!");

        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "repartidor-jefe-javier-torres-seed-id",
            UserName = "jefe.reparto@nexopostal.es",
            Email = "jefe.reparto@nexopostal.es",
            NombreCompleto = "Javier Torres Moreno",
            CodigoEmpleado = "RPJ001",
            Rol = Rol.JefeReparto,
            EmailConfirmed = true
        }, "Repartidor123!");

        // ===== CLIENTE DE PRUEBA =====
        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "cliente-ana-lopez-seed-id",
            UserName = "cliente@example.com",
            Email = "cliente@example.com",
            NombreCompleto = "Ana López",
            PhoneNumber = "612345678",
            Rol = Rol.Cliente,
            EmailConfirmed = true
        }, "Cliente123!");
    }

    private static async Task CrearUsuarioSiNoExiste(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        string password)
    {
        var existente = await userManager.FindByIdAsync(user.Id);
        if (existente == null)
        {
            await userManager.CreateAsync(user, password);
        }
        else if (existente.Email != user.Email || existente.Rol != user.Rol)
        {
            // Actualizar email/rol si cambiaron entre despliegues
            existente.Email              = user.Email;
            existente.UserName           = user.UserName;
            existente.NormalizedEmail    = user.Email!.ToUpperInvariant();
            existente.NormalizedUserName = user.UserName!.ToUpperInvariant();
            existente.Rol                = user.Rol;
            await userManager.UpdateAsync(existente);
        }
    }
}

