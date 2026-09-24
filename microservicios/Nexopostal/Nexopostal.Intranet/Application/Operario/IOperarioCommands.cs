using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Hubs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Operario;
/// <summary>Contrato de operaciones que modifican estado para Operario.</summary>
public interface IOperarioCommands
{
    /// <summary>Mueve una asignación de CTA de un usuario (vista admin).</summary>
    Task<(bool Ok, string? Error, bool Conflict)> ActualizarCtaAdmin(string identityUserId, AdminActualizarCtaDto dto);
    /// <summary>Crea un nuevo operario y lo asigna a un CTA</summary>
    Task<OperarioResumenDto> CrearOperario(CrearOperarioDto dto);
    /// <summary>Desactiva un operario</summary>
    Task<bool> DesactivarOperario(int operarioId);
    /// <summary>Reactiva un operario previamente desactivado</summary>
    Task<bool> ReactivarOperario(int operarioId);
}
