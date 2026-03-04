namespace Nexopostal.Intranet.DTOs;

// ============================================================
//  DTOs para Centros de Tratamiento Automatizado (CTAs)
// ============================================================

/// <summary>
/// Resumen de un CTA para listados.
/// </summary>
public class CtaResumenDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public bool EsNodoAereo { get; set; }
    public bool EsNodoMaritimo { get; set; }
    public bool Activo { get; set; }
    public int TotalOperarios { get; set; }
}

/// <summary>
/// Detalle completo de un CTA con sus operarios y rutas asignadas.
/// </summary>
public class CtaDetalleDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public bool EsNodoAereo { get; set; }
    public bool EsNodoMaritimo { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public List<OperarioResumenDto> Operarios { get; set; } = [];
    public List<RutaCtaDto> RutasAsignadas { get; set; } = [];
}

/// <summary>
/// Ruta de enrutamiento: prefijo CP → CTA.
/// </summary>
public class RutaCtaDto
{
    public string PrefijoCp { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de resolver un código postal a su CTA correspondiente.
/// </summary>
public class ResolverCtaResponseDto
{
    public string CodigoPostal { get; set; } = string.Empty;
    public string PrefijoCp { get; set; } = string.Empty;
    public string Provincia { get; set; } = string.Empty;
    public int CtaId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;
    public string CtaNombre { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
}

/// <summary>
/// Estadísticas del dashboard de un CTA.
/// </summary>
public class DashboardCtaDto
{
    public int CtaId { get; set; }
    public string CtaCodigo { get; set; } = string.Empty;
    public string CtaNombre { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;

    // Operarios
    public int TotalOperarios { get; set; }
    public int OperariosActivos { get; set; }

    // Asignaciones
    public int TareasPendientes { get; set; }
    public int TareasEnProgreso { get; set; }
    public int TareasCompletadasHoy { get; set; }
    public int TareasUrgentes { get; set; }

    // Movimientos
    public int MovimientosProgramados { get; set; }
    public int MovimientosEnTransito { get; set; }
    public int MovimientosRecibidosHoy { get; set; }

    // Incidencias
    public int IncidenciasAbiertas { get; set; }
    public int IncidenciasEnRevision { get; set; }
}

/// <summary>
/// Dashboard global de administración: agrupa estadísticas de todos los CTAs.
/// Solo accesible por administradores.
/// </summary>
public class DashboardAdminDto
{
    // Red logística
    public int TotalCtas { get; set; }
    public int CtasActivos { get; set; }

    // Operarios globales
    public int TotalOperarios { get; set; }
    public int OperariosActivos { get; set; }

    // Asignaciones globales
    public int TareasPendientesGlobal { get; set; }
    public int TareasEnProgresoGlobal { get; set; }
    public int TareasCompletadasHoyGlobal { get; set; }
    public int TareasUrgentesGlobal { get; set; }

    // Movimientos globales
    public int MovimientosProgramadosGlobal { get; set; }
    public int MovimientosEnTransitoGlobal { get; set; }
    public int MovimientosRecibidosHoyGlobal { get; set; }

    // Incidencias globales
    public int IncidenciasAbiertasGlobal { get; set; }
    public int IncidenciasEnRevisionGlobal { get; set; }

    // Detalle por CTA
    public List<DashboardCtaDto> DetallePorCta { get; set; } = [];
}
