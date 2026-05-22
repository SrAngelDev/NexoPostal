using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio admin de envíos. Operaciones de gestión (cambio de estado, anular, reabrir)
/// sin tocar ningún flujo de pagos/Stripe.
/// </summary>
public interface IAdminEnviosService
{
    Task<List<AdminEnvioListItemDto>> ListarAsync(
        EstadoEnvio? estado,
        EstadoInterno? estadoInterno,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        string? q,
        string? codigoPostal,
        bool? pagado,
        int limit);

    Task<AdminEnvioDetalleDto?> ObtenerAsync(string numeroSeguimiento);

    Task<(AdminEnvioDetalleDto? envio, string? error)> CambiarEstadoAsync(
        string numeroSeguimiento, CambiarEstadoEnvioDto dto, string? adminUserId);

    Task<(AdminEnvioDetalleDto? envio, string? error)> AnularAsync(
        string numeroSeguimiento, AccionEnvioDto dto, string? adminUserId);

    Task<(AdminEnvioDetalleDto? envio, string? error)> ReabrirAsync(
        string numeroSeguimiento, AccionEnvioDto dto, string? adminUserId);
}

public class AdminEnviosService : IAdminEnviosService
{
    private readonly IEnvioRepository _repo;
    private readonly ITrackingNotificacionService _tracking;
    private readonly ILogger<AdminEnviosService> _logger;

    public AdminEnviosService(
        IEnvioRepository repo,
        ITrackingNotificacionService tracking,
        ILogger<AdminEnviosService> logger)
    {
        _repo = repo;
        _tracking = tracking;
        _logger = logger;
    }

    public async Task<List<AdminEnvioListItemDto>> ListarAsync(
        EstadoEnvio? estado,
        EstadoInterno? estadoInterno,
        DateTime? fechaDesde,
        DateTime? fechaHasta,
        string? q,
        string? codigoPostal,
        bool? pagado,
        int limit)
    {
        var lista = await _repo.GetAdminListAsync(estado, estadoInterno, fechaDesde, fechaHasta, q, codigoPostal, pagado, limit);
        return lista.Select(ToListItem).ToList();
    }

    public async Task<AdminEnvioDetalleDto?> ObtenerAsync(string numeroSeguimiento)
    {
        var e = await _repo.GetByTrackingAsync(numeroSeguimiento);
        return e == null ? null : ToDetalle(e);
    }

    public async Task<(AdminEnvioDetalleDto? envio, string? error)> CambiarEstadoAsync(
        string numeroSeguimiento, CambiarEstadoEnvioDto dto, string? adminUserId)
    {
        var e = await _repo.GetByTrackingAsync(numeroSeguimiento);
        if (e == null) return (null, "Envío no encontrado");

        e.EstadoActual = dto.EstadoPublico;
        e.EstadoInternoActual = dto.EstadoInterno;
        AppendObservacionAdmin(e, adminUserId,
            $"Cambio de estado → {dto.EstadoPublico} / {dto.EstadoInterno}" +
            (string.IsNullOrWhiteSpace(dto.Motivo) ? "" : $" · Motivo: {dto.Motivo.Trim()}"));
        await _repo.UpdateAsync(e);

        await NotificarSeguro(e, dto.Motivo ?? $"Estado actualizado por administración: {dto.EstadoPublico}");
        _logger.LogInformation("Admin {Admin} cambió estado de {Tracking} → {Publico}/{Interno}",
            adminUserId, numeroSeguimiento, dto.EstadoPublico, dto.EstadoInterno);
        return (ToDetalle(e), null);
    }

    public async Task<(AdminEnvioDetalleDto? envio, string? error)> AnularAsync(
        string numeroSeguimiento, AccionEnvioDto dto, string? adminUserId)
    {
        var e = await _repo.GetByTrackingAsync(numeroSeguimiento);
        if (e == null) return (null, "Envío no encontrado");
        if (e.EstadoActual == EstadoEnvio.Entregado)
            return (null, "No se puede anular un envío ya entregado");
        if (e.EstadoActual == EstadoEnvio.Devuelto)
            return (null, "El envío ya está marcado como devuelto");

        e.EstadoActual = EstadoEnvio.Devuelto;
        e.EstadoInternoActual = EstadoInterno.EnDevolucionAlRemitente;
        AppendObservacionAdmin(e, adminUserId,
            $"Envío ANULADO por administración" +
            (string.IsNullOrWhiteSpace(dto.Motivo) ? "" : $" · Motivo: {dto.Motivo.Trim()}"));
        await _repo.UpdateAsync(e);

        await NotificarSeguro(e, dto.Motivo ?? "Envío anulado por administración");
        _logger.LogWarning("Admin {Admin} ANULÓ envío {Tracking}. Motivo: {Motivo}",
            adminUserId, numeroSeguimiento, dto.Motivo ?? "(sin motivo)");
        return (ToDetalle(e), null);
    }

