using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Hubs;
using Nexopostal.Reparto.Models;

namespace Nexopostal.Reparto.Services;

/// <summary>
/// Servicio que gestiona la bandeja del JefeReparto: paquetes que el CTA destino
/// ha marcado como "DisponibleParaReparto" y que están a la espera de ser
/// añadidos a una ruta concreta por el JefeReparto.
/// </summary>
public interface IBandejaPendientesService
{
    /// <summary>
    /// Registra un paquete en la bandeja del CTA. Idempotente por número de expedición:
    /// si ya existía una entrada no asignada, devuelve la existente con Idempotente=true.
    /// </summary>
    Task<RegistrarPaqueteBandejaResponseDto> RegistrarPaqueteAsync(RegistrarPaqueteBandejaRequestDto dto);

    /// <summary>Lista los pendientes de un CTA. Por defecto solo los no asignados.</summary>
    Task<List<PaqueteBandejaDto>> ListarPendientesAsync(int? ctaId, bool incluirAsignados = false);

    /// <summary>Añade un pendiente a una ruta planificada y crea la EntregaPaquete asociada.</summary>
    Task<(PaqueteBandejaDto? Pendiente, EntregaPaqueteDto? Entrega, string? Error)> AsignarARutaAsync(
        int pendienteId,
        AsignarPendienteARutaDto dto,
        string? asignadoPorIdentityUserId);
}

public class BandejaPendientesService : IBandejaPendientesService
{
    private readonly RepartoDbContext _db;
    private readonly IRepartoService _repartoService;
    private readonly IHubContext<RepartoHub> _hub;
    private readonly ILogger<BandejaPendientesService> _logger;

    public BandejaPendientesService(
        RepartoDbContext db,
        IRepartoService repartoService,
        IHubContext<RepartoHub> hub,
        ILogger<BandejaPendientesService> logger)
    {
        _db = db;
        _repartoService = repartoService;
        _hub = hub;
        _logger = logger;
    }

