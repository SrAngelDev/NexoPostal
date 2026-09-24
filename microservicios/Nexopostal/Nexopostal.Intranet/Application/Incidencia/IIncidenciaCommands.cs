using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Incidencia;
/// <summary>Contrato de operaciones que modifican estado para Incidencia.</summary>
public interface IIncidenciaCommands
{
    /// <summary>Crea una nueva incidencia</summary>
    Task<IncidenciaDetalleDto> CrearIncidencia(CrearIncidenciaDto dto, int operarioJefeId, int ctaId);
    /// <summary>Actualiza el estado de una incidencia</summary>
    Task<IncidenciaDetalleDto?> ActualizarIncidencia(int incidenciaId, ActualizarIncidenciaDto dto);
}
