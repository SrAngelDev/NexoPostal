using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Incidencia;
/// <summary>Contrato de lectura sin escrituras para Incidencia.</summary>
public interface IIncidenciaQueries
{
    /// <summary>Obtiene las incidencias de un CTA</summary>
    Task<List<IncidenciaResumenDto>> ObtenerIncidenciasCta(int ctaId, EstadoIncidencia? filtroEstado = null);
    /// <summary>Obtiene incidencias globales (Admin)</summary>
    Task<List<IncidenciaResumenDto>> ObtenerIncidenciasGlobales(EstadoIncidencia? filtroEstado = null, int? ctaId = null, TipoIncidencia? tipo = null);
    /// <summary>Obtiene el detalle de una incidencia</summary>
    Task<IncidenciaDetalleDto?> ObtenerDetalle(int incidenciaId);
    /// <summary>Obtiene las incidencias de un paquete específico</summary>
    Task<List<IncidenciaResumenDto>> ObtenerIncidenciasPaquete(string numeroExpedicion);
}
