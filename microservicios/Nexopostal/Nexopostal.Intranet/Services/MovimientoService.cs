using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio para gestionar los movimientos de paquetes entre CTAs (rutas troncales).
/// Los camiones viajan de noche entre áreas zonales.
/// Los paquetes urgentes tienen espacio asegurado en el primer transporte.
/// </summary>
public interface IMovimientoService
{
    /// <summary>Crea un movimiento entre CTAs</summary>
    Task<MovimientoDetalleDto> CrearMovimiento(CrearMovimientoDto dto);

    /// <summary>Marca un movimiento como despachado (Programado → EnTransito)</summary>
    Task<MovimientoDetalleDto?> DespacharMovimiento(int movimientoId);

    /// <summary>Marca un movimiento como recibido (EnTransito → Recibido)</summary>
    Task<MovimientoDetalleDto?> RecibirMovimiento(int movimientoId);

    /// <summary>Cancela un movimiento</summary>
    Task<bool> CancelarMovimiento(int movimientoId);

    /// <summary>Obtiene los movimientos de un CTA (como origen o destino)</summary>
    Task<List<MovimientoResumenDto>> ObtenerMovimientosCta(int ctaId, EstadoMovimiento? filtroEstado = null);

    /// <summary>Lista global de movimientos (Admin).</summary>
    Task<List<MovimientoResumenDto>> ObtenerMovimientosGlobales(EstadoMovimiento? filtroEstado = null, int? ctaOrigenId = null, int? ctaDestinoId = null);

    /// <summary>Obtiene el detalle de un movimiento</summary>
    Task<MovimientoDetalleDto?> ObtenerDetalle(int movimientoId);

    /// <summary>Obtiene el historial de movimientos de un paquete</summary>
    Task<List<MovimientoResumenDto>> ObtenerHistorialPaquete(string numeroExpedicion);
}

public class MovimientoService : IMovimientoService
{
    private readonly IMovimientoPaqueteRepository _movimientoRepo;
    private readonly ICentroTratamientoRepository _ctaRepo;
    private readonly IClasificacionService _clasificacionService;
    private readonly INotificacionService _notificacionService;
    private readonly ILogger<MovimientoService> _logger;

