using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Hubs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.BandejaPendientes;
/// <summary>Contrato de operaciones que modifican estado para BandejaPendientes.</summary>
public interface IBandejaPendientesCommands
{
    /// <summary>
    /// Registra un paquete en la bandeja del CTA. Idempotente por número de expedición:
    /// si ya existía una entrada no asignada, devuelve la existente con Idempotente=true.
    /// </summary>
    Task<RegistrarPaqueteBandejaResponseDto> RegistrarPaqueteAsync(RegistrarPaqueteBandejaRequestDto dto);
    /// <summary>Añade un pendiente a una ruta planificada y crea la EntregaPaquete asociada.</summary>
    Task<(PaqueteBandejaDto? Pendiente, EntregaPaqueteDto? Entrega, string? Error)> AsignarARutaAsync(int pendienteId, AsignarPendienteARutaDto dto, string? asignadoPorIdentityUserId);
}
