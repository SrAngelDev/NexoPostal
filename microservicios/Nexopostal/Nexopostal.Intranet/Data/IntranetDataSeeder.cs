using Microsoft.EntityFrameworkCore;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Data;

/// <summary>
/// Seeder de datos iniciales para la red logística de NexoPostal.
/// 
/// Siembra:
///   - 17 CTAs distribuidos en las 7 Áreas Zonales de España
///   - 52 rutas de enrutamiento (prefijos CP → CTA)
///   - 4 operarios de CTA repartidos entre CTA-MAD y CTA-BCN (1 CTA por usuario)
///   - 2 operarios de oficina: María en oficinas de Madrid, Diego en oficinas de Barcelona
/// 
/// Roles:
///   - Admin: control total del sistema (sin asignación explícita a CTA u oficina)
///   - OperarioOficina (María → Madrid, Diego → Barcelona): mueve paquetes en las oficinas
///   - OperarioCTA (Pedro → MAD, Sergio → BCN): clasificación y movimientos troncales
///   - Supervisor (Laura → MAD, Marta → BCN): incidencias, altas de personal y métricas del CTA
/// </summary>
public static class IntranetDataSeeder
{
    // IDs fijos que coinciden con los del SeedData de Nexopostal.Auth
    private const string AdminSeedId = "admin-seed-id";
    private const string OperarioOficinaSeedId = "operario-maria-garcia-seed-id";
    private const string OperarioOficina2SeedId = "operario-oficina-diego-herrera-seed-id";
    private const string OperarioCtaSeedId = "operario-logistico-pedro-martinez-seed-id";
    private const string OperarioCta2SeedId = "operario-logistico-sergio-romero-seed-id";
    private const string SupervisorSeedId = "operario-jefe-laura-fernandez-seed-id";
    private const string Supervisor2SeedId = "operario-jefe-marta-jimenez-seed-id";

    // IDs de usuarios SOLO de desarrollo (definidos en Auth.SeedData dentro del bloque dev).
    private const string DevOperarioOficinaBilbaoId = "dev-operario-oficina-bilbao-id";
    private const string DevOperarioOficinaSevillaId = "dev-operario-oficina-sevilla-id";
    private const string DevOperarioCtaBilbaoId = "dev-operario-cta-bilbao-id";
    private const string DevOperarioCtaSevillaId = "dev-operario-cta-sevilla-id";
    private const string DevSupervisorBilbaoId = "dev-supervisor-bilbao-id";
    private const string DevSupervisorSevillaId = "dev-supervisor-sevilla-id";

