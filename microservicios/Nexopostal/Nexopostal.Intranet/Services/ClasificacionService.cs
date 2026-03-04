using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio de clasificación y enrutamiento de paquetes.
/// Determina el CTA de destino según el código postal y el tipo de transporte.
/// </summary>
public interface IClasificacionService
{
    /// <summary>Resuelve el CTA de destino para un código postal dado</summary>
    Task<ResolverCtaResponseDto?> ResolverCtaDestino(string codigoPostal);

    /// <summary>Determina el tipo de transporte óptimo entre dos CTAs</summary>
    Task<TipoTransporte> DeterminarTipoTransporte(int ctaOrigenId, int ctaDestinoId, bool esUrgente);

    /// <summary>Obtiene todos los CTAs</summary>
    Task<List<CtaResumenDto>> ObtenerTodosCtas();

    /// <summary>Obtiene el detalle de un CTA con sus operarios y rutas</summary>
    Task<CtaDetalleDto?> ObtenerCtaDetalle(int ctaId);

    /// <summary>Obtiene el dashboard de estadísticas de un CTA</summary>
    Task<DashboardCtaDto?> ObtenerDashboardCta(int ctaId);

    /// <summary>Obtiene el dashboard global de administración agregando todos los CTAs</summary>
    Task<DashboardAdminDto> ObtenerDashboardAdmin();
}

public class ClasificacionService : IClasificacionService
{
    private readonly ICentroTratamientoRepository _ctaRepo;
    private readonly IRutaCtaRepository _rutaRepo;
    private readonly IOperarioCtaRepository _operarioRepo;
    private readonly IAsignacionPaqueteRepository _asignacionRepo;
    private readonly IMovimientoPaqueteRepository _movimientoRepo;
    private readonly IIncidenciaRepository _incidenciaRepo;
    private readonly ILogger<ClasificacionService> _logger;

