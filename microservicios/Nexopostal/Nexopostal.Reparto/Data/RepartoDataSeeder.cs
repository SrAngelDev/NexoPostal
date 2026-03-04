using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Models;

namespace Nexopostal.Reparto.Data;

/// <summary>
/// Seeder de datos iniciales para el módulo de Reparto.
/// Crea repartidores de prueba vinculados a las cuentas del Auth seed.
/// </summary>
public static class RepartoDataSeeder
{
    // IDs de Auth.SeedData — deben coincidir exactamente
    private const string RepartidorSeedId = "repartidor-carlos-rodriguez-seed-id";
    private const string RepartidorLogisticoSeedId = "repartidor-logistico-sofia-navarro-seed-id";
    private const string RepartidorJefeSeedId = "repartidor-jefe-javier-torres-seed-id";

    // Oficinas JSON de Madrid (del oficinas.json de Correos)
    private const int OficinaMoncloa = 1001;
    private const string OficinaMoncloanNombre = "Sucursal 01 - Madrid-Moncloa";
    private const int OficinaChamberí = 1002;
    private const string OficinaChamberíNombre = "Sucursal 02 - Madrid-Chamberí";

    public static async Task SeedAsync(RepartoDbContext context, ILogger logger)
    {
        await CrearRepartidores(context, logger);

        logger.LogInformation("Seed de Reparto completado");
    }

    private static async Task CrearRepartidores(RepartoDbContext context, ILogger logger)
    {
        if (await context.Repartidores.AnyAsync())
        {
            logger.LogInformation("Repartidores ya existen, omitiendo seed");
            return;
        }

        var repartidores = new List<Repartidor>
        {
            new()
            {
                IdentityUserId = RepartidorSeedId,
                NombreCompleto = "Carlos Rodríguez Sánchez",
                CodigoEmpleado = "REP001",
                Telefono = "620111222",
                OficinaJsonId = OficinaMoncloa,
                OficinaNombre = OficinaMoncloanNombre,
                TipoVehiculo = TipoVehiculo.Furgoneta,
                MatriculaVehiculo = "1234-ABC"
            },
            new()
            {
                IdentityUserId = RepartidorLogisticoSeedId,
                NombreCompleto = "Sofía Navarro Gil",
                CodigoEmpleado = "RPL001",
                Telefono = "620333444",
                OficinaJsonId = OficinaMoncloa,
                OficinaNombre = OficinaMoncloanNombre,
                TipoVehiculo = TipoVehiculo.Moto,
                MatriculaVehiculo = "5678-DEF"
            },
            new()
            {
                IdentityUserId = RepartidorJefeSeedId,
                NombreCompleto = "Javier Torres Moreno",
                CodigoEmpleado = "RPJ001",
                Telefono = "620555666",
                OficinaJsonId = OficinaChamberí,
                OficinaNombre = OficinaChamberíNombre,
                TipoVehiculo = TipoVehiculo.Furgoneta,
                MatriculaVehiculo = "9012-GHI"
            }
        };

        context.Repartidores.AddRange(repartidores);
        await context.SaveChangesAsync();

        logger.LogInformation("Creados {Count} repartidores de prueba", repartidores.Count);
    }
}