    public MovimientoService(
        IMovimientoPaqueteRepository movimientoRepo,
        ICentroTratamientoRepository ctaRepo,
        IClasificacionService clasificacionService,
        INotificacionService notificacionService,
        ILogger<MovimientoService> logger)
    {
        _movimientoRepo = movimientoRepo;
        _ctaRepo = ctaRepo;
        _clasificacionService = clasificacionService;
        _notificacionService = notificacionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<MovimientoDetalleDto> CrearMovimiento(CrearMovimientoDto dto)
    {
        // Verificar CTAs
        var ctaOrigen = await _ctaRepo.GetByIdAsync(dto.CtaOrigenId)
            ?? throw new ArgumentException("CTA de origen no encontrado");
        var ctaDestino = await _ctaRepo.GetByIdAsync(dto.CtaDestinoId)
            ?? throw new ArgumentException("CTA de destino no encontrado");

        if (dto.CtaOrigenId == dto.CtaDestinoId)
            throw new InvalidOperationException("El CTA de origen y destino no pueden ser el mismo");

        // Determinar tipo de transporte automáticamente si no se especifica
        TipoTransporte tipoTransporte;
        if (!string.IsNullOrEmpty(dto.TipoTransporte) && Enum.TryParse<TipoTransporte>(dto.TipoTransporte, true, out var tp))
        {
            tipoTransporte = tp;
        }
        else
        {
            tipoTransporte = await _clasificacionService.DeterminarTipoTransporte(
                dto.CtaOrigenId, dto.CtaDestinoId, dto.EsUrgente);
        }

        var movimiento = new MovimientoPaquete
        {
            NumeroExpedicion = dto.NumeroExpedicion,
            CtaOrigenId = dto.CtaOrigenId,
            CtaDestinoId = dto.CtaDestinoId,
            TipoTransporte = tipoTransporte,
            EsUrgente = dto.EsUrgente,
            Observaciones = dto.Observaciones
        };

        await _movimientoRepo.CreateAsync(movimiento);

        _logger.LogInformation(
            "Movimiento creado: {Expedicion} de {Origen} a {Destino} vía {Transporte} (urgente: {Urgente})",
            dto.NumeroExpedicion, ctaOrigen.Codigo, ctaDestino.Codigo, tipoTransporte, dto.EsUrgente);

        return await ObtenerDetalle(movimiento.Id) ?? throw new Exception("Error al obtener detalle");
    }

    /// <inheritdoc />
    public async Task<MovimientoDetalleDto?> DespacharMovimiento(int movimientoId)
    {
        var movimiento = await _movimientoRepo.GetByIdAsync(movimientoId);
        if (movimiento == null) return null;

        if (movimiento.Estado != EstadoMovimiento.Programado)
            throw new InvalidOperationException($"Solo se pueden despachar movimientos programados. Estado actual: {movimiento.Estado}");

        movimiento.Estado = EstadoMovimiento.EnTransito;
        movimiento.FechaSalida = DateTime.UtcNow;
        await _movimientoRepo.UpdateAsync(movimiento);

        _logger.LogInformation("Movimiento {Id} despachado (en tránsito)", movimientoId);

        // 📡 Notificar a logísticos del CTA destino que el paquete está en camino
        var ctaDestino = await _ctaRepo.GetByIdAsync(movimiento.CtaDestinoId);
        var ctaOrigen = await _ctaRepo.GetByIdAsync(movimiento.CtaOrigenId);
        if (ctaDestino != null && ctaOrigen != null)
        {
            await _notificacionService.NotificarMovimientoDespachado(
                movimiento.CtaDestinoId,
                ctaDestino.Codigo,
                ctaOrigen.Codigo,
                movimiento.NumeroExpedicion,
                movimiento.TipoTransporte.ToString(),
                movimiento.EsUrgente);
        }

        return await ObtenerDetalle(movimientoId);
    }

    /// <inheritdoc />
    public async Task<MovimientoDetalleDto?> RecibirMovimiento(int movimientoId)
    {
        var movimiento = await _movimientoRepo.GetByIdAsync(movimientoId);
        if (movimiento == null) return null;

        if (movimiento.Estado != EstadoMovimiento.EnTransito)
            throw new InvalidOperationException($"Solo se pueden recibir movimientos en tránsito. Estado actual: {movimiento.Estado}");

        movimiento.Estado = EstadoMovimiento.Recibido;
        movimiento.FechaLlegada = DateTime.UtcNow;
        await _movimientoRepo.UpdateAsync(movimiento);

        _logger.LogInformation("Movimiento {Id} recibido en CTA destino", movimientoId);

        // 📡 Notificar a logísticos del CTA destino que el paquete ha llegado
        var ctaDest = await _ctaRepo.GetByIdAsync(movimiento.CtaDestinoId);
        var ctaOrig = await _ctaRepo.GetByIdAsync(movimiento.CtaOrigenId);
        if (ctaDest != null && ctaOrig != null)
        {
            await _notificacionService.NotificarMovimientoRecibido(
                movimiento.CtaDestinoId,
                ctaDest.Codigo,
                ctaOrig.Codigo,
                movimiento.NumeroExpedicion,
                movimiento.EsUrgente);
        }

        return await ObtenerDetalle(movimientoId);
    }

    /// <inheritdoc />
    public async Task<bool> CancelarMovimiento(int movimientoId)
    {
        var movimiento = await _movimientoRepo.GetByIdAsync(movimientoId);
        if (movimiento == null) return false;

        if (movimiento.Estado == EstadoMovimiento.Recibido)
            throw new InvalidOperationException("No se puede cancelar un movimiento ya recibido");

        movimiento.Estado = EstadoMovimiento.Cancelado;
        await _movimientoRepo.UpdateAsync(movimiento);

        _logger.LogInformation("Movimiento {Id} cancelado", movimientoId);
        return true;
    }

    /// <inheritdoc />
    public async Task<List<MovimientoResumenDto>> ObtenerMovimientosCta(int ctaId, EstadoMovimiento? filtroEstado = null)
    {
        var movimientos = await _movimientoRepo.GetByCtaAsync(ctaId, filtroEstado);
        return movimientos.Select(m => new MovimientoResumenDto
        {
            Id = m.Id,
            NumeroExpedicion = m.NumeroExpedicion,
            CtaOrigenCodigo = m.CtaOrigen.Codigo,
            CtaDestinoCodigo = m.CtaDestino.Codigo,
            Estado = m.Estado.ToString(),
            TipoTransporte = m.TipoTransporte.ToString(),
            EsUrgente = m.EsUrgente,
            FechaCreacion = m.FechaCreacion,
            FechaSalida = m.FechaSalida,
            FechaLlegada = m.FechaLlegada
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<List<MovimientoResumenDto>> ObtenerMovimientosGlobales(EstadoMovimiento? filtroEstado = null, int? ctaOrigenId = null, int? ctaDestinoId = null)
    {
        var movimientos = await _movimientoRepo.GetAllAsync(filtroEstado, ctaOrigenId, ctaDestinoId);
        return movimientos.Select(m => new MovimientoResumenDto
        {
            Id = m.Id,
            NumeroExpedicion = m.NumeroExpedicion,
            CtaOrigenCodigo = m.CtaOrigen.Codigo,
            CtaDestinoCodigo = m.CtaDestino.Codigo,
            Estado = m.Estado.ToString(),
            TipoTransporte = m.TipoTransporte.ToString(),
            EsUrgente = m.EsUrgente,
            FechaCreacion = m.FechaCreacion,
            FechaSalida = m.FechaSalida,
            FechaLlegada = m.FechaLlegada
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<MovimientoDetalleDto?> ObtenerDetalle(int movimientoId)
    {
        var m = await _movimientoRepo.GetDetailAsync(movimientoId);
        if (m == null) return null;

        return new MovimientoDetalleDto
        {
            Id = m.Id,
            NumeroExpedicion = m.NumeroExpedicion,
            CtaOrigenId = m.CtaOrigenId,
            CtaOrigenCodigo = m.CtaOrigen.Codigo,
            CtaOrigenNombre = m.CtaOrigen.Nombre,
            CtaOrigenArea = m.CtaOrigen.Area.ToString(),
            CtaDestinoId = m.CtaDestinoId,
            CtaDestinoCodigo = m.CtaDestino.Codigo,
            CtaDestinoNombre = m.CtaDestino.Nombre,
            CtaDestinoArea = m.CtaDestino.Area.ToString(),
            Estado = m.Estado.ToString(),
            TipoTransporte = m.TipoTransporte.ToString(),
            EsUrgente = m.EsUrgente,
            Observaciones = m.Observaciones,
            FechaCreacion = m.FechaCreacion,
            FechaSalida = m.FechaSalida,
            FechaLlegada = m.FechaLlegada
        };
    }

    /// <inheritdoc />
    public async Task<List<MovimientoResumenDto>> ObtenerHistorialPaquete(string numeroExpedicion)
    {
        var movimientos = await _movimientoRepo.GetByExpedicionAsync(numeroExpedicion);
        return movimientos.Select(m => new MovimientoResumenDto
        {
            Id = m.Id,
            NumeroExpedicion = m.NumeroExpedicion,
            CtaOrigenCodigo = m.CtaOrigen.Codigo,
            CtaDestinoCodigo = m.CtaDestino.Codigo,
            Estado = m.Estado.ToString(),
            TipoTransporte = m.TipoTransporte.ToString(),
            EsUrgente = m.EsUrgente,
            FechaCreacion = m.FechaCreacion,
            FechaSalida = m.FechaSalida,
            FechaLlegada = m.FechaLlegada
        }).ToList();
    }
}