    public ClasificacionService(
        ICentroTratamientoRepository ctaRepo,
        IRutaCtaRepository rutaRepo,
        IOperarioCtaRepository operarioRepo,
        IAsignacionPaqueteRepository asignacionRepo,
        IMovimientoPaqueteRepository movimientoRepo,
        IIncidenciaRepository incidenciaRepo,
        ILogger<ClasificacionService> logger)
    {
        _ctaRepo = ctaRepo;
        _rutaRepo = rutaRepo;
        _operarioRepo = operarioRepo;
        _asignacionRepo = asignacionRepo;
        _movimientoRepo = movimientoRepo;
        _incidenciaRepo = incidenciaRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResolverCtaResponseDto?> ResolverCtaDestino(string codigoPostal)
    {
        if (string.IsNullOrWhiteSpace(codigoPostal) || codigoPostal.Length < 2)
            return null;

        var prefijo = codigoPostal[..2];

        var ruta = await _rutaRepo.GetByPrefijoAsync(prefijo);

        if (ruta == null)
        {
            _logger.LogWarning("No se encontró ruta para el prefijo CP: {Prefijo}", prefijo);
            return null;
        }

        return new ResolverCtaResponseDto
        {
            CodigoPostal = codigoPostal,
            PrefijoCp = prefijo,
            Provincia = ruta.Provincia,
            CtaId = ruta.Cta.Id,
            CtaCodigo = ruta.Cta.Codigo,
            CtaNombre = ruta.Cta.Nombre,
            Area = ruta.Cta.Area.ToString()
        };
    }

    /// <inheritdoc />
    public async Task<TipoTransporte> DeterminarTipoTransporte(int ctaOrigenId, int ctaDestinoId, bool esUrgente)
    {
        var ctaOrigen = await _ctaRepo.GetByIdAsync(ctaOrigenId);
        var ctaDestino = await _ctaRepo.GetByIdAsync(ctaDestinoId);

        if (ctaOrigen == null || ctaDestino == null)
            return TipoTransporte.Terrestre;

        // Regla 1: Si el destino es insular
        if (ctaDestino.Area == AreaZonal.Insular || ctaOrigen.Area == AreaZonal.Insular)
        {
            // Urgente → aéreo; Normal → marítimo
            if (esUrgente && (ctaDestino.EsNodoAereo || ctaOrigen.EsNodoAereo))
                return TipoTransporte.Aereo;

            return TipoTransporte.Maritimo;
        }

        // Regla 2: Si destino es Ceuta/Melilla (CTA-CEU tiene nodo marítimo)
        if (ctaDestino.EsNodoMaritimo && !ctaDestino.EsNodoAereo)
        {
            return esUrgente ? TipoTransporte.Aereo : TipoTransporte.Maritimo;
        }

        // Regla 3: Si es urgente y las áreas son diferentes (larga distancia)
        if (esUrgente && ctaOrigen.Area != ctaDestino.Area)
        {
            // Urgente larga distancia → aéreo si hay nodo disponible
            if (ctaOrigen.EsNodoAereo && ctaDestino.EsNodoAereo)
                return TipoTransporte.Aereo;
        }

        // Regla 4: Por defecto → terrestre (camiones nocturnos)
        return TipoTransporte.Terrestre;
    }

    /// <inheritdoc />
    public async Task<List<CtaResumenDto>> ObtenerTodosCtas()
    {
        var ctas = await _ctaRepo.GetAllWithOperariosAsync();
        return ctas.Select(c => new CtaResumenDto
        {
            Id = c.Id,
            Codigo = c.Codigo,
            Nombre = c.Nombre,
            Area = c.Area.ToString(),
            Ciudad = c.Ciudad,
            Provincia = c.Provincia,
            EsNodoAereo = c.EsNodoAereo,
            EsNodoMaritimo = c.EsNodoMaritimo,
            Activo = c.Activo,
            TotalOperarios = c.Operarios.Count(o => o.Activo)
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<CtaDetalleDto?> ObtenerCtaDetalle(int ctaId)
    {
        var cta = await _ctaRepo.GetWithDetailAsync(ctaId);

        if (cta == null) return null;

        return new CtaDetalleDto
        {
            Id = cta.Id,
            Codigo = cta.Codigo,
            Nombre = cta.Nombre,
            Area = cta.Area.ToString(),
            Ciudad = cta.Ciudad,
            Provincia = cta.Provincia,
            Direccion = cta.Direccion,
            CodigoPostal = cta.CodigoPostal,
            EsNodoAereo = cta.EsNodoAereo,
            EsNodoMaritimo = cta.EsNodoMaritimo,
            Activo = cta.Activo,
            FechaCreacion = cta.FechaCreacion,
            Operarios = cta.Operarios
                .Where(o => o.Activo)
                .Select(o => new OperarioResumenDto
                {
                    Id = o.Id,
                    NombreCompleto = o.NombreCompleto,
                    CodigoEmpleado = o.CodigoEmpleado,
                    Rol = o.Rol.ToString(),
                    Activo = o.Activo,
                    FechaAsignacion = o.FechaAsignacion
                })
                .ToList(),
            RutasAsignadas = cta.RutasAsignadas
                .OrderBy(r => r.PrefijoCp)
                .Select(r => new RutaCtaDto
                {
                    PrefijoCp = r.PrefijoCp,
                    Provincia = r.Provincia
                })
                .ToList()
        };
    }

    /// <inheritdoc />
    public async Task<DashboardCtaDto?> ObtenerDashboardCta(int ctaId)
    {
        var cta = await _ctaRepo.GetByIdAsync(ctaId);
        if (cta == null) return null;

        var dashboard = new DashboardCtaDto
        {
            CtaId = cta.Id,
            CtaCodigo = cta.Codigo,
            CtaNombre = cta.Nombre,
            Area = cta.Area.ToString(),

            TotalOperarios = await _operarioRepo.CountByCtaIdAsync(ctaId),
            OperariosActivos = await _operarioRepo.CountByCtaIdAsync(ctaId, true),

            TareasPendientes = await _asignacionRepo.CountByCtaAndEstadoAsync(ctaId, EstadoTarea.Pendiente),
            TareasEnProgreso = await _asignacionRepo.CountByCtaAndEstadoAsync(ctaId, EstadoTarea.EnProgreso),
            TareasCompletadasHoy = await _asignacionRepo.CountCompletadasHoyAsync(ctaId),
            TareasUrgentes = await _asignacionRepo.CountByCtaUrgentesAsync(ctaId),

            MovimientosProgramados = await _movimientoRepo.CountByCtaAndEstadoAsync(ctaId, EstadoMovimiento.Programado),
            MovimientosEnTransito = await _movimientoRepo.CountByCtaAndEstadoAsync(ctaId, EstadoMovimiento.EnTransito),
            MovimientosRecibidosHoy = await _movimientoRepo.CountRecibidosHoyByCtaAsync(ctaId),

            IncidenciasAbiertas = await _incidenciaRepo.CountByCtaAndEstadoAsync(ctaId, EstadoIncidencia.Abierta),
            IncidenciasEnRevision = await _incidenciaRepo.CountByCtaAndEstadoAsync(ctaId, EstadoIncidencia.EnRevision)
        };

        return dashboard;
    }

    /// <inheritdoc />
    public async Task<DashboardAdminDto> ObtenerDashboardAdmin()
    {
        var ctas = await _ctaRepo.GetAllAsync();
        var ctaList = ctas.ToList();

        var detalle = new List<DashboardCtaDto>();
        foreach (var cta in ctaList)
        {
            var d = await ObtenerDashboardCta(cta.Id);
            if (d != null) detalle.Add(d);
        }

        return new DashboardAdminDto
        {
            TotalCtas = ctaList.Count,
            CtasActivos = ctaList.Count(c => c.Activo),
            TotalOperarios = detalle.Sum(d => d.TotalOperarios),
            OperariosActivos = detalle.Sum(d => d.OperariosActivos),
            TareasPendientesGlobal = detalle.Sum(d => d.TareasPendientes),
            TareasEnProgresoGlobal = detalle.Sum(d => d.TareasEnProgreso),
            TareasCompletadasHoyGlobal = detalle.Sum(d => d.TareasCompletadasHoy),
            TareasUrgentesGlobal = detalle.Sum(d => d.TareasUrgentes),
            MovimientosProgramadosGlobal = detalle.Sum(d => d.MovimientosProgramados),
            MovimientosEnTransitoGlobal = detalle.Sum(d => d.MovimientosEnTransito),
            MovimientosRecibidosHoyGlobal = detalle.Sum(d => d.MovimientosRecibidosHoy),
            IncidenciasAbiertasGlobal = detalle.Sum(d => d.IncidenciasAbiertas),
            IncidenciasEnRevisionGlobal = detalle.Sum(d => d.IncidenciasEnRevision),
            DetallePorCta = detalle
        };
    }
}
