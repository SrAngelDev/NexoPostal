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

        // Cargar todas las asignaciones (activas e inactivas) — necesario para reactivar
        // una asignación previa al CTA destino si ya existía.
        var todasLasAsignaciones = await _operarioRepo.GetAllByIdentityUserIdIncludingInactiveAsync(identityUserId);

        // Verificar que el CTA destino existe
        var ctaDestino = await _ctaRepo.GetByIdAsync(dto.NuevoCtaId);
        if (ctaDestino == null)
            return (false, $"CTA con ID {dto.NuevoCtaId} no encontrado.", false);

        // ─── Caso especial: PRIMERA ASIGNACIÓN ───
        // El usuario aún no tiene ninguna asignación CTA. Para crearla necesitamos
        // los datos de identidad operativa que el front nos envía desde el detalle de usuario.
        if (todasLasAsignaciones.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(dto.NombreCompleto)
                || string.IsNullOrWhiteSpace(dto.CodigoEmpleado)
                || string.IsNullOrWhiteSpace(dto.Rol))
            {
                return (false,
                    "El usuario no tiene asignación previa. Para crear la primera se requieren NombreCompleto, CodigoEmpleado y Rol.",
                    false);
            }

            if (!Enum.TryParse<RolOperario>(dto.Rol, true, out var rolOp))
                return (false, $"Rol operativo no válido: {dto.Rol}.", false);

            var nuevaPrimera = new OperarioCta
            {
                IdentityUserId = identityUserId,
                NombreCompleto = dto.NombreCompleto!,
                CodigoEmpleado = dto.CodigoEmpleado!,
                Rol = rolOp,
                CentroTratamientoId = dto.NuevoCtaId,
                FechaAsignacion = DateTime.UtcNow,
                Activo = true
            };
            await _operarioRepo.CreateAsync(nuevaPrimera);

            _logger.LogInformation(
                "Admin asignó por primera vez al usuario {IdentityUserId} al CTA {CtaDestino} (rol {Rol})",
                identityUserId,
                ctaDestino.Codigo,
                rolOp);

            return (true, null, false);
        }

        var activas = todasLasAsignaciones.Where(a => a.Activo).ToList();

        // Caso idempotente: ya tiene EXACTAMENTE una asignación activa y es la del destino.
        if (activas.Count == 1 && activas[0].CentroTratamientoId == dto.NuevoCtaId)
            return (true, null, false);

        // Bloqueo: cualquier asignación activa con tareas pendientes/en progreso impide el cambio.
        foreach (var activa in activas)
        {
            var pendientes = await _asignacionRepo.CountByOperarioAndEstadoAsync(activa.Id, EstadoTarea.Pendiente);
            var enProgreso = await _asignacionRepo.CountByOperarioAndEstadoAsync(activa.Id, EstadoTarea.EnProgreso);
            if (pendientes > 0 || enProgreso > 0)
                return (false, $"No se puede mover: la asignación al CTA {activa.CentroTratamiento?.Codigo ?? "?"} tiene tareas pendientes/en progreso.", true);
        }

        // Capturar info origen (la primera activa, para log)
        var origen = activas.FirstOrDefault();
        var ctaOrigenCodigo = origen?.CentroTratamiento?.Codigo ?? "?";
        var ctaOrigenId = origen?.CentroTratamientoId ?? 0;

        // 1) Desactivar TODAS las asignaciones activas que no sean el destino
        foreach (var activa in activas.Where(a => a.CentroTratamientoId != dto.NuevoCtaId))
        {
            activa.Activo = false;
            await _operarioRepo.UpdateAsync(activa);
        }

        // 2) Obtener o crear la asignación al CTA destino
        var asignacionDestino = todasLasAsignaciones.FirstOrDefault(a => a.CentroTratamientoId == dto.NuevoCtaId);
        int destinoId;
        if (asignacionDestino != null)
        {
            // Existía (activa o inactiva): reactivarla y actualizar fecha
            asignacionDestino.Activo = true;
            asignacionDestino.FechaAsignacion = DateTime.UtcNow;
            await _operarioRepo.UpdateAsync(asignacionDestino);
            destinoId = asignacionDestino.Id;
        }
        else
        {
            // No existía: crear una nueva tomando datos del operario (de la primera asignación previa)
            var plantilla = todasLasAsignaciones.First();
            var nueva = new OperarioCta
            {
                IdentityUserId = identityUserId,
                NombreCompleto = plantilla.NombreCompleto,
                CodigoEmpleado = plantilla.CodigoEmpleado,
                Rol = plantilla.Rol,
                CentroTratamientoId = dto.NuevoCtaId,
                FechaAsignacion = DateTime.UtcNow,
                Activo = true
            };
            var creada = await _operarioRepo.CreateAsync(nueva);
            destinoId = creada.Id;
        }

        _logger.LogInformation(
            "Admin movió usuario {IdentityUserId} a CTA único {CtaDestino} (origen previa: {CtaOrigen})",
            identityUserId,
            ctaDestino.Codigo,
            ctaOrigenCodigo);

        // Notificar en tiempo real (si el operario tenía sesión en alguna asignación origen)
        if (origen != null)
        {
            await _hubContext.Clients.Group($"operario-{origen.Id}")
                .SendAsync("CtaCambiada", new
                {
                    operarioCtaId = destinoId,
                    ctaAnteriorId = ctaOrigenId,
                    ctaAnteriorCodigo = ctaOrigenCodigo,
                    ctaNuevoId = dto.NuevoCtaId,
                    ctaNuevoCodigo = ctaDestino.Codigo,
                    ctaNuevoNombre = ctaDestino.Nombre,
                    mensaje = $"Has sido reasignado al CTA {ctaDestino.Codigo} ({ctaDestino.Nombre})"
                });
        }

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