    public async Task<(AdminEnvioDetalleDto? envio, string? error)> ReabrirAsync(
        string numeroSeguimiento, AccionEnvioDto dto, string? adminUserId)
    {
        var e = await _repo.GetByTrackingAsync(numeroSeguimiento);
        if (e == null) return (null, "Envío no encontrado");
        if (e.EstadoActual != EstadoEnvio.Devuelto && e.EstadoActual != EstadoEnvio.Incidencia)
            return (null, "Sólo se pueden reabrir envíos en estado Devuelto o Incidencia");

        e.EstadoActual = EstadoEnvio.Admitido;
        e.EstadoInternoActual = EstadoInterno.PendienteRecogida;
        AppendObservacionAdmin(e, adminUserId,
            $"Envío REABIERTO por administración" +
            (string.IsNullOrWhiteSpace(dto.Motivo) ? "" : $" · Motivo: {dto.Motivo.Trim()}"));
        await _repo.UpdateAsync(e);

        await NotificarSeguro(e, dto.Motivo ?? "Envío reabierto por administración");
        _logger.LogInformation("Admin {Admin} REABRIÓ envío {Tracking}", adminUserId, numeroSeguimiento);
        return (ToDetalle(e), null);
    }

    // ──────────────────────────────────────────────────────────────────────
    private static void AppendObservacionAdmin(Envio e, string? adminUserId, string mensaje)
    {
        var stamp = $"[ADMIN {DateTime.UtcNow:yyyy-MM-dd HH:mm} UID={adminUserId ?? "?"}] {mensaje}";
        e.Observaciones = string.IsNullOrWhiteSpace(e.Observaciones)
            ? stamp
            : $"{e.Observaciones}\n{stamp}";
        // Trim si excede el máximo del modelo.
        if (e.Observaciones.Length > 1000)
            e.Observaciones = e.Observaciones[^1000..];
    }

    private async Task NotificarSeguro(Envio e, string descripcion)
    {
        try
        {
            await _tracking.NotificarCambioEstado(
                e.NumeroSeguimiento,
                e.EstadoActual.ToString(),
                e.EstadoInternoActual.ToString(),
                descripcion);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar notificación SignalR para {Tracking}", e.NumeroSeguimiento);
        }
    }

    private static AdminEnvioListItemDto ToListItem(Envio e) => new()
    {
        NumeroSeguimiento = e.NumeroSeguimiento,
        NumeroExpedicion = e.NumeroExpedicion,
        FechaCreacion = e.FechaCreacion,
        EstadoActual = e.EstadoActual,
        EstadoInternoActual = e.EstadoInternoActual,
        Pagado = e.Pagado,
        Origen = e.Origen,
        Destino = e.Destino,
        CodigoPostalDestino = e.CodigoPostalDestino,
        NombreRemitente = e.NombreRemitente,
        EmailRemitente = e.EmailRemitente,
        NombreDestinatario = e.NombreDestinatario,
        TipoTarifa = e.TipoTarifa,
        CosteCalculado = e.CosteCalculado
    };

    private static AdminEnvioDetalleDto ToDetalle(Envio e) => new()
    {
        NumeroSeguimiento = e.NumeroSeguimiento,
        NumeroExpedicion = e.NumeroExpedicion,
        FechaCreacion = e.FechaCreacion,
        EstadoActual = e.EstadoActual,
        EstadoInternoActual = e.EstadoInternoActual,
        Pagado = e.Pagado,
        Origen = e.Origen,
        Destino = e.Destino,
        CodigoPostalDestino = e.CodigoPostalDestino,
        NombreRemitente = e.NombreRemitente,
        EmailRemitente = e.EmailRemitente,
        NombreDestinatario = e.NombreDestinatario,
        TipoTarifa = e.TipoTarifa,
        CosteCalculado = e.CosteCalculado,
        IdentityUserId = e.IdentityUserId,
        PesoKg = e.PesoKg,
        Dimensiones = e.Dimensiones,
        CodigoPostalOrigen = e.CodigoPostalOrigen,
        TiempoEntregaEstimado = e.TiempoEntregaEstimado,
        ApellidosRemitente = e.ApellidosRemitente,
        TelefonoRemitente = e.TelefonoRemitente,
        DniRemitente = e.DniRemitente,
        ApellidosDestinatario = e.ApellidosDestinatario,
        TelefonoDestinatario = e.TelefonoDestinatario,
        EmailDestinatario = e.EmailDestinatario,
        DniDestinatario = e.DniDestinatario,
        Observaciones = e.Observaciones,
        FechaPago = e.FechaPago
    };
}
