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
 
    // IDs SOLO de desarrollo
    private const string DevRepartidorBilbaoId = "dev-repartidor-bilbao-id";
    private const string DevRepartidorSevillaId = "dev-repartidor-sevilla-id";
    private const string DevJefeRepartoSevillaId = "dev-jefe-reparto-sevilla-id";

    // Oficinas JSON de Madrid (del oficinas.json de NexoPostal)
    private const int OficinaMadridPrincipal = 1001;
    private const string OficinaMadridPrincipalNombre = "Oficina Principal - Madrid";

    // Oficinas JSON de Barcelona
    private const int OficinaBarcelonaPrincipal = 1026;
    private const string OficinaBarcelonaPrincipalNombre = "Oficina Principal - Barcelona";

    // Oficinas JSON de Bilbao y Sevilla (solo se usan en seed de desarrollo)
    private const int OficinaBilbaoPrincipal = 1117;
    private const string OficinaBilbaoPrincipalNombre = "Oficina Principal - Bilbao";
    private const int OficinaSevillaPrincipal = 1061;
    private const string OficinaSevillaPrincipalNombre = "Oficina Principal - Sevilla";

    public static async Task SeedAsync(RepartoDbContext context, ILogger logger, IHostEnvironment env)
    {
        await CrearRepartidores(context, logger);

        if (env.IsDevelopment())
        {
            await SeedDevelopmentRepartidoresAsync(context, logger);
        }

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

    /// <summary>
    /// Repartidores extra para entorno de desarrollo (Bilbao y Sevilla).
    /// Idempotente: comprueba existencia individual por IdentityUserId.
    /// </summary>
    private static async Task SeedDevelopmentRepartidoresAsync(RepartoDbContext context, ILogger logger)
    {
        var extras = new List<Repartidor>
        {
            // ===== BILBAO =====
            new()
            {
                IdentityUserId = DevRepartidorBilbaoId,
                NombreCompleto = "Naia Aguirre Larrañaga",
                CodigoEmpleado = "REP003",
                Telefono = "620999000",
                OficinaJsonId = OficinaBilbaoPrincipal,
                OficinaNombre = OficinaBilbaoPrincipalNombre,
                TipoVehiculo = TipoVehiculo.Furgoneta,
                MatriculaVehiculo = "7890-MNO"
            },
            // ===== SEVILLA =====
            new()
            {
                IdentityUserId = DevRepartidorSevillaId,
                NombreCompleto = "Andrés Molina Reyes",
                CodigoEmpleado = "REP004",
                Telefono = "620888777",
                OficinaJsonId = OficinaSevillaPrincipal,
                OficinaNombre = OficinaSevillaPrincipalNombre,
                TipoVehiculo = TipoVehiculo.Moto,
                MatriculaVehiculo = "4321-PQR"
            },
            new()
            {
                IdentityUserId = DevJefeRepartoSevillaId,
                NombreCompleto = "Isabel Domínguez Pérez",
                CodigoEmpleado = "JRP003",
                Telefono = "620777666",
                OficinaJsonId = OficinaSevillaPrincipal,
                OficinaNombre = OficinaSevillaPrincipalNombre,
                TipoVehiculo = TipoVehiculo.Furgoneta,
                MatriculaVehiculo = "6543-STU"
            }
        };

        var nuevos = 0;
        foreach (var r in extras)
        {
            var existe = await context.Repartidores.AnyAsync(x => x.IdentityUserId == r.IdentityUserId);
            if (existe) continue;

            context.Repartidores.Add(r);
            nuevos++;
        }
        if (nuevos > 0) await context.SaveChangesAsync();
        logger.LogInformation("[DEV] Creados {Count} repartidores extra (Bilbao/Sevilla)", nuevos);
    }
}
