using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Operaciones administrativas sobre el catálogo de Centros de Tratamiento Automatizado (CTAs).
/// Solo accesible por rol Admin.
/// </summary>
public interface IAdminCtaService
{
    Task<(CtaDetalleDto? cta, string? error)> CrearCta(CrearCtaDto dto);
    Task<(CtaDetalleDto? cta, string? error)> EditarCta(int id, EditarCtaDto dto);
    Task<(bool ok, string? error)> DesactivarCta(int id);
    Task<(bool ok, string? error)> ReactivarCta(int id);
}

public class AdminCtaService : IAdminCtaService
{
    private readonly ICentroTratamientoRepository _ctaRepo;
    private readonly IOperarioCtaRepository _operarioRepo;
    private readonly IAsignacionPaqueteRepository _asignacionRepo;
    private readonly IMovimientoPaqueteRepository _movimientoRepo;
    private readonly IClasificacionService _clasificacionService;
    private readonly ILogger<AdminCtaService> _logger;

    public AdminCtaService(
        ICentroTratamientoRepository ctaRepo,
        IOperarioCtaRepository operarioRepo,
        IAsignacionPaqueteRepository asignacionRepo,
        IMovimientoPaqueteRepository movimientoRepo,
        IClasificacionService clasificacionService,
        ILogger<AdminCtaService> logger)
    {
        _ctaRepo = ctaRepo;
        _operarioRepo = operarioRepo;
        _asignacionRepo = asignacionRepo;
        _movimientoRepo = movimientoRepo;
        _clasificacionService = clasificacionService;
        _logger = logger;
    }

    public async Task<(CtaDetalleDto?, string?)> CrearCta(CrearCtaDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Codigo)) return (null, "El código es obligatorio.");
        if (string.IsNullOrWhiteSpace(dto.Nombre)) return (null, "El nombre es obligatorio.");
        if (!Enum.TryParse<AreaZonal>(dto.Area, ignoreCase: true, out var area))
            return (null, $"Área zonal inválida: {dto.Area}");

        var codigo = dto.Codigo.Trim().ToUpperInvariant();
        var existente = await _ctaRepo.GetByCodigoAsync(codigo);
        if (existente != null)
            return (null, $"Ya existe un CTA con el código {codigo}");

        var entity = new CentroTratamiento
        {
            Codigo = codigo,
            Nombre = dto.Nombre.Trim(),
            Area = area,
            Provincia = dto.Provincia.Trim(),
            Ciudad = dto.Ciudad.Trim(),
            Direccion = dto.Direccion.Trim(),
            CodigoPostal = dto.CodigoPostal.Trim(),
            EsNodoAereo = dto.EsNodoAereo,
            EsNodoMaritimo = dto.EsNodoMaritimo,
            Activo = true,
            FechaCreacion = DateTime.UtcNow
        };

        await _ctaRepo.CreateAsync(entity);
        _logger.LogInformation("Admin creó CTA {Codigo} (id={Id})", entity.Codigo, entity.Id);

        var detalle = await _clasificacionService.ObtenerCtaDetalle(entity.Id);
        return (detalle, null);
    }

    public async Task<(CtaDetalleDto?, string?)> EditarCta(int id, EditarCtaDto dto)
    {
        var cta = await _ctaRepo.GetByIdAsync(id);
        if (cta == null) return (null, "CTA no encontrado.");

        if (string.IsNullOrWhiteSpace(dto.Nombre)) return (null, "El nombre es obligatorio.");
        if (!Enum.TryParse<AreaZonal>(dto.Area, ignoreCase: true, out var area))
            return (null, $"Área zonal inválida: {dto.Area}");

        cta.Nombre = dto.Nombre.Trim();
        cta.Area = area;
        cta.Provincia = dto.Provincia.Trim();
        cta.Ciudad = dto.Ciudad.Trim();
        cta.Direccion = dto.Direccion.Trim();
        cta.CodigoPostal = dto.CodigoPostal.Trim();
        cta.EsNodoAereo = dto.EsNodoAereo;
        cta.EsNodoMaritimo = dto.EsNodoMaritimo;

        await _ctaRepo.UpdateAsync(cta);
        _logger.LogInformation("Admin editó CTA {Codigo} (id={Id})", cta.Codigo, cta.Id);

        var detalle = await _clasificacionService.ObtenerCtaDetalle(cta.Id);
        return (detalle, null);
    }

    public async Task<(bool, string?)> DesactivarCta(int id)
    {
        var cta = await _ctaRepo.GetByIdAsync(id);
        if (cta == null) return (false, "CTA no encontrado.");
        if (!cta.Activo) return (true, null);

        // Guard: no permitir desactivar si hay operarios activos
        var operariosActivos = await _operarioRepo.CountByCtaIdAsync(id, true);
        if (operariosActivos > 0)
            return (false, $"No se puede desactivar: el CTA tiene {operariosActivos} operarios activos. Reasígnalos primero.");

        // Guard: tareas pendientes / en progreso
        var pendientes = await _asignacionRepo.CountByCtaAndEstadoAsync(id, EstadoTarea.Pendiente);
        var enProgreso = await _asignacionRepo.CountByCtaAndEstadoAsync(id, EstadoTarea.EnProgreso);
        if (pendientes + enProgreso > 0)
            return (false, $"No se puede desactivar: hay {pendientes + enProgreso} tareas pendientes/en progreso.");

        // Guard: movimientos programados / en tránsito
        var movProg = await _movimientoRepo.CountByCtaAndEstadoAsync(id, EstadoMovimiento.Programado);
        var movTran = await _movimientoRepo.CountByCtaAndEstadoAsync(id, EstadoMovimiento.EnTransito);
        if (movProg + movTran > 0)
            return (false, $"No se puede desactivar: hay {movProg + movTran} movimientos troncales activos.");

        cta.Activo = false;
        await _ctaRepo.UpdateAsync(cta);
        _logger.LogInformation("Admin desactivó CTA {Codigo} (id={Id})", cta.Codigo, cta.Id);
        return (true, null);
    }

    public async Task<(bool, string?)> ReactivarCta(int id)
    {
        var cta = await _ctaRepo.GetByIdAsync(id);
        if (cta == null) return (false, "CTA no encontrado.");
        if (cta.Activo) return (true, null);

        cta.Activo = true;
        await _ctaRepo.UpdateAsync(cta);
        _logger.LogInformation("Admin reactivó CTA {Codigo} (id={Id})", cta.Codigo, cta.Id);
        return (true, null);
    }
}
