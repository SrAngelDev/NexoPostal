using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Asignacion;
/// <summary>Contrato de lectura sin escrituras para Asignacion.</summary>
public interface IAsignacionQueries
{
    /// <summary>Obtiene las tareas pendientes de un operario (urgentes primero)</summary>
    Task<List<AsignacionResumenDto>> ObtenerTareasPendientes(int operarioId);
    /// <summary>Obtiene las tareas en progreso de un operario</summary>
    Task<List<AsignacionResumenDto>> ObtenerTareasEnProgreso(int operarioId);
    /// <summary>Obtiene las últimas tareas completadas de un operario (más recientes primero).</summary>
    Task<List<AsignacionResumenDto>> ObtenerTareasCompletadas(int operarioId, int max = 50);
    /// <summary>Obtiene todas las asignaciones de un CTA</summary>
    Task<List<AsignacionResumenDto>> ObtenerAsignacionesCta(int ctaId, EstadoTarea? filtroEstado = null);
    /// <summary>Obtiene el detalle de una asignación</summary>
    Task<AsignacionDetalleDto?> ObtenerDetalle(int asignacionId);
    /// <summary>Busca una tarea pendiente o en progreso del operario por número de expedición / seguimiento.</summary>
    Task<AsignacionResumenDto?> BuscarEnMisTareasAsync(int operarioId, string codigo);
    /// <summary>Obtiene las tareas pendientes de un OperarioOficina (urgentes primero).</summary>
    Task<List<AsignacionResumenDto>> ObtenerTareasPendientesOficina(int operarioOficinaId);
    /// <summary>Obtiene las tareas en progreso de un OperarioOficina.</summary>
    Task<List<AsignacionResumenDto>> ObtenerTareasEnProgresoOficina(int operarioOficinaId);
    /// <summary>Tareas completadas recientemente por un OperarioOficina (más recientes primero).</summary>
    Task<List<AsignacionResumenDto>> ObtenerTareasCompletadasOficina(int operarioOficinaId, int max = 50);
    /// <summary>Busca una tarea pendiente o en progreso de un OperarioOficina por número de expedición / seguimiento.</summary>
    Task<AsignacionResumenDto?> BuscarEnMisTareasOficinaAsync(int operarioOficinaId, string codigo);
}