    public static async Task SeedAsync(IntranetDbContext context, ILogger logger, OficinasJsonService oficinasService, IHostEnvironment env)
    {
        // ── 0. Sembrar oficinas postales desde JSON (idempotente) ──
        if (!await context.OficinasPostales.AnyAsync())
        {
            logger.LogInformation("Sembrando OficinasPostales desde JSON...");
            var desdeJson = oficinasService.CargarDesdeJsonFile();
            var ahora = DateTime.UtcNow;

            foreach (var o in desdeJson)
            {
                context.OficinasPostales.Add(new OficinaPostal
                {
                    Id = o.Id,
                    Nombre = o.Nombre,
                    Direccion = o.Direccion,
                    CodigoPostal = o.CodigoPostal,
                    Ciudad = o.Ciudad,
                    Provincia = null,
                    Telefono = null,
                    Horario = o.Horario,
                    Servicios = o.Servicios,
                    Latitud = o.Latitud,
                    Longitud = o.Longitud,
                    Activo = true,
                    FechaAlta = ahora,
                    FechaModificacion = ahora,
                    ModificadoPorUserId = AdminSeedId
                });
            }
            await context.SaveChangesAsync();

            // Resync de la secuencia Identity de Postgres tras inserts con Id explícito.
            try
            {
                await context.Database.ExecuteSqlRawAsync(
                    "SELECT setval(pg_get_serial_sequence('\"OficinasPostales\"', 'Id'), " +
                    "(SELECT COALESCE(MAX(\"Id\"), 1) FROM \"OficinasPostales\"));");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo resyncronizar la secuencia de OficinasPostales (puede ser por dialecto no-Postgres)");
            }

            oficinasService.Invalidar();
            logger.LogInformation("✓ {Count} oficinas postales sembradas en BD", desdeJson.Count);
        }

        // Solo sembrar el resto si no hay CTAs
        if (await context.CentrosTratamiento.AnyAsync())
        {
            logger.LogInformation("La base de datos ya contiene datos logísticos. Seeding de CTAs/operarios omitido.");
            // Aun así, en desarrollo, intentamos completar los operarios extra (idempotente).
            if (env.IsDevelopment())
            {
                await SeedDevelopmentExtrasAsync(context, logger, oficinasService);
            }
            return;
        }

        logger.LogInformation("Iniciando seeding de datos logísticos...");

        // 1. Crear los 17 CTAs
        var ctas = CrearCtas();
        context.CentrosTratamiento.AddRange(ctas);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ {Count} CTAs creados", ctas.Count);

        // 2. Crear las rutas de enrutamiento (52 prefijos CP)
        var rutas = CrearRutasCta(ctas);
        context.RutasCta.AddRange(rutas);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ {Count} rutas de enrutamiento creadas", rutas.Count);

        // 3. Asignar operarios de CTA: cada usuario tiene UN único CTA (Madrid o Barcelona).
        var operariosCta = CrearOperariosCta(ctas);
        context.OperariosCta.AddRange(operariosCta);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ {Count} asignaciones operario-CTA creadas (1 CTA por usuario)",
            operariosCta.Count);

        // 4. Asignar operarios de oficina a oficinas de Madrid y Barcelona.
        var todasLasOficinas = oficinasService.ObtenerTodas();
        var operariosOficina = CrearOperariosOficina(todasLasOficinas);
        context.OperariosOficina.AddRange(operariosOficina);
        await context.SaveChangesAsync();
        logger.LogInformation("✓ {Count} asignaciones operario-oficina creadas (María en Madrid, Diego en Barcelona)",
            operariosOficina.Count);

        // ===== EXTRAS SOLO PARA DESARROLLO LOCAL =====
        if (env.IsDevelopment())
        {
            await SeedDevelopmentExtrasAsync(context, logger, oficinasService);
        }

