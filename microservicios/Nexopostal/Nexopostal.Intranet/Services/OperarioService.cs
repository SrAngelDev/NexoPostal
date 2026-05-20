using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Hubs;
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

    /// <summary>Obtiene el detalle operativo de un usuario por IdentityUserId (vista admin).</summary>
    Task<AdminOperarioDetalleDto?> ObtenerDetalleAdminPorIdentityUserId(string identityUserId);

    /// <summary>Mueve una asignación de CTA de un usuario (vista admin).</summary>
    Task<(bool Ok, string? Error, bool Conflict)> ActualizarCtaAdmin(string identityUserId, AdminActualizarCtaDto dto);

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
    private readonly IHubContext<IntranetHub> _hubContext;
    private readonly ILogger<OperarioService> _logger;

    public OperarioService(
        IOperarioCtaRepository operarioRepo,
        ICentroTratamientoRepository ctaRepo,
        IAsignacionPaqueteRepository asignacionRepo,
        IHubContext<IntranetHub> hubContext,
        ILogger<OperarioService> logger)
    {
        _operarioRepo = operarioRepo;
        _ctaRepo = ctaRepo;
        _asignacionRepo = asignacionRepo;
        _hubContext = hubContext;
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
    public async Task<AdminOperarioDetalleDto?> ObtenerDetalleAdminPorIdentityUserId(string identityUserId)
    {
        var asignaciones = await _operarioRepo.GetAllByIdentityUserIdAsync(identityUserId);
        if (asignaciones.Count == 0)
            return null;

        var primero = asignaciones.First();
        var detalle = new AdminOperarioDetalleDto
        {
            IdentityUserId = identityUserId,
            NombreCompleto = primero.NombreCompleto,
            CodigoEmpleado = primero.CodigoEmpleado
        };

        foreach (var operario in asignaciones
            .OrderByDescending(o => o.Activo)
            .ThenBy(o => o.CentroTratamiento.Codigo))
        {
            detalle.AsignacionesCta.Add(new AdminOperarioCtaAsignacionDto
            {
                OperarioCtaId = operario.Id,
                CtaId = operario.CentroTratamientoId,
                CtaCodigo = operario.CentroTratamiento.Codigo,
                CtaNombre = operario.CentroTratamiento.Nombre,
                Area = operario.CentroTratamiento.Area.ToString(),
                RolOperativo = operario.Rol.ToString(),
                Activo = operario.Activo,
                FechaAsignacion = operario.FechaAsignacion,
                TareasPendientes = await _asignacionRepo.CountByOperarioAndEstadoAsync(operario.Id, EstadoTarea.Pendiente),
                TareasEnProgreso = await _asignacionRepo.CountByOperarioAndEstadoAsync(operario.Id, EstadoTarea.EnProgreso),
                TareasCompletadasHoy = await _asignacionRepo.CountCompletadasHoyByOperarioAsync(operario.Id)
            });
        }

        return detalle;
    }

    /// <inheritdoc />
    public async Task<(bool Ok, string? Error, bool Conflict)> ActualizarCtaAdmin(string identityUserId, AdminActualizarCtaDto dto)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
            return (false, "IdentityUserId no válido.", false);

        var asignaciones = await _operarioRepo.GetAllByIdentityUserIdAsync(identityUserId);
        if (asignaciones.Count == 0)
            return (false, "No hay asignaciones CTA para este usuario.", false);

        OperarioCta? asignacion = null;
        if (dto.OperarioCtaId.HasValue)
        {
            asignacion = asignaciones.FirstOrDefault(o => o.Id == dto.OperarioCtaId.Value);
            if (asignacion == null)
                return (false, "La asignación indicada no existe para este usuario.", false);
        }
        else if (asignaciones.Count == 1)
        {
            asignacion = asignaciones[0];
        }
        else
        {
            return (false, "Debes indicar OperarioCtaId cuando el usuario tenga múltiples asignaciones.", false);
        }

        if (asignacion.CentroTratamientoId == dto.NuevoCtaId)
            return (true, null, false);

        var ctaDestino = await _ctaRepo.GetByIdAsync(dto.NuevoCtaId);
        if (ctaDestino == null)
            return (false, $"CTA con ID {dto.NuevoCtaId} no encontrado.", false);

        var tareasPendientes = await _asignacionRepo.CountByOperarioAndEstadoAsync(asignacion.Id, EstadoTarea.Pendiente);
        var tareasEnProgreso = await _asignacionRepo.CountByOperarioAndEstadoAsync(asignacion.Id, EstadoTarea.EnProgreso);
        if (tareasPendientes > 0 || tareasEnProgreso > 0)
            return (false, "No se puede mover de CTA mientras tenga tareas pendientes o en progreso.", false);

        // Capture source info before any mutation
        var ctaOrigenId = asignacion.CentroTratamientoId;
        var ctaOrigenCodigo = asignacion.CentroTratamiento?.Codigo ?? "?";
        var sourceOperarioCtaId = asignacion.Id;

        // Check if user already has an active assignment at the destination CTA.
        // (asignaciones only contains active records — see GetAllByIdentityUserIdAsync)
        var asignacionDestino = asignaciones.FirstOrDefault(o => o.CentroTratamientoId == dto.NuevoCtaId);
        if (asignacionDestino != null)
        {
            // Destination already active (e.g. seeder assigns every worker to all CTAs).
            // Deactivate the source record so only the destination remains.
            asignacion.Activo = false;
            await _operarioRepo.UpdateAsync(asignacion);
        }
        else
        {
            // Normal move: reassign source record to the destination CTA.
            asignacion.CentroTratamientoId = dto.NuevoCtaId;
            asignacion.FechaAsignacion = DateTime.UtcNow;
            await _operarioRepo.UpdateAsync(asignacion);
        }

        _logger.LogInformation(
            "Admin movió usuario {IdentityUserId} de CTA {CtaOrigen} a {CtaDestino}",
            identityUserId,
            ctaOrigenCodigo,
            ctaDestino.Codigo);

        // Notify the worker in real time via SignalR
        await _hubContext.Clients.Group($"operario-{sourceOperarioCtaId}")
            .SendAsync("CtaCambiada", new
            {
                operarioCtaId = sourceOperarioCtaId,
                ctaAnteriorId = ctaOrigenId,
                ctaAnteriorCodigo = ctaOrigenCodigo,
                ctaNuevoId = dto.NuevoCtaId,
                ctaNuevoCodigo = ctaDestino.Codigo,
                ctaNuevoNombre = ctaDestino.Nombre,
                mensaje = $"Has sido movido al CTA {ctaDestino.Codigo} ({ctaDestino.Nombre})"
            });

        return (true, null, false);
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
