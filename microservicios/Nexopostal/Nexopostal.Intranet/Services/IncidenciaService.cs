using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio para gestionar las incidencias de un CTA.
/// Solo el Supervisor puede crear, actualizar y resolver incidencias.
/// </summary>
public interface IIncidenciaService
{
    /// <summary>Crea una nueva incidencia</summary>
    Task<IncidenciaDetalleDto> CrearIncidencia(CrearIncidenciaDto dto, int operarioJefeId, int ctaId);

    /// <summary>Actualiza el estado de una incidencia</summary>
    Task<IncidenciaDetalleDto?> ActualizarIncidencia(int incidenciaId, ActualizarIncidenciaDto dto);

    /// <summary>Obtiene las incidencias de un CTA</summary>
    Task<List<IncidenciaResumenDto>> ObtenerIncidenciasCta(int ctaId, EstadoIncidencia? filtroEstado = null);

    /// <summary>Obtiene el detalle de una incidencia</summary>
    Task<IncidenciaDetalleDto?> ObtenerDetalle(int incidenciaId);

    /// <summary>Obtiene las incidencias de un paquete específico</summary>
    Task<List<IncidenciaResumenDto>> ObtenerIncidenciasPaquete(string numeroExpedicion);
}

public class IncidenciaService : IIncidenciaService
{
    private readonly IIncidenciaRepository _incidenciaRepo;
    private readonly IOperarioCtaRepository _operarioRepo;
    private readonly ICentroTratamientoRepository _ctaRepo;
    private readonly INotificacionService _notificacionService;
    private readonly ILogger<IncidenciaService> _logger;

    public IncidenciaService(
        IIncidenciaRepository incidenciaRepo,
        IOperarioCtaRepository operarioRepo,
        ICentroTratamientoRepository ctaRepo,
        INotificacionService notificacionService,
        ILogger<IncidenciaService> logger)
    {
        _incidenciaRepo = incidenciaRepo;
        _operarioRepo = operarioRepo;
        _ctaRepo = ctaRepo;
        _notificacionService = notificacionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IncidenciaDetalleDto> CrearIncidencia(CrearIncidenciaDto dto, int operarioJefeId, int ctaId)
    {
        if (!Enum.TryParse<TipoIncidencia>(dto.Tipo, true, out var tipo))
            throw new ArgumentException($"Tipo de incidencia no válido: {dto.Tipo}");

        var incidencia = new Incidencia
        {
            NumeroExpedicion = dto.NumeroExpedicion,
            CtaId = ctaId,
            ReportadaPorId = operarioJefeId,
            Tipo = tipo,
            Descripcion = dto.Descripcion
        };

        await _incidenciaRepo.CreateAsync(incidencia);

        _logger.LogInformation("Incidencia {Tipo} creada para paquete {Expedicion} en CTA {Cta}",
            tipo, dto.NumeroExpedicion, ctaId);

        // 📡 Notificar a todo el CTA de la nueva incidencia
        var jefe = await _operarioRepo.GetWithCtaAsync(operarioJefeId)
            ?? throw new InvalidOperationException("Supervisor no encontrado");
        await _notificacionService.NotificarIncidenciaCreada(
            ctaId,
            jefe.CentroTratamiento.Codigo,
            dto.NumeroExpedicion,
            tipo.ToString(),
            jefe.NombreCompleto);

        return await ObtenerDetalle(incidencia.Id) ?? throw new Exception("Error al obtener detalle");
    }

    /// <inheritdoc />
    public async Task<IncidenciaDetalleDto?> ActualizarIncidencia(int incidenciaId, ActualizarIncidenciaDto dto)
    {
        var incidencia = await _incidenciaRepo.GetByIdAsync(incidenciaId);
        if (incidencia == null) return null;

        if (!Enum.TryParse<EstadoIncidencia>(dto.Estado, true, out var estado))
            throw new ArgumentException($"Estado no válido: {dto.Estado}");

        // Si se resuelve, exigir resolución
        if (estado == EstadoIncidencia.Resuelta && string.IsNullOrWhiteSpace(dto.Resolucion))
            throw new InvalidOperationException("Se requiere una descripción de la resolución");

        incidencia.Estado = estado;

        if (!string.IsNullOrWhiteSpace(dto.Resolucion))
            incidencia.Resolucion = dto.Resolucion;

        if (estado == EstadoIncidencia.Resuelta || estado == EstadoIncidencia.Cerrada)
            incidencia.FechaResolucion = DateTime.UtcNow;

        await _incidenciaRepo.UpdateAsync(incidencia);

        _logger.LogInformation("Incidencia {Id} actualizada a estado {Estado}", incidenciaId, estado);

        // 📡 Notificar a todo el CTA del cambio de estado
        var cta = await _ctaRepo.GetByIdAsync(incidencia.CtaId);
        if (cta != null)
        {
            await _notificacionService.NotificarIncidenciaActualizada(
                incidencia.CtaId,
                cta.Codigo,
                incidencia.NumeroExpedicion,
                incidencia.Tipo.ToString(),
                estado.ToString(),
                dto.Resolucion);
        }

        return await ObtenerDetalle(incidenciaId);
    }

    /// <inheritdoc />
    public async Task<List<IncidenciaResumenDto>> ObtenerIncidenciasCta(int ctaId, EstadoIncidencia? filtroEstado = null)
    {
        var incidencias = await _incidenciaRepo.GetByCtaAsync(ctaId, filtroEstado);
        return incidencias.Select(i => new IncidenciaResumenDto
        {
            Id = i.Id,
            NumeroExpedicion = i.NumeroExpedicion,
            Tipo = i.Tipo.ToString(),
            Estado = i.Estado.ToString(),
            ReportadaPor = i.ReportadaPor.NombreCompleto,
            FechaCreacion = i.FechaCreacion,
            FechaResolucion = i.FechaResolucion
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IncidenciaDetalleDto?> ObtenerDetalle(int incidenciaId)
    {
        var i = await _incidenciaRepo.GetDetailAsync(incidenciaId);
        if (i == null) return null;

        return new IncidenciaDetalleDto
        {
            Id = i.Id,
            NumeroExpedicion = i.NumeroExpedicion,
            Tipo = i.Tipo.ToString(),
            Estado = i.Estado.ToString(),
            Descripcion = i.Descripcion,
            Resolucion = i.Resolucion,
            CtaId = i.CtaId,
            CtaCodigo = i.Cta.Codigo,
            CtaNombre = i.Cta.Nombre,
            ReportadaPorId = i.ReportadaPorId,
            ReportadaPorNombre = i.ReportadaPor.NombreCompleto,
            ReportadaPorCodigo = i.ReportadaPor.CodigoEmpleado,
            FechaCreacion = i.FechaCreacion,
            FechaResolucion = i.FechaResolucion
        };
    }

    /// <inheritdoc />
    public async Task<List<IncidenciaResumenDto>> ObtenerIncidenciasPaquete(string numeroExpedicion)
    {
        var incidencias = await _incidenciaRepo.GetByExpedicionAsync(numeroExpedicion);
        return incidencias.Select(i => new IncidenciaResumenDto
        {
            Id = i.Id,
            NumeroExpedicion = i.NumeroExpedicion,
            Tipo = i.Tipo.ToString(),
            Estado = i.Estado.ToString(),
            ReportadaPor = i.ReportadaPor.NombreCompleto,
            FechaCreacion = i.FechaCreacion,
            FechaResolucion = i.FechaResolucion
        }).ToList();
    }
}
