using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio para gestionar el historial de estados (trazabilidad) de los paquetes.
/// Registra cada cambio de estado/ubicación y proporciona consultas para
/// tracking público (clientes) y auditoría interna (operarios).
/// </summary>
public interface IHistorialService
{
    /// <summary>
    /// Registra un nuevo evento de trazabilidad en el historial.
    /// Se llama cada vez que un paquete cambia de estado o ubicación.
    /// </summary>
    Task<HistorialEventoInternoDto> RegistrarEvento(CrearHistorialEventoDto dto);

    /// <summary>
    /// Obtiene el historial completo de un paquete por número de expedición (vista interna).
    /// Incluye todos los eventos, visibles y no visibles para el cliente.
    /// </summary>
    Task<List<HistorialEventoInternoDto>> ObtenerHistorialInterno(string numeroExpedicion);

    /// <summary>
    /// Obtiene el historial público de un paquete por número de seguimiento.
    /// Solo incluye eventos marcados como visibles para el cliente.
    /// </summary>
    Task<List<HistorialEventoDto>> ObtenerHistorialPublico(string numeroSeguimiento);

    /// <summary>
    /// Obtiene el último evento registrado de un paquete.
    /// </summary>
    Task<HistorialEventoInternoDto?> ObtenerUltimoEvento(string numeroExpedicion);
}

public class HistorialService : IHistorialService
{
    private readonly IHistorialEstadoRepository _historialRepo;
    private readonly ILogger<HistorialService> _logger;

    public HistorialService(IHistorialEstadoRepository historialRepo, ILogger<HistorialService> logger)
    {
        _historialRepo = historialRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HistorialEventoInternoDto> RegistrarEvento(CrearHistorialEventoDto dto)
    {
        // Si no se proporcionó estado previo, obtener el último
        if (string.IsNullOrEmpty(dto.EstadoPrevio))
        {
            var ultimo = await _historialRepo.GetUltimoEventoAsync(dto.NumeroExpedicion);
            dto.EstadoPrevio = ultimo?.Estado;
        }

        // Parsear el tipo de ubicación
        if (!Enum.TryParse<TipoUbicacion>(dto.TipoUbicacion, true, out var tipoUbicacion))
        {
            tipoUbicacion = TipoUbicacion.Sistema;
        }

        var historial = new HistorialEstado
        {
            NumeroExpedicion = dto.NumeroExpedicion,
            NumeroSeguimiento = dto.NumeroSeguimiento,
            Estado = dto.Estado,
            EstadoPrevio = dto.EstadoPrevio,
            TipoUbicacion = tipoUbicacion,
            UbicacionId = dto.UbicacionId,
            UbicacionNombre = dto.UbicacionNombre,
            UbicacionCodigo = dto.UbicacionCodigo,
            OperarioId = dto.OperarioId,
            OperarioNombre = dto.OperarioNombre,
            Descripcion = dto.Descripcion,
            Observaciones = dto.Observaciones,
            VisibleParaCliente = dto.VisibleParaCliente,
            FechaEvento = DateTime.UtcNow
        };

        await _historialRepo.CreateAsync(historial);

        _logger.LogInformation(
            "📋 Historial → {Expedicion}: {EstadoPrevio} → {Estado} en {Ubicacion} ({TipoUbicacion})",
            dto.NumeroExpedicion, dto.EstadoPrevio ?? "N/A", dto.Estado,
            dto.UbicacionNombre ?? "Sistema", dto.TipoUbicacion);

        return MapToInternoDto(historial);
    }

    /// <inheritdoc />
    public async Task<List<HistorialEventoInternoDto>> ObtenerHistorialInterno(string numeroExpedicion)
    {
        var historial = await _historialRepo.GetByExpedicionAsync(numeroExpedicion);
        return historial.Select(MapToInternoDto).ToList();
    }

    /// <inheritdoc />
    public async Task<List<HistorialEventoDto>> ObtenerHistorialPublico(string numeroSeguimiento)
    {
        var historial = await _historialRepo.GetPublicoByTrackingAsync(numeroSeguimiento);

        return historial.Select(h => new HistorialEventoDto
        {
            Estado = h.Estado,
            Descripcion = h.Descripcion,
            Ubicacion = h.UbicacionNombre,
            UbicacionCodigo = h.UbicacionCodigo,
            TipoUbicacion = h.TipoUbicacion.ToString(),
            FechaEvento = h.FechaEvento
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<HistorialEventoInternoDto?> ObtenerUltimoEvento(string numeroExpedicion)
    {
        var ultimo = await _historialRepo.GetUltimoEventoAsync(numeroExpedicion);
        return ultimo != null ? MapToInternoDto(ultimo) : null;
    }

    private static HistorialEventoInternoDto MapToInternoDto(HistorialEstado h) => new()
    {
        Id = h.Id,
        NumeroExpedicion = h.NumeroExpedicion,
        NumeroSeguimiento = h.NumeroSeguimiento,
        Estado = h.Estado,
        EstadoPrevio = h.EstadoPrevio,
        TipoUbicacion = h.TipoUbicacion.ToString(),
        UbicacionId = h.UbicacionId,
        UbicacionNombre = h.UbicacionNombre,
        UbicacionCodigo = h.UbicacionCodigo,
        OperarioId = h.OperarioId,
        OperarioNombre = h.OperarioNombre,
        Descripcion = h.Descripcion,
        Observaciones = h.Observaciones,
        VisibleParaCliente = h.VisibleParaCliente,
        FechaEvento = h.FechaEvento
    };
}
