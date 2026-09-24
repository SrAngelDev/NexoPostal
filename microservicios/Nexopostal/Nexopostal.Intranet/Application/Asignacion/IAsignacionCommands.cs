using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Asignacion;
/// <summary>Contrato de operaciones que modifican estado para Asignacion.</summary>
public interface IAsignacionCommands
{
    /// <summary>Crea una asignación de tarea (solo OperarioLogistico)</summary>
    Task<AsignacionDetalleDto> CrearAsignacion(CrearAsignacionDto dto, int operarioLogisticoId, int ctaId);
    /// <summary>
    /// Crea una asignación de tarea para un OperarioOficina (sin requerir CTA).
    /// Usada en alta presencial, salida de oficina origen, entrega CTA→oficina destino y entrega al cliente.
    /// <paramref name = "creadorOperarioCtaId"/> es opcional (null si la crea el sistema).
    /// </summary>
    Task<AsignacionDetalleDto> CrearAsignacionOficina(string numeroExpedicion, int operarioOficinaId, TipoTarea tipoTarea, int? oficinaJsonId, string? oficinaNombre, bool esUrgente = false, int? creadorOperarioCtaId = null, string? observaciones = null);
    /// <summary>Marca una tarea como iniciada (Pendiente → EnProgreso)</summary>
    Task<AsignacionDetalleDto?> IniciarTarea(int asignacionId, int operarioId);
    /// <summary>Marca una tarea como completada (EnProgreso → Completada)</summary>
    Task<AsignacionDetalleDto?> CompletarTarea(int asignacionId, int operarioId);
    /// <summary>Cancela una tarea (cualquier estado → Cancelada)</summary>
    Task<bool> CancelarTarea(int asignacionId, int operarioLogisticoId);
    /// <summary>
    /// Reasigna una tarea (Pendiente o EnProgreso) a otro OperarioCTA del mismo CTA.
    /// Resetea el estado a Pendiente para que el nuevo operario inicie de cero.
    /// </summary>
    Task<AsignacionDetalleDto?> ReasignarTarea(int asignacionId, int nuevoOperarioId, int supervisorOperarioId);
}
