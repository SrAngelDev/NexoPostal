using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Clasificacion;
/// <summary>Contrato de lectura sin escrituras para Clasificacion.</summary>
public interface IClasificacionQueries
{
    /// <summary>Resuelve el CTA de destino para un código postal dado</summary>
    Task<ResolverCtaResponseDto?> ResolverCtaDestino(string codigoPostal);
    /// <summary>Determina el tipo de transporte óptimo entre dos CTAs</summary>
    Task<TipoTransporte> DeterminarTipoTransporte(int ctaOrigenId, int ctaDestinoId, bool esUrgente);
    /// <summary>Obtiene todos los CTAs</summary>
    Task<List<CtaResumenDto>> ObtenerTodosCtas();
    /// <summary>Obtiene el detalle de un CTA con sus operarios y rutas</summary>
    Task<CtaDetalleDto?> ObtenerCtaDetalle(int ctaId);
    /// <summary>Obtiene el dashboard de estadísticas de un CTA</summary>
    Task<DashboardCtaDto?> ObtenerDashboardCta(int ctaId);
    /// <summary>Obtiene el dashboard global de administración agregando todos los CTAs</summary>
    Task<DashboardAdminDto> ObtenerDashboardAdmin();
}
