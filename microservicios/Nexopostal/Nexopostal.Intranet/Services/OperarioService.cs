using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio para gestionar operarios asignados a CTAs.
/// </summary>
public interface IOperarioService
{
    /// <summary>Obtiene el primer operario activo vinculado al IdentityUserId</summary>
    Task<OperarioCta?> ObtenerPorIdentityUserId(string identityUserId);

    /// <summary>Obtiene TODOS los operarios activos vinculados al IdentityUserId (uno por CTA)</summary>
    Task<List<OperarioCta>> ObtenerTodosPorIdentityUserId(string identityUserId);

    /// <summary>Obtiene la info resumida del CTA del operario autenticado (primer CTA)</summary>
    Task<MiCtaInfoDto?> ObtenerMiCtaInfo(string identityUserId);

    /// <summary>Obtiene la info de TODOS los CTAs del operario autenticado</summary>
    Task<MisCtasInfoDto?> ObtenerMisCtasInfo(string identityUserId);

    /// <summary>Obtiene todos los operarios de un CTA</summary>
    Task<List<OperarioResumenDto>> ObtenerOperariosCta(int ctaId);

    /// <summary>Obtiene el detalle de un operario</summary>
    Task<OperarioDetalleDto?> ObtenerDetalle(int operarioId);

    /// <summary>Crea un nuevo operario y lo asigna a un CTA</summary>
    Task<OperarioResumenDto> CrearOperario(CrearOperarioDto dto);

    /// <summary>Desactiva un operario</summary>
    Task<bool> DesactivarOperario(int operarioId);
}

public class OperarioService : IOperarioService
{
    private readonly IOperarioCtaRepository _operarioRepo;
    private readonly ICentroTratamientoRepository _ctaRepo;
    private readonly IAsignacionPaqueteRepository _asignacionRepo;
    private readonly ILogger<OperarioService> _logger;

