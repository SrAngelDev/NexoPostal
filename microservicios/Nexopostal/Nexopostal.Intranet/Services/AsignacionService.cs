using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio para gestionar la asignación de paquetes a operarios.
/// El OperarioLogistico crea asignaciones → el Operario las ejecuta.
/// Los paquetes urgentes tienen prioridad (pase VIP).
/// </summary>
public interface IAsignacionService
{
    /// <summary>Crea una asignación de tarea (solo OperarioLogistico)</summary>
    Task<AsignacionDetalleDto> CrearAsignacion(CrearAsignacionDto dto, int operarioLogisticoId, int ctaId);

    /// <summary>Obtiene las tareas pendientes de un operario (urgentes primero)</summary>
    Task<List<AsignacionResumenDto>> ObtenerTareasPendientes(int operarioId);

    /// <summary>Obtiene las tareas en progreso de un operario</summary>
    Task<List<AsignacionResumenDto>> ObtenerTareasEnProgreso(int operarioId);

    /// <summary>Marca una tarea como iniciada (Pendiente → EnProgreso)</summary>
    Task<AsignacionDetalleDto?> IniciarTarea(int asignacionId, int operarioId);

    /// <summary>Marca una tarea como completada (EnProgreso → Completada)</summary>
    Task<AsignacionDetalleDto?> CompletarTarea(int asignacionId, int operarioId);

    /// <summary>Cancela una tarea (cualquier estado → Cancelada)</summary>
    Task<bool> CancelarTarea(int asignacionId, int operarioLogisticoId);

    /// <summary>Obtiene todas las asignaciones de un CTA</summary>
    Task<List<AsignacionResumenDto>> ObtenerAsignacionesCta(int ctaId, EstadoTarea? filtroEstado = null);

    /// <summary>Obtiene el detalle de una asignación</summary>
    Task<AsignacionDetalleDto?> ObtenerDetalle(int asignacionId);
}

public class AsignacionService : IAsignacionService
{
    private readonly IAsignacionPaqueteRepository _asignacionRepo;
    private readonly IOperarioCtaRepository _operarioRepo;
    private readonly INotificacionService _notificacionService;
    private readonly ILogger<AsignacionService> _logger;