    public async Task<RegistrarPaqueteBandejaResponseDto> RegistrarPaqueteAsync(RegistrarPaqueteBandejaRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.NumeroExpedicion))
        {
            return new RegistrarPaqueteBandejaResponseDto
            {
                Success = false,
                Message = "NumeroExpedicion es obligatorio."
            };
        }

        var expedicion = dto.NumeroExpedicion.Trim().ToUpperInvariant();

        var existente = await _db.PaquetesPendientesReparto
            .FirstOrDefaultAsync(p => p.NumeroExpedicion == expedicion);

        if (existente is not null)
        {
            // Idempotencia: si aún no se asignó, refrescamos algunos campos y devolvemos.
            if (existente.AsignadoARutaId is null)
            {
                existente.CtaId = dto.CtaId;
                existente.CtaCodigo = dto.CtaCodigo ?? existente.CtaCodigo;
                existente.EsUrgente = dto.EsUrgente || existente.EsUrgente;
                existente.NombreDestinatario = string.IsNullOrWhiteSpace(dto.NombreDestinatario)
                    ? existente.NombreDestinatario
                    : dto.NombreDestinatario!;
                existente.TelefonoDestinatario = dto.TelefonoDestinatario ?? existente.TelefonoDestinatario;
                existente.DireccionEntrega = string.IsNullOrWhiteSpace(dto.DireccionEntrega)
                    ? existente.DireccionEntrega
                    : dto.DireccionEntrega!;
                existente.CodigoPostalDestino = string.IsNullOrWhiteSpace(dto.CodigoPostalDestino)
                    ? existente.CodigoPostalDestino
                    : dto.CodigoPostalDestino!;
                existente.CiudadDestino = dto.CiudadDestino ?? existente.CiudadDestino;
                existente.Observaciones = dto.Observaciones ?? existente.Observaciones;
                await _db.SaveChangesAsync();
            }

            return new RegistrarPaqueteBandejaResponseDto
            {
                Success = true,
                Idempotente = true,
                Id = existente.Id,
                Message = existente.AsignadoARutaId is null
                    ? "Paquete ya estaba en bandeja; datos actualizados."
                    : "Paquete ya estaba en una ruta; no se modifica."
            };
        }

        var pendiente = new PaquetePendienteReparto
        {
            NumeroExpedicion = expedicion,
            NumeroSeguimiento = string.IsNullOrWhiteSpace(dto.NumeroSeguimiento)
                ? expedicion
                : dto.NumeroSeguimiento!.Trim().ToUpperInvariant(),
            CtaId = dto.CtaId,
            CtaCodigo = dto.CtaCodigo ?? string.Empty,
            NombreDestinatario = dto.NombreDestinatario ?? "Destinatario no informado",
            TelefonoDestinatario = dto.TelefonoDestinatario,
            DireccionEntrega = dto.DireccionEntrega ?? string.Empty,
            CodigoPostalDestino = dto.CodigoPostalDestino ?? string.Empty,
            CiudadDestino = dto.CiudadDestino,
            EsUrgente = dto.EsUrgente,
            Observaciones = dto.Observaciones,
            FechaRegistro = DateTime.UtcNow
        };

        _db.PaquetesPendientesReparto.Add(pendiente);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Paquete {Expedicion} añadido a bandeja del CTA {Cta} ({CtaCodigo}).",
            pendiente.NumeroExpedicion, pendiente.CtaId, pendiente.CtaCodigo);

        // Notificar en tiempo real a los JefeReparto conectados.
        try
        {
            await _hub.Clients.Group("jefes-reparto").SendAsync("PaqueteEnBandeja", new
            {
                ctaId = pendiente.CtaId,
                ctaCodigo = pendiente.CtaCodigo,
                numeroExpedicion = pendiente.NumeroExpedicion,
                esUrgente = pendiente.EsUrgente
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo enviar notificación SignalR PaqueteEnBandeja para {Expedicion}",
                pendiente.NumeroExpedicion);
        }

        return new RegistrarPaqueteBandejaResponseDto
        {
            Success = true,
            Idempotente = false,
            Id = pendiente.Id,
            Message = "Paquete registrado en bandeja del JefeReparto."
        };
    }

    public async Task<List<PaqueteBandejaDto>> ListarPendientesAsync(int? ctaId, bool incluirAsignados = false)
    {
        var query = _db.PaquetesPendientesReparto.AsNoTracking().AsQueryable();

        if (ctaId.HasValue)
            query = query.Where(p => p.CtaId == ctaId.Value);

        if (!incluirAsignados)
            query = query.Where(p => p.AsignadoARutaId == null);

        var items = await query
            .OrderByDescending(p => p.EsUrgente)
            .ThenBy(p => p.FechaRegistro)
            .ToListAsync();

        return items.Select(MapearBandeja).ToList();
    }

    public async Task<(PaqueteBandejaDto? Pendiente, EntregaPaqueteDto? Entrega, string? Error)> AsignarARutaAsync(
        int pendienteId,
        AsignarPendienteARutaDto dto,
        string? asignadoPorIdentityUserId)
    {
        var pendiente = await _db.PaquetesPendientesReparto.FirstOrDefaultAsync(p => p.Id == pendienteId);
        if (pendiente is null)
            return (null, null, "No existe el paquete pendiente.");

        if (pendiente.AsignadoARutaId.HasValue)
            return (null, null, "El paquete ya está asignado a una ruta.");

        var entregaDto = await _repartoService.AgregarEntregaARuta(dto.RutaRepartoId, new AgregarEntregaDto
        {
            NumeroExpedicion = pendiente.NumeroExpedicion,
            NumeroSeguimiento = pendiente.NumeroSeguimiento,
            DireccionEntrega = pendiente.DireccionEntrega,
            CodigoPostal = pendiente.CodigoPostalDestino,
            Ciudad = pendiente.CiudadDestino ?? string.Empty,
            NombreDestinatario = pendiente.NombreDestinatario,
            TelefonoDestinatario = pendiente.TelefonoDestinatario
        });

        if (entregaDto is null)
            return (null, null, "No se pudo crear la entrega en la ruta destino (revisa que esté en estado Planificada).");

        pendiente.AsignadoARutaId = dto.RutaRepartoId;
        pendiente.EntregaPaqueteId = entregaDto.Id;
        pendiente.FechaAsignacion = DateTime.UtcNow;
        pendiente.AsignadoPorIdentityUserId = asignadoPorIdentityUserId;

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Pendiente {Id} ({Expedicion}) asignado a ruta {RutaId} como entrega {EntregaId}.",
            pendiente.Id, pendiente.NumeroExpedicion, dto.RutaRepartoId, entregaDto.Id);

        return (MapearBandeja(pendiente), entregaDto, null);
    }

    private static PaqueteBandejaDto MapearBandeja(PaquetePendienteReparto p) => new()
    {
        Id = p.Id,
        NumeroExpedicion = p.NumeroExpedicion,
        NumeroSeguimiento = p.NumeroSeguimiento,
        CtaId = p.CtaId,
        CtaCodigo = p.CtaCodigo,
        NombreDestinatario = p.NombreDestinatario,
        TelefonoDestinatario = p.TelefonoDestinatario,
        DireccionEntrega = p.DireccionEntrega,
        CodigoPostalDestino = p.CodigoPostalDestino,
        CiudadDestino = p.CiudadDestino,
        EsUrgente = p.EsUrgente,
        Observaciones = p.Observaciones,
        FechaRegistro = p.FechaRegistro,
        AsignadoARutaId = p.AsignadoARutaId,
        EntregaPaqueteId = p.EntregaPaqueteId,
        FechaAsignacion = p.FechaAsignacion
    };
}