        logger.LogInformation("Seeding completado exitosamente.");
    }

    /// <summary>
    /// Operarios adicionales SOLO para el entorno de desarrollo.
    /// Cubre CTA-BIL, CTA-SEV y oficinas principales de Bilbao y Sevilla
    /// para poder ejecutar todos los escenarios del Plan E2E.
    /// Idempotente: comprueba existencia antes de insertar cada fila.
    /// </summary>
    private static async Task SeedDevelopmentExtrasAsync(
        IntranetDbContext context,
        ILogger logger,
        OficinasJsonService oficinasService)
    {
        logger.LogInformation("[DEV] Sembrando operarios extra para desarrollo...");

        var ctaBilbao = await context.CentrosTratamiento.FirstOrDefaultAsync(c => c.Codigo == "CTA-BIL");
        var ctaSevilla = await context.CentrosTratamiento.FirstOrDefaultAsync(c => c.Codigo == "CTA-SEV");

        // -------- Operarios CTA --------
        var operariosCtaDev = new List<(string IdentityUserId, string Nombre, string Codigo, RolOperario Rol, int? CtaId)>
        {
            (DevOperarioCtaBilbaoId,  "Iker Mendizábal Aranzadi", "OPL003", RolOperario.OperarioCTA, ctaBilbao?.Id),
            (DevOperarioCtaSevillaId, "Manuel Guerrero Ortiz",    "OPL004", RolOperario.OperarioCTA, ctaSevilla?.Id),
            (DevSupervisorBilbaoId,   "Aitor Ibarra Goikoetxea",  "OPJ003", RolOperario.Supervisor,  ctaBilbao?.Id),
            (DevSupervisorSevillaId,  "Elena Cortés Vargas",      "OPJ004", RolOperario.Supervisor,  ctaSevilla?.Id),
        };

        var nuevosCta = 0;
        foreach (var op in operariosCtaDev)
        {
            if (op.CtaId is null) continue;
            var existe = await context.OperariosCta.AnyAsync(o => o.IdentityUserId == op.IdentityUserId);
            if (existe) continue;

            context.OperariosCta.Add(new OperarioCta
            {
                IdentityUserId = op.IdentityUserId,
                NombreCompleto = op.Nombre,
                CodigoEmpleado = op.Codigo,
                Rol = op.Rol,
                CentroTratamientoId = op.CtaId.Value
            });
            nuevosCta++;
        }
        if (nuevosCta > 0) await context.SaveChangesAsync();
        logger.LogInformation("[DEV] ✓ {Count} operarios de CTA añadidos", nuevosCta);

        // -------- Operarios Oficina --------
        var todasLasOficinas = oficinasService.ObtenerTodas();
        var oficinaBilbao = todasLasOficinas
            .Where(o => o.Ciudad.Equals("BILBAO", StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Id)
            .FirstOrDefault();
        var oficinaSevilla = todasLasOficinas
            .Where(o => o.Ciudad.Equals("SEVILLA", StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Id)
            .FirstOrDefault();

        var operariosOficinaDev = new List<(string IdentityUserId, string Nombre, string Codigo, DTOs.OficinaJsonDto? Oficina)>
        {
            (DevOperarioOficinaBilbaoId,  "Roberto Sáenz Etxebarria", "OPE003", oficinaBilbao),
            (DevOperarioOficinaSevillaId, "Lucía Romero Cabrera",     "OPE004", oficinaSevilla),
        };

        var nuevasOficinas = 0;
        foreach (var op in operariosOficinaDev)
        {
            if (op.Oficina is null) continue;
            var existe = await context.OperariosOficina.AnyAsync(o => o.IdentityUserId == op.IdentityUserId);
            if (existe) continue;

            context.OperariosOficina.Add(new OperarioOficina
            {
                IdentityUserId = op.IdentityUserId,
                NombreCompleto = op.Nombre,
                CodigoEmpleado = op.Codigo,
                Rol = RolOperario.OperarioOficina,
                OficinaJsonId = op.Oficina.Id,
                OficinaNombre = op.Oficina.Nombre
            });
            nuevasOficinas++;
        }
        if (nuevasOficinas > 0) await context.SaveChangesAsync();
        logger.LogInformation("[DEV] ✓ {Count} operarios de oficina añadidos (Bilbao/Sevilla)", nuevasOficinas);
    }

    /// <summary>
    /// Crea los 17 CTAs distribuidos en las 7 Áreas Zonales.
    /// </summary>
    private static List<CentroTratamiento> CrearCtas()
    {
        return
        [
            // ===== ÁREA NOROESTE (Galicia, Asturias, León, Zamora) =====
            new CentroTratamiento
            {
                Codigo = "CTA-COR",
                Nombre = "CTA A Coruña",
                Area = AreaZonal.Noroeste,
                Provincia = "A Coruña",
                Ciudad = "A Coruña",
                Direccion = "Polígono Industrial de Agrela, s/n",
                CodigoPostal = "15008",
                EsNodoAereo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-GIJ",
                Nombre = "CTA Gijón",
                Area = AreaZonal.Noroeste,
                Provincia = "Asturias",
                Ciudad = "Gijón",
                Direccion = "Polígono Industrial de Roces, s/n",
                CodigoPostal = "33211"
            },

            // ===== ÁREA NORTE (País Vasco, Cantabria, Navarra, La Rioja, norte CyL) =====
            new CentroTratamiento
            {
                Codigo = "CTA-BIL",
                Nombre = "CTA Bilbao",
                Area = AreaZonal.Norte,
                Provincia = "Vizcaya",
                Ciudad = "Bilbao",
                Direccion = "Polígono Industrial Arriaga, s/n",
                CodigoPostal = "48015",
                EsNodoAereo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-PNA",
                Nombre = "CTA Pamplona",
                Area = AreaZonal.Norte,
                Provincia = "Navarra",
                Ciudad = "Pamplona",
                Direccion = "Polígono Industrial Landaben, s/n",
                CodigoPostal = "31012"
            },

            // ===== ÁREA NORESTE (Cataluña, Aragón) =====
            new CentroTratamiento
            {
                Codigo = "CTA-BCN",
                Nombre = "CTA Barcelona - El Prat",
                Area = AreaZonal.Noreste,
                Provincia = "Barcelona",
                Ciudad = "El Prat de Llobregat",
                Direccion = "Zona de Actividades Logísticas, Aeropuerto",
                CodigoPostal = "08820",
                EsNodoAereo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-ZGZ",
                Nombre = "CTA Zaragoza",
                Area = AreaZonal.Noreste,
                Provincia = "Zaragoza",
                Ciudad = "Zaragoza",
                Direccion = "Plataforma Logística PLAZA, s/n",
                CodigoPostal = "50197",
                EsNodoAereo = true
            },

            // ===== ÁREA CENTRO (Madrid, Castilla-La Mancha, centro CyL) =====
            new CentroTratamiento
            {
                Codigo = "CTA-MAD",
                Nombre = "CTA Madrid - Barajas",
                Area = AreaZonal.Centro,
                Provincia = "Madrid",
                Ciudad = "Madrid",
                Direccion = "Centro Logístico de Barajas, Ctra. M-111",
                CodigoPostal = "28042",
                EsNodoAereo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-VLL",
                Nombre = "CTA Valladolid",
                Area = AreaZonal.Centro,
                Provincia = "Valladolid",
                Ciudad = "Valladolid",
                Direccion = "Polígono Industrial San Cristóbal, s/n",
                CodigoPostal = "47012"
            },

            // ===== ÁREA ESTE (Comunidad Valenciana, Murcia) =====
            new CentroTratamiento
            {
                Codigo = "CTA-VLC",
                Nombre = "CTA Valencia",
                Area = AreaZonal.Este,
                Provincia = "Valencia",
                Ciudad = "Valencia",
                Direccion = "Centro Logístico de Riba-roja de Túria",
                CodigoPostal = "46190",
                EsNodoAereo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-ALI",
                Nombre = "CTA Alicante",
                Area = AreaZonal.Este,
                Provincia = "Alicante",
                Ciudad = "Alicante",
                Direccion = "Polígono Industrial Las Atalayas, s/n",
                CodigoPostal = "03114",
                EsNodoAereo = true
            },

            // ===== ÁREA SUR (Andalucía, Extremadura, Ceuta, Melilla) =====
            new CentroTratamiento
            {
                Codigo = "CTA-SEV",
                Nombre = "CTA Sevilla",
                Area = AreaZonal.Sur,
                Provincia = "Sevilla",
                Ciudad = "Sevilla",
                Direccion = "Centro Logístico Aeropuerto San Pablo",
                CodigoPostal = "41020",
                EsNodoAereo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-MAL",
                Nombre = "CTA Málaga",
                Area = AreaZonal.Sur,
                Provincia = "Málaga",
                Ciudad = "Málaga",
                Direccion = "Polígono Industrial Guadalhorce, s/n",
                CodigoPostal = "29004",
                EsNodoAereo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-BAD",
                Nombre = "CTA Badajoz",
                Area = AreaZonal.Sur,
                Provincia = "Badajoz",
                Ciudad = "Badajoz",
                Direccion = "Polígono Industrial El Nevero, s/n",
                CodigoPostal = "06006"
            },

            // ===== ÁREA INSULAR (Canarias, Baleares) =====
            new CentroTratamiento
            {
                Codigo = "CTA-PMI",
                Nombre = "CTA Palma de Mallorca",
                Area = AreaZonal.Insular,
                Provincia = "Islas Baleares",
                Ciudad = "Palma de Mallorca",
                Direccion = "Centro Logístico Aeropuerto Son Sant Joan",
                CodigoPostal = "07199",
                EsNodoAereo = true,
                EsNodoMaritimo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-LPA",
                Nombre = "CTA Las Palmas de Gran Canaria",
                Area = AreaZonal.Insular,
                Provincia = "Las Palmas",
                Ciudad = "Las Palmas de Gran Canaria",
                Direccion = "Centro Logístico Puerto de La Luz",
                CodigoPostal = "35008",
                EsNodoAereo = true,
                EsNodoMaritimo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-TFE",
                Nombre = "CTA Santa Cruz de Tenerife",
                Area = AreaZonal.Insular,
                Provincia = "Santa Cruz de Tenerife",
                Ciudad = "Santa Cruz de Tenerife",
                Direccion = "Centro Logístico Puerto de Santa Cruz",
                CodigoPostal = "38001",
                EsNodoAereo = true,
                EsNodoMaritimo = true
            },
            new CentroTratamiento
            {
                Codigo = "CTA-CEU",
                Nombre = "CTA Ceuta-Melilla (Hub Málaga)",
                Area = AreaZonal.Sur,
                Provincia = "Ceuta/Melilla",
                Ciudad = "Málaga",
                Direccion = "Terminal Marítima del Puerto de Málaga",
                CodigoPostal = "29001",
                EsNodoMaritimo = true
            }
        ];
    }

    /// <summary>
    /// Crea las 52 rutas de enrutamiento (prefijo CP → CTA).
    /// Los 2 primeros dígitos del código postal determinan la provincia
    /// y, por extensión, el CTA que debe gestionar el envío.
    /// </summary>
    private static List<RutaCta> CrearRutasCta(List<CentroTratamiento> ctas)
    {
        // Diccionario rápido por código
        var ctaPorCodigo = ctas.ToDictionary(c => c.Codigo, c => c.Id);

        return
        [
            // ===== ÁREA NOROESTE =====
            // CTA-COR: Galicia
            new RutaCta { PrefijoCp = "15", Provincia = "A Coruña", CtaId = ctaPorCodigo["CTA-COR"] },
            new RutaCta { PrefijoCp = "27", Provincia = "Lugo", CtaId = ctaPorCodigo["CTA-COR"] },
            new RutaCta { PrefijoCp = "32", Provincia = "Ourense", CtaId = ctaPorCodigo["CTA-COR"] },
            new RutaCta { PrefijoCp = "36", Provincia = "Pontevedra", CtaId = ctaPorCodigo["CTA-COR"] },
            // CTA-GIJ: Asturias, León, Zamora
            new RutaCta { PrefijoCp = "33", Provincia = "Asturias", CtaId = ctaPorCodigo["CTA-GIJ"] },
            new RutaCta { PrefijoCp = "24", Provincia = "León", CtaId = ctaPorCodigo["CTA-GIJ"] },
            new RutaCta { PrefijoCp = "49", Provincia = "Zamora", CtaId = ctaPorCodigo["CTA-GIJ"] },

            // ===== ÁREA NORTE =====
            // CTA-BIL: País Vasco, Cantabria
            new RutaCta { PrefijoCp = "01", Provincia = "Álava", CtaId = ctaPorCodigo["CTA-BIL"] },
            new RutaCta { PrefijoCp = "20", Provincia = "Guipúzcoa", CtaId = ctaPorCodigo["CTA-BIL"] },
            new RutaCta { PrefijoCp = "48", Provincia = "Vizcaya", CtaId = ctaPorCodigo["CTA-BIL"] },
            new RutaCta { PrefijoCp = "39", Provincia = "Cantabria", CtaId = ctaPorCodigo["CTA-BIL"] },
            // CTA-PNA: Navarra, La Rioja, norte CyL
            new RutaCta { PrefijoCp = "31", Provincia = "Navarra", CtaId = ctaPorCodigo["CTA-PNA"] },
            new RutaCta { PrefijoCp = "26", Provincia = "La Rioja", CtaId = ctaPorCodigo["CTA-PNA"] },
            new RutaCta { PrefijoCp = "09", Provincia = "Burgos", CtaId = ctaPorCodigo["CTA-PNA"] },
            new RutaCta { PrefijoCp = "34", Provincia = "Palencia", CtaId = ctaPorCodigo["CTA-PNA"] },
            new RutaCta { PrefijoCp = "42", Provincia = "Soria", CtaId = ctaPorCodigo["CTA-PNA"] },

            // ===== ÁREA NORESTE =====
            // CTA-BCN: Cataluña
            new RutaCta { PrefijoCp = "08", Provincia = "Barcelona", CtaId = ctaPorCodigo["CTA-BCN"] },
            new RutaCta { PrefijoCp = "17", Provincia = "Girona", CtaId = ctaPorCodigo["CTA-BCN"] },
            new RutaCta { PrefijoCp = "25", Provincia = "Lleida", CtaId = ctaPorCodigo["CTA-BCN"] },
            new RutaCta { PrefijoCp = "43", Provincia = "Tarragona", CtaId = ctaPorCodigo["CTA-BCN"] },
            // CTA-ZGZ: Aragón
            new RutaCta { PrefijoCp = "50", Provincia = "Zaragoza", CtaId = ctaPorCodigo["CTA-ZGZ"] },
            new RutaCta { PrefijoCp = "22", Provincia = "Huesca", CtaId = ctaPorCodigo["CTA-ZGZ"] },
            new RutaCta { PrefijoCp = "44", Provincia = "Teruel", CtaId = ctaPorCodigo["CTA-ZGZ"] },

            // ===== ÁREA CENTRO =====
            // CTA-MAD: Madrid, Castilla-La Mancha
            new RutaCta { PrefijoCp = "28", Provincia = "Madrid", CtaId = ctaPorCodigo["CTA-MAD"] },
            new RutaCta { PrefijoCp = "19", Provincia = "Guadalajara", CtaId = ctaPorCodigo["CTA-MAD"] },
            new RutaCta { PrefijoCp = "45", Provincia = "Toledo", CtaId = ctaPorCodigo["CTA-MAD"] },
            new RutaCta { PrefijoCp = "16", Provincia = "Cuenca", CtaId = ctaPorCodigo["CTA-MAD"] },
            new RutaCta { PrefijoCp = "13", Provincia = "Ciudad Real", CtaId = ctaPorCodigo["CTA-MAD"] },
            new RutaCta { PrefijoCp = "02", Provincia = "Albacete", CtaId = ctaPorCodigo["CTA-MAD"] },
            // CTA-VLL: Centro-norte CyL
            new RutaCta { PrefijoCp = "47", Provincia = "Valladolid", CtaId = ctaPorCodigo["CTA-VLL"] },
            new RutaCta { PrefijoCp = "37", Provincia = "Salamanca", CtaId = ctaPorCodigo["CTA-VLL"] },
            new RutaCta { PrefijoCp = "05", Provincia = "Ávila", CtaId = ctaPorCodigo["CTA-VLL"] },
            new RutaCta { PrefijoCp = "40", Provincia = "Segovia", CtaId = ctaPorCodigo["CTA-VLL"] },

            // ===== ÁREA ESTE =====
            // CTA-VLC: Valencia, Castellón
            new RutaCta { PrefijoCp = "46", Provincia = "Valencia", CtaId = ctaPorCodigo["CTA-VLC"] },
            new RutaCta { PrefijoCp = "12", Provincia = "Castellón", CtaId = ctaPorCodigo["CTA-VLC"] },
            // CTA-ALI: Alicante, Murcia
            new RutaCta { PrefijoCp = "03", Provincia = "Alicante", CtaId = ctaPorCodigo["CTA-ALI"] },
            new RutaCta { PrefijoCp = "30", Provincia = "Murcia", CtaId = ctaPorCodigo["CTA-ALI"] },

            // ===== ÁREA SUR =====
            // CTA-SEV: Andalucía occidental, Extremadura
            new RutaCta { PrefijoCp = "41", Provincia = "Sevilla", CtaId = ctaPorCodigo["CTA-SEV"] },
            new RutaCta { PrefijoCp = "21", Provincia = "Huelva", CtaId = ctaPorCodigo["CTA-SEV"] },
            new RutaCta { PrefijoCp = "11", Provincia = "Cádiz", CtaId = ctaPorCodigo["CTA-SEV"] },
            new RutaCta { PrefijoCp = "14", Provincia = "Córdoba", CtaId = ctaPorCodigo["CTA-SEV"] },
            // CTA-MAL: Andalucía oriental
            new RutaCta { PrefijoCp = "29", Provincia = "Málaga", CtaId = ctaPorCodigo["CTA-MAL"] },
            new RutaCta { PrefijoCp = "18", Provincia = "Granada", CtaId = ctaPorCodigo["CTA-MAL"] },
            new RutaCta { PrefijoCp = "04", Provincia = "Almería", CtaId = ctaPorCodigo["CTA-MAL"] },
            new RutaCta { PrefijoCp = "23", Provincia = "Jaén", CtaId = ctaPorCodigo["CTA-MAL"] },
            // CTA-BAD: Extremadura
            new RutaCta { PrefijoCp = "06", Provincia = "Badajoz", CtaId = ctaPorCodigo["CTA-BAD"] },
            new RutaCta { PrefijoCp = "10", Provincia = "Cáceres", CtaId = ctaPorCodigo["CTA-BAD"] },
            // CTA-CEU: Ceuta y Melilla (hub marítimo en Málaga)
            new RutaCta { PrefijoCp = "51", Provincia = "Ceuta", CtaId = ctaPorCodigo["CTA-CEU"] },
            new RutaCta { PrefijoCp = "52", Provincia = "Melilla", CtaId = ctaPorCodigo["CTA-CEU"] },

            // ===== ÁREA INSULAR =====
            // CTA-PMI: Baleares
            new RutaCta { PrefijoCp = "07", Provincia = "Islas Baleares", CtaId = ctaPorCodigo["CTA-PMI"] },
            // CTA-LPA: Canarias oriental
            new RutaCta { PrefijoCp = "35", Provincia = "Las Palmas", CtaId = ctaPorCodigo["CTA-LPA"] },
            // CTA-TFE: Canarias occidental
            new RutaCta { PrefijoCp = "38", Provincia = "S/C de Tenerife", CtaId = ctaPorCodigo["CTA-TFE"] }
        ];
    }

    /// <summary>
    /// Asigna cada operario de CTA a UN único CTA (Madrid o Barcelona).
    ///
    /// Distribución:
    ///   - CTA-MAD: Pedro Martínez (OperarioCTA), Laura Fernández (Supervisor)
    ///   - CTA-BCN: Sergio Romero (OperarioCTA), Marta Jiménez (Supervisor)
    ///
    /// Nota: el Admin NO se asigna a ningún CTA. Su rol global (Rol.Admin)
    /// le da acceso a todos los centros sin necesidad de una fila en OperariosCta.
    ///
    /// El resto de CTAs queda sin operarios sembrados; se pueden añadir manualmente
    /// desde la intranet (alta de personal) cuando se necesite.
    /// </summary>
    private static List<OperarioCta> CrearOperariosCta(List<CentroTratamiento> ctas)
    {
        var ctaMadrid = ctas.First(c => c.Codigo == "CTA-MAD");
        var ctaBarcelona = ctas.First(c => c.Codigo == "CTA-BCN");

        var asignaciones = new[]
        {
            new { IdentityUserId = OperarioCtaSeedId,  Nombre = "Pedro Martínez Ruiz",  Codigo = "OPL001", Rol = RolOperario.OperarioCTA, CtaId = ctaMadrid.Id },
            new { IdentityUserId = OperarioCta2SeedId, Nombre = "Sergio Romero Vega",   Codigo = "OPL002", Rol = RolOperario.OperarioCTA, CtaId = ctaBarcelona.Id },
            new { IdentityUserId = SupervisorSeedId,   Nombre = "Laura Fernández Díaz", Codigo = "OPJ001", Rol = RolOperario.Supervisor,  CtaId = ctaMadrid.Id },
            new { IdentityUserId = Supervisor2SeedId,  Nombre = "Marta Jiménez Castro", Codigo = "OPJ002", Rol = RolOperario.Supervisor,  CtaId = ctaBarcelona.Id },
        };

        return asignaciones
            .Select(a => new OperarioCta
            {
                IdentityUserId = a.IdentityUserId,
                NombreCompleto = a.Nombre,
                CodigoEmpleado = a.Codigo,
                Rol = a.Rol,
                CentroTratamientoId = a.CtaId
            })
            .ToList();
    }

    /// <summary>
    /// Asigna operarios de oficina a las oficinas de Madrid y Barcelona.
    ///
    /// Distribución:
    ///   - María García (operario@nexopostal.es) → OperarioOficina en oficinas de MADRID
    ///   - Diego Herrera (operario2@nexopostal.es) → OperarioOficina en oficinas de BARCELONA
    ///
    /// El resto de oficinas (resto de España) queda sin operarios sembrados;
    /// se pueden añadir manualmente desde la intranet (alta de personal).
    /// </summary>
    private static List<OperarioOficina> CrearOperariosOficina(List<DTOs.OficinaJsonDto> oficinas)
    {
        // Cada operario opera desde UNA sola oficina (la principal de su ciudad).
        // Asignar varias filas activas a la vez choca con el índice único
        // (IdentityUserId, OficinaJsonId) cuando un Admin intenta reasignar.
        var oficinaMadrid = oficinas
            .Where(o => o.Ciudad.Equals("MADRID", StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Id)
            .FirstOrDefault();
        var oficinaBarcelona = oficinas
            .Where(o => o.Ciudad.Equals("BARCELONA", StringComparison.OrdinalIgnoreCase))
            .OrderBy(o => o.Id)
            .FirstOrDefault();

        var operariosOficina = new List<OperarioOficina>();

        if (oficinaMadrid != null)
        {
            operariosOficina.Add(new OperarioOficina
            {
                IdentityUserId = OperarioOficinaSeedId,
                NombreCompleto = "María García López",
                CodigoEmpleado = "OPE001",
                Rol = RolOperario.OperarioOficina,
                OficinaJsonId = oficinaMadrid.Id,
                OficinaNombre = oficinaMadrid.Nombre
            });
        }

        if (oficinaBarcelona != null)
        {
            operariosOficina.Add(new OperarioOficina
            {
                IdentityUserId = OperarioOficina2SeedId,
                NombreCompleto = "Diego Herrera Ortiz",
                CodigoEmpleado = "OPE002",
                Rol = RolOperario.OperarioOficina,
                OficinaJsonId = oficinaBarcelona.Id,
                OficinaNombre = oficinaBarcelona.Nombre
            });
        }

        return operariosOficina;
    }
}
