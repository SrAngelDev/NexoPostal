using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.OficinaPostal;
/// <summary>Contrato de lectura sin escrituras para OficinaPostal.</summary>
public interface IOficinaPostalQueries
{
    /// <summary>
    /// Resuelve la oficina más cercana + CTA para un código postal.
    /// Este es el método clave del flujo logístico automático:
    ///   CP 28919 → Oficina "NexoPostal Leganés" + CTA-MAD
    /// </summary>
    Task<ResolverOficinaCtaResponseDto?> ResolverOficinaPorCp(string codigoPostal);
    /// <summary>Obtiene los operarios asignados a una oficina</summary>
    Task<List<OperarioOficinaResumenDto>> ObtenerOperariosOficina(int oficinaJsonId);
    /// <summary>Obtiene las oficinas cuyo prefijo de CP coincide con alguna ruta del CTA dado.</summary>
    Task<List<OficinaJsonDto>> ObtenerOficinasPorCta(int ctaId);
    /// <summary>Obtiene la oficina asignada activa al operario autenticado.</summary>
    Task<MiOficinaInfoDto?> ObtenerMiOficina(string identityUserId);
    /// <summary>Obtiene la asignación de oficina (activa o no) de un usuario, vista admin.</summary>
    Task<MiOficinaInfoDto?> ObtenerOficinaAdmin(string identityUserId);
}
