using Microsoft.AspNetCore.SignalR;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Hubs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Operario;
/// <summary>Contrato de lectura sin escrituras para Operario.</summary>
public interface IOperarioQueries
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
}
