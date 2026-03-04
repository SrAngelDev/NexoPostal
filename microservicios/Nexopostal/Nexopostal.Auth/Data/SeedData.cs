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
            UserName = "operario.logistico@nexopostal.es",
            Email = "operario.logistico@nexopostal.es",
            NombreCompleto = "Pedro Martínez Ruiz",
            CodigoEmpleado = "OPL001",
            Rol = Rol.OperarioLogistico,
            EmailConfirmed = true
        }, "Operario123!");

        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "operario-jefe-laura-fernandez-seed-id",
            UserName = "operario.jefe@nexopostal.es",
            Email = "operario.jefe@nexopostal.es",
            NombreCompleto = "Laura Fernández Díaz",
            CodigoEmpleado = "OPJ001",
            Rol = Rol.OperarioJefe,
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
            Id = "repartidor-logistico-sofia-navarro-seed-id",
            UserName = "repartidor.logistico@nexopostal.es",
            Email = "repartidor.logistico@nexopostal.es",
            NombreCompleto = "Sofía Navarro Gil",
            CodigoEmpleado = "RPL001",
            Rol = Rol.RepartidorLogistico,
            EmailConfirmed = true
        }, "Repartidor123!");

        await CrearUsuarioSiNoExiste(userManager, new ApplicationUser
        {
            Id = "repartidor-jefe-javier-torres-seed-id",
            UserName = "repartidor.jefe@nexopostal.es",
            Email = "repartidor.jefe@nexopostal.es",
            NombreCompleto = "Javier Torres Moreno",
            CodigoEmpleado = "RPJ001",
            Rol = Rol.RepartidorJefe,
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
        if (await userManager.FindByEmailAsync(user.Email!) == null)
        {
            await userManager.CreateAsync(user, password);
        }
    }
}