    public AsignacionService(
        IAsignacionPaqueteRepository asignacionRepo,
        IOperarioCtaRepository operarioRepo,
        INotificacionService notificacionService,
        ILogger<AsignacionService> logger)
    {
        _asignacionRepo = asignacionRepo;
        _operarioRepo = operarioRepo;
        _notificacionService = notificacionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AsignacionDetalleDto> CrearAsignacion(CrearAsignacionDto dto, int operarioLogisticoId, int ctaId)
    {
        if (!Enum.TryParse<TipoTarea>(dto.TipoTarea, true, out var tipoTarea))
            throw new ArgumentException($"Tipo de tarea no válido: {dto.TipoTarea}");

        // Verificar que el operario asignado existe y pertenece al mismo CTA
        var operarioAsignado = await _operarioRepo.GetByIdAsync(dto.OperarioAsignadoId)
            ?? throw new ArgumentException("Operario asignado no encontrado");

        if (operarioAsignado.CentroTratamientoId != ctaId)
            throw new InvalidOperationException("El operario no pertenece a este CTA");

        if (operarioAsignado.Rol != RolOperario.OperarioOficina)
            throw new InvalidOperationException("Solo se pueden asignar tareas a operarios de oficina");

        var asignacion = new AsignacionPaquete
        {
            NumeroExpedicion = dto.NumeroExpedicion,
            OperarioAsignadoId = dto.OperarioAsignadoId,
            AsignadoPorId = operarioLogisticoId,
            CtaId = ctaId,
            TipoTarea = tipoTarea,
            EsUrgente = dto.EsUrgente,
            Observaciones = dto.Observaciones
        };

        await _asignacionRepo.CreateAsync(asignacion);

        _logger.LogInformation("Tarea {Tipo} asignada a {Operario} para paquete {Expedicion} (urgente: {Urgente})",
            tipoTarea, operarioAsignado.CodigoEmpleado, dto.NumeroExpedicion, dto.EsUrgente);

        // Cargar el nombre del logístico que asigna
        var logistico = await _operarioRepo.GetWithCtaAsync(operarioLogisticoId)
            ?? throw new InvalidOperationException("Operario logístico no encontrado");

        // 📡 Notificar al operario asignado vía SignalR
        await _notificacionService.NotificarTareaAsignada(
            dto.OperarioAsignadoId,
            ctaId,
            logistico.CentroTratamiento.Codigo,
            dto.NumeroExpedicion,
            tipoTarea.ToString(),
            dto.EsUrgente,
            logistico.NombreCompleto);

        return await ObtenerDetalle(asignacion.Id) ?? throw new Exception("Error al obtener detalle de asignación");
    }

    /// <inheritdoc />
    public async Task<List<AsignacionResumenDto>> ObtenerTareasPendientes(int operarioId)
    {
        var asignaciones = await _asignacionRepo.GetByOperarioAsync(operarioId, EstadoTarea.Pendiente);
        return asignaciones.Select(a => MapToResumen(a)).ToList();
    }

    /// <inheritdoc />
    public async Task<List<AsignacionResumenDto>> ObtenerTareasEnProgreso(int operarioId)
    {
        var asignaciones = await _asignacionRepo.GetByOperarioAsync(operarioId, EstadoTarea.EnProgreso);
        return asignaciones.Select(a => MapToResumen(a)).ToList();
    }

    /// <inheritdoc />
    public async Task<AsignacionDetalleDto?> IniciarTarea(int asignacionId, int operarioId)
    {
        var asignacion = await _asignacionRepo.GetByIdAsync(asignacionId);
        if (asignacion == null) return null;

        if (asignacion.OperarioAsignadoId != operarioId)
            throw new InvalidOperationException("Esta tarea no está asignada a ti");

        if (asignacion.EstadoTarea != EstadoTarea.Pendiente)
            throw new InvalidOperationException($"Solo se pueden iniciar tareas pendientes. Estado actual: {asignacion.EstadoTarea}");

        asignacion.EstadoTarea = EstadoTarea.EnProgreso;
        asignacion.FechaInicio = DateTime.UtcNow;
        await _asignacionRepo.UpdateAsync(asignacion);

        _logger.LogInformation("Tarea {Id} iniciada por operario {OperarioId}", asignacionId, operarioId);

        // 📡 Notificar a logísticos que el operario ha empezado
        var operarioInfo = await _operarioRepo.GetWithCtaAsync(operarioId)
            ?? throw new InvalidOperationException("Operario no encontrado");
        await _notificacionService.NotificarTareaIniciada(
            operarioInfo.CentroTratamientoId,
            operarioInfo.CentroTratamiento.Codigo,
            asignacion.NumeroExpedicion,
            asignacion.TipoTarea.ToString(),
            operarioInfo.NombreCompleto);

        return await ObtenerDetalle(asignacionId);
    }

    /// <inheritdoc />
    public async Task<AsignacionDetalleDto?> CompletarTarea(int asignacionId, int operarioId)
    {
        var asignacion = await _asignacionRepo.GetByIdAsync(asignacionId);
        if (asignacion == null) return null;

        if (asignacion.OperarioAsignadoId != operarioId)
            throw new InvalidOperationException("Esta tarea no está asignada a ti");

        if (asignacion.EstadoTarea != EstadoTarea.EnProgreso)
            throw new InvalidOperationException($"Solo se pueden completar tareas en progreso. Estado actual: {asignacion.EstadoTarea}");

        asignacion.EstadoTarea = EstadoTarea.Completada;
        asignacion.FechaCompletada = DateTime.UtcNow;
        await _asignacionRepo.UpdateAsync(asignacion);

        _logger.LogInformation("Tarea {Id} completada por operario {OperarioId}", asignacionId, operarioId);

        // 📡 Notificar a logísticos que la tarea se ha completado
        var opInfo = await _operarioRepo.GetWithCtaAsync(operarioId)
            ?? throw new InvalidOperationException("Operario no encontrado");
        await _notificacionService.NotificarTareaCompletada(
            opInfo.CentroTratamientoId,
            opInfo.CentroTratamiento.Codigo,
            asignacion.NumeroExpedicion,
            asignacion.TipoTarea.ToString(),
            opInfo.NombreCompleto);

        return await ObtenerDetalle(asignacionId);
    }

    /// <inheritdoc />
    public async Task<bool> CancelarTarea(int asignacionId, int operarioLogisticoId)
    {
        var asignacion = await _asignacionRepo.GetByIdAsync(asignacionId);
        if (asignacion == null) return false;

        if (asignacion.EstadoTarea == EstadoTarea.Completada)
            throw new InvalidOperationException("No se puede cancelar una tarea completada");

        asignacion.EstadoTarea = EstadoTarea.Cancelada;
        await _asignacionRepo.UpdateAsync(asignacion);

        _logger.LogInformation("Tarea {Id} cancelada por logístico {LogisticoId}", asignacionId, operarioLogisticoId);

        // 📡 Notificar a todo el CTA que se ha cancelado la tarea
        var logInfo = await _operarioRepo.GetWithCtaAsync(operarioLogisticoId)
            ?? throw new InvalidOperationException("Operario logístico no encontrado");
        await _notificacionService.NotificarTareaCancelada(
            logInfo.CentroTratamientoId,
            logInfo.CentroTratamiento.Codigo,
            asignacion.NumeroExpedicion,
            asignacion.TipoTarea.ToString(),
            logInfo.NombreCompleto);

        return true;
    }

    /// <inheritdoc />
    public async Task<List<AsignacionResumenDto>> ObtenerAsignacionesCta(int ctaId, EstadoTarea? filtroEstado = null)
    {
        var asignaciones = await _asignacionRepo.GetByCtaAsync(ctaId, filtroEstado);
        return asignaciones.Select(a => MapToResumen(a)).ToList();
    }

    /// <inheritdoc />
    public async Task<AsignacionDetalleDto?> ObtenerDetalle(int asignacionId)
    {
        var a = await _asignacionRepo.GetDetailAsync(asignacionId);
        if (a == null) return null;

        return new AsignacionDetalleDto
        {
            Id = a.Id,
            NumeroExpedicion = a.NumeroExpedicion,
            TipoTarea = a.TipoTarea.ToString(),
            EstadoTarea = a.EstadoTarea.ToString(),
            EsUrgente = a.EsUrgente,
            Observaciones = a.Observaciones,
            OperarioAsignadoId = a.OperarioAsignadoId,
            OperarioAsignadoNombre = a.OperarioAsignado.NombreCompleto,
            OperarioAsignadoCodigo = a.OperarioAsignado.CodigoEmpleado,
            AsignadoPorId = a.AsignadoPorId,
            AsignadoPorNombre = a.AsignadoPor.NombreCompleto,
            CtaId = a.CtaId,
            CtaCodigo = a.Cta.Codigo,
            FechaAsignacion = a.FechaAsignacion,
            FechaInicio = a.FechaInicio,
            FechaCompletada = a.FechaCompletada
        };
    }

    private static AsignacionResumenDto MapToResumen(AsignacionPaquete a) => new()
    {
        Id = a.Id,
        NumeroExpedicion = a.NumeroExpedicion,
        TipoTarea = a.TipoTarea.ToString(),
        EstadoTarea = a.EstadoTarea.ToString(),
        EsUrgente = a.EsUrgente,
        OperarioAsignado = a.OperarioAsignado.NombreCompleto,
        AsignadoPor = a.AsignadoPor.NombreCompleto,
        FechaAsignacion = a.FechaAsignacion,
        FechaCompletada = a.FechaCompletada
    };
}
