using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.Reparto;
/// <summary>Contrato de lectura sin escrituras para Reparto.</summary>
public interface IRepartoQueries
{
    // ─── Repartidores ───
    Task<List<RepartidorResumenDto>> ObtenerRepartidores(int? oficinaJsonId = null, bool incluirInactivos = false);
    Task<RepartidorResumenDto?> ObtenerRepartidorPorIdentityId(string identityUserId);
    // ─── Rutas ───
    Task<List<RutaRepartoResumenDto>> ObtenerRutas(DateOnly? fecha = null, int? repartidorId = null, int? oficinaJsonId = null);
    Task<RutaRepartoDetalleDto?> ObtenerRutaPorId(int id);
    Task<RutaRepartoDetalleDto?> ObtenerRutaPorCodigo(string codigo);
    Task<List<EntregaPaqueteDto>> ObtenerEntregasPorRuta(int rutaId);
    Task<List<EntregaPaqueteDto>> ObtenerEntregasPorSeguimiento(string numeroSeguimiento);
    // ─── Dashboard ───
    Task<DashboardRepartoDto> ObtenerDashboard(int? oficinaJsonId = null);
    Task<List<UbicacionActivaDto>> ObtenerUbicacionesActivas(int? oficinaJsonId = null, int ventanaMinutos = 10);
    // ─── Asignación manual de paradas (JefeReparto) ───
    Task<List<EntregaPendienteAsignacionDto>> ObtenerEntregasPendientesAsignacion(int? oficinaJsonId = null);
}
