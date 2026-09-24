using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;
using Nexopostal.Intranet.Services;

namespace Nexopostal.Intranet.Application.Movimiento;
/// <summary>Contrato de lectura sin escrituras para Movimiento.</summary>
public interface IMovimientoQueries
{
    /// <summary>Obtiene los movimientos de un CTA (como origen o destino)</summary>
    Task<List<MovimientoResumenDto>> ObtenerMovimientosCta(int ctaId, EstadoMovimiento? filtroEstado = null);
    /// <summary>Lista global de movimientos (Admin).</summary>
    Task<List<MovimientoResumenDto>> ObtenerMovimientosGlobales(EstadoMovimiento? filtroEstado = null, int? ctaOrigenId = null, int? ctaDestinoId = null);
    /// <summary>Obtiene el detalle de un movimiento</summary>
    Task<MovimientoDetalleDto?> ObtenerDetalle(int movimientoId);
    /// <summary>Obtiene el historial de movimientos de un paquete</summary>
    Task<List<MovimientoResumenDto>> ObtenerHistorialPaquete(string numeroExpedicion);
}