    public OperarioService(
        IOperarioCtaRepository operarioRepo,
        ICentroTratamientoRepository ctaRepo,
        IAsignacionPaqueteRepository asignacionRepo,
        ILogger<OperarioService> logger)
    {
        _operarioRepo = operarioRepo;
        _ctaRepo = ctaRepo;
        _asignacionRepo = asignacionRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OperarioCta?> ObtenerPorIdentityUserId(string identityUserId)
    {
        return await _operarioRepo.GetByIdentityUserIdAsync(identityUserId);
    }

    /// <inheritdoc />
    public async Task<List<OperarioCta>> ObtenerTodosPorIdentityUserId(string identityUserId)
    {
        return await _operarioRepo.GetAllByIdentityUserIdAsync(identityUserId);
    }

    /// <inheritdoc />
    public async Task<MiCtaInfoDto?> ObtenerMiCtaInfo(string identityUserId)
    {
        var operario = await ObtenerPorIdentityUserId(identityUserId);
        if (operario == null) return null;

        return new MiCtaInfoDto
        {
            OperarioId = operario.Id,
            NombreCompleto = operario.NombreCompleto,
            CodigoEmpleado = operario.CodigoEmpleado,
            Rol = operario.Rol.ToString(),
            CtaId = operario.CentroTratamiento.Id,
            CtaCodigo = operario.CentroTratamiento.Codigo,
            CtaNombre = operario.CentroTratamiento.Nombre,
            Area = operario.CentroTratamiento.Area.ToString()
        };
    }

    /// <inheritdoc />
    public async Task<MisCtasInfoDto?> ObtenerMisCtasInfo(string identityUserId)
    {
        var operarios = await ObtenerTodosPorIdentityUserId(identityUserId);
        if (operarios.Count == 0) return null;

        var primero = operarios.First();
        return new MisCtasInfoDto
        {
            NombreCompleto = primero.NombreCompleto,
            CodigoEmpleado = primero.CodigoEmpleado,
            Rol = primero.Rol.ToString(),
            Ctas = operarios.Select(o => new CtaAsignacionDto
            {
                OperarioCtaId = o.Id,
                CtaId = o.CentroTratamiento.Id,
                CtaCodigo = o.CentroTratamiento.Codigo,
                CtaNombre = o.CentroTratamiento.Nombre,
                Area = o.CentroTratamiento.Area.ToString()
            }).ToList()
        };
    }

    /// <inheritdoc />
    public async Task<List<OperarioResumenDto>> ObtenerOperariosCta(int ctaId)
    {
        var operarios = await _operarioRepo.GetByCtaIdAsync(ctaId, soloActivos: null);
        return operarios
            .OrderBy(o => o.Rol)
            .ThenBy(o => o.NombreCompleto)
            .Select(o => new OperarioResumenDto
            {
                Id = o.Id,
                NombreCompleto = o.NombreCompleto,
                CodigoEmpleado = o.CodigoEmpleado,
                Rol = o.Rol.ToString(),
                Activo = o.Activo,
                FechaAsignacion = o.FechaAsignacion
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<OperarioDetalleDto?> ObtenerDetalle(int operarioId)
    {
        var operario = await _operarioRepo.GetWithCtaAsync(operarioId);
        if (operario == null) return null;

        return new OperarioDetalleDto
        {
            Id = operario.Id,
            IdentityUserId = operario.IdentityUserId,
            NombreCompleto = operario.NombreCompleto,
            CodigoEmpleado = operario.CodigoEmpleado,
            Rol = operario.Rol.ToString(),
            CentroTratamientoId = operario.CentroTratamientoId,
            CtaCodigo = operario.CentroTratamiento.Codigo,
            CtaNombre = operario.CentroTratamiento.Nombre,
            Activo = operario.Activo,
            FechaAsignacion = operario.FechaAsignacion,
            TareasPendientes = await _asignacionRepo.CountByOperarioAndEstadoAsync(operarioId, EstadoTarea.Pendiente),
            TareasEnProgreso = await _asignacionRepo.CountByOperarioAndEstadoAsync(operarioId, EstadoTarea.EnProgreso),
            TareasCompletadasHoy = await _asignacionRepo.CountCompletadasHoyByOperarioAsync(operarioId)
        };
    }

    /// <inheritdoc />
    public async Task<OperarioResumenDto> CrearOperario(CrearOperarioDto dto)
    {
        if (!Enum.TryParse<RolOperario>(dto.Rol, true, out var rol))
            throw new ArgumentException($"Rol no válido: {dto.Rol}");

        // Verificar que el CTA existe
        var cta = await _ctaRepo.GetByIdAsync(dto.CentroTratamientoId)
            ?? throw new ArgumentException($"CTA con ID {dto.CentroTratamientoId} no encontrado");

        // Verificar que no existe un operario con ese IdentityUserId en ese CTA
        var existe = await _operarioRepo.ExistsByIdentityUserIdAndCtaAsync(dto.IdentityUserId, dto.CentroTratamientoId);
        if (existe)
            throw new InvalidOperationException("Este operario ya está asignado a ese CTA");

        var operario = new OperarioCta
        {
            IdentityUserId = dto.IdentityUserId,
            NombreCompleto = dto.NombreCompleto,
            CodigoEmpleado = dto.CodigoEmpleado,
            Rol = rol,
            CentroTratamientoId = dto.CentroTratamientoId
        };

        await _operarioRepo.CreateAsync(operario);

        _logger.LogInformation("Operario {Nombre} ({Codigo}) creado y asignado a {Cta}",
            operario.NombreCompleto, operario.CodigoEmpleado, cta.Codigo);

        return new OperarioResumenDto
        {
            Id = operario.Id,
            NombreCompleto = operario.NombreCompleto,
            CodigoEmpleado = operario.CodigoEmpleado,
            Rol = operario.Rol.ToString(),
            Activo = operario.Activo,
            FechaAsignacion = operario.FechaAsignacion
        };
    }

    /// <inheritdoc />
    public async Task<bool> DesactivarOperario(int operarioId)
    {
        var operario = await _operarioRepo.GetByIdAsync(operarioId);
        if (operario == null) return false;

        operario.Activo = false;
        await _operarioRepo.UpdateAsync(operario);

        _logger.LogInformation("Operario {Codigo} desactivado", operario.CodigoEmpleado);
        return true;
    }
}
