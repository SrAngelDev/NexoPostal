using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Nexopostal.Reparto.Data;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Hubs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.BandejaPendientes;
/// <summary>Contrato de lectura sin escrituras para BandejaPendientes.</summary>
public interface IBandejaPendientesQueries
{
    /// <summary>Lista los pendientes de un CTA. Por defecto solo los no asignados.</summary>
    Task<List<PaqueteBandejaDto>> ListarPendientesAsync(int? ctaId, bool incluirAsignados = false);
}
