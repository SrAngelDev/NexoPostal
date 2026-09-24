using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Movimiento;
/// <summary>Contrato de operaciones que modifican estado para Movimiento.</summary>
public interface IMovimientoCommands
{
    /// <summary>Crea un movimiento entre CTAs</summary>
    Task<MovimientoDetalleDto> CrearMovimiento(CrearMovimientoDto dto);
    /// <summary>Marca un movimiento como despachado (Programado → EnTransito)</summary>
    Task<MovimientoDetalleDto?> DespacharMovimiento(int movimientoId);
    /// <summary>Marca un movimiento como recibido (EnTransito → Recibido)</summary>
    Task<MovimientoDetalleDto?> RecibirMovimiento(int movimientoId);
    /// <summary>Cancela un movimiento</summary>
    Task<bool> CancelarMovimiento(int movimientoId);
}
