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
    private const string RepartidorSeedId2 = "repartidor-sofia-navarro-seed-id";
    private const string JefeRepartoSeedId = "repartidor-jefe-javier-torres-seed-id";
    private const string JefeRepartoSeedId2 = "repartidor-jefe-cristina-vidal-seed-id";

    // Oficinas JSON de Madrid (del oficinas.json de NexoPostal)
    private const int OficinaMadridPrincipal = 1001;
    private const string OficinaMadridPrincipalNombre = "Oficina Principal - Madrid";

    // Oficinas JSON de Barcelona
    private const int OficinaBarcelonaPrincipal = 1026;
    private const string OficinaBarcelonaPrincipalNombre = "Oficina Principal - Barcelona";

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
            // ===== MADRID =====
            new()
            {
                IdentityUserId = RepartidorSeedId,
                NombreCompleto = "Carlos Rodríguez Sánchez",
                CodigoEmpleado = "REP001",
                Telefono = "620111222",
                OficinaJsonId = OficinaMadridPrincipal,
                OficinaNombre = OficinaMadridPrincipalNombre,
                TipoVehiculo = TipoVehiculo.Furgoneta,
                MatriculaVehiculo = "1234-ABC"
            },
            new()
            {
                IdentityUserId = JefeRepartoSeedId,
                NombreCompleto = "Javier Torres Moreno",
                CodigoEmpleado = "JRP001",
                Telefono = "620555666",
                OficinaJsonId = OficinaMadridPrincipal,
                OficinaNombre = OficinaMadridPrincipalNombre,
                TipoVehiculo = TipoVehiculo.Furgoneta,
                MatriculaVehiculo = "9012-GHI"
            },
            // ===== BARCELONA =====
            new()
            {
                IdentityUserId = RepartidorSeedId2,
                NombreCompleto = "Sofía Navarro Gil",
                CodigoEmpleado = "REP002",
                Telefono = "620333444",
                OficinaJsonId = OficinaBarcelonaPrincipal,
                OficinaNombre = OficinaBarcelonaPrincipalNombre,
                TipoVehiculo = TipoVehiculo.Moto,
                MatriculaVehiculo = "5678-DEF"
            },
            new()
            {
                IdentityUserId = JefeRepartoSeedId2,
                NombreCompleto = "Cristina Vidal Roca",
                CodigoEmpleado = "JRP002",
                Telefono = "620777888",
                OficinaJsonId = OficinaBarcelonaPrincipal,
                OficinaNombre = OficinaBarcelonaPrincipalNombre,
                TipoVehiculo = TipoVehiculo.Furgoneta,
                MatriculaVehiculo = "3456-JKL"
            }
        };

        context.Repartidores.AddRange(repartidores);
        await context.SaveChangesAsync();

        logger.LogInformation("Creados {Count} repartidores de prueba", repartidores.Count);
    }
}
