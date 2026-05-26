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
            await SeedPaquetesPendientesBcnAsync(context, logger);
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
                Rol = "JefeReparto",
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
                Rol = "JefeReparto",
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
                Rol = "JefeReparto",
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

    // ─────────────────────────────────────────────────────────────────────────
    // Bandeja del JefeReparto en CTA Barcelona - El Prat (CTA-BCN)
    // ─────────────────────────────────────────────────────────────────────────
    // CTA-BCN es el 5º CTA creado por IntranetDataSeeder, por lo que su Id
    // autogenerado es 5 (orden: COR, GIJ, BIL, PNA, BCN, ZGZ, MAD, ...).
    private const int CtaBcnId = 5;
    private const string CtaBcnCodigo = "CTA-BCN";

    /// <summary>
    /// Siembra paquetes en la bandeja del JefeReparto del CTA Barcelona - El Prat
    /// para poder probar el panel "/bandeja-jefe" sin necesidad de hacer escaneos
    /// reales en toda la cadena Admisión → CTA origen → CTA destino.
    /// Idempotente por <c>NumeroExpedicion</c> (prefijo NXI-SEEDBCN-...).
    /// </summary>
    private static async Task SeedPaquetesPendientesBcnAsync(RepartoDbContext context, ILogger logger)
    {
        var pendientes = new List<PaquetePendienteReparto>
        {
            new()
            {
                NumeroExpedicion = "NXI-SEEDBCN-001",
                NumeroSeguimiento = "NXP-SEEDBCN-001",
                CtaId = CtaBcnId,
                CtaCodigo = CtaBcnCodigo,
                NombreDestinatario = "Marta Puig Ferrer",
                TelefonoDestinatario = "612001001",
                DireccionEntrega = "Carrer de Provença, 215, 3º 2ª",
                CodigoPostalDestino = "08008",
                CiudadDestino = "Barcelona",
                EsUrgente = true,
                Observaciones = "Llamar al portero automático",
                FechaRegistro = DateTime.UtcNow.AddMinutes(-45)
            },
            new()
            {
                NumeroExpedicion = "NXI-SEEDBCN-002",
                NumeroSeguimiento = "NXP-SEEDBCN-002",
                CtaId = CtaBcnId,
                CtaCodigo = CtaBcnCodigo,
                NombreDestinatario = "Jordi Mas Soler",
                TelefonoDestinatario = "612002002",
                DireccionEntrega = "Avinguda Diagonal, 547, 5º 1ª",
                CodigoPostalDestino = "08029",
                CiudadDestino = "Barcelona",
                EsUrgente = false,
                Observaciones = null,
                FechaRegistro = DateTime.UtcNow.AddMinutes(-40)
            },
            new()
            {
                NumeroExpedicion = "NXI-SEEDBCN-003",
                NumeroSeguimiento = "NXP-SEEDBCN-003",
                CtaId = CtaBcnId,
                CtaCodigo = CtaBcnCodigo,
                NombreDestinatario = "Núria Vila Camps",
                TelefonoDestinatario = "612003003",
                DireccionEntrega = "Carrer del Comerç, 36, bajos",
                CodigoPostalDestino = "08003",
                CiudadDestino = "Barcelona",
                EsUrgente = true,
                Observaciones = "Entregar antes de las 14:00",
                FechaRegistro = DateTime.UtcNow.AddMinutes(-30)
            },
            new()
            {
                NumeroExpedicion = "NXI-SEEDBCN-004",
                NumeroSeguimiento = "NXP-SEEDBCN-004",
                CtaId = CtaBcnId,
                CtaCodigo = CtaBcnCodigo,
                NombreDestinatario = "Pau Casas Vidal",
                TelefonoDestinatario = "612004004",
                DireccionEntrega = "Plaça de Catalunya, 14, 2º",
                CodigoPostalDestino = "08002",
                CiudadDestino = "Barcelona",
                EsUrgente = false,
                Observaciones = null,
                FechaRegistro = DateTime.UtcNow.AddMinutes(-25)
            },
            new()
            {
                NumeroExpedicion = "NXI-SEEDBCN-005",
                NumeroSeguimiento = "NXP-SEEDBCN-005",
                CtaId = CtaBcnId,
                CtaCodigo = CtaBcnCodigo,
                NombreDestinatario = "Helena Ribas Aragó",
                TelefonoDestinatario = "612005005",
                DireccionEntrega = "Carrer del Bruc, 122, 4º 3ª",
                CodigoPostalDestino = "08009",
                CiudadDestino = "Barcelona",
                EsUrgente = false,
                Observaciones = "Dejar en buzón si no hay nadie",
                FechaRegistro = DateTime.UtcNow.AddMinutes(-15)
            },
            new()
            {
                NumeroExpedicion = "NXI-SEEDBCN-006",
                NumeroSeguimiento = "NXP-SEEDBCN-006",
                CtaId = CtaBcnId,
                CtaCodigo = CtaBcnCodigo,
                NombreDestinatario = "Ferran Roig Bosch",
                TelefonoDestinatario = "612006006",
                DireccionEntrega = "Carrer Major, 28, 1º",
                CodigoPostalDestino = "08820",
                CiudadDestino = "El Prat de Llobregat",
                EsUrgente = true,
                Observaciones = "Cliente prefiere entrega en mano",
                FechaRegistro = DateTime.UtcNow.AddMinutes(-10)
            },
            new()
            {
                NumeroExpedicion = "NXI-SEEDBCN-007",
                NumeroSeguimiento = "NXP-SEEDBCN-007",
                CtaId = CtaBcnId,
                CtaCodigo = CtaBcnCodigo,
                NombreDestinatario = "Aina Serra Lloret",
                TelefonoDestinatario = "612007007",
                DireccionEntrega = "Rambla de Catalunya, 90, 6º 1ª",
                CodigoPostalDestino = "08008",
                CiudadDestino = "Barcelona",
                EsUrgente = false,
                Observaciones = null,
                FechaRegistro = DateTime.UtcNow.AddMinutes(-5)
            },
            new()
            {
                NumeroExpedicion = "NXI-SEEDBCN-008",
                NumeroSeguimiento = "NXP-SEEDBCN-008",
                CtaId = CtaBcnId,
                CtaCodigo = CtaBcnCodigo,
                NombreDestinatario = "Roger Llorens Pou",
                TelefonoDestinatario = "612008008",
                DireccionEntrega = "Carrer de Sants, 200, ático",
                CodigoPostalDestino = "08028",
                CiudadDestino = "Barcelona",
                EsUrgente = false,
                Observaciones = "Edificio sin ascensor",
                FechaRegistro = DateTime.UtcNow.AddMinutes(-2)
            }
        };

        var nuevos = 0;
        foreach (var p in pendientes)
        {
            var existe = await context.PaquetesPendientesReparto
                .AnyAsync(x => x.NumeroExpedicion == p.NumeroExpedicion);
            if (existe) continue;

            context.PaquetesPendientesReparto.Add(p);
            nuevos++;
        }

        if (nuevos > 0)
        {
            await context.SaveChangesAsync();
            logger.LogInformation(
                "[DEV] Sembrados {Count} paquetes pendientes en bandeja CTA-BCN (Barcelona - El Prat)",
                nuevos);
        }
        else
        {
            logger.LogInformation("[DEV] Paquetes pendientes CTA-BCN ya existen, omitiendo seed");
        }
    }
}
