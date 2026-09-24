using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;
using Nexopostal.Reparto.Services;

namespace Nexopostal.Reparto.Application.Reparto;
/// <summary>Contrato de operaciones que modifican estado para Reparto.</summary>
public interface IRepartoCommands
{
    Task<RepartidorResumenDto> CrearRepartidor(CrearRepartidorDto dto);
    Task<(RepartidorResumenDto? Repartidor, string? Error)> EditarRepartidor(int id, EditarRepartidorDto dto);
    Task<(bool Ok, string? Error)> DesactivarRepartidor(int id);
    Task<(bool Ok, string? Error)> ReactivarRepartidor(int id);
    Task<RutaRepartoDetalleDto> CrearRuta(CrearRutaRepartoDto dto);
    Task<RutaRepartoDetalleDto?> IniciarRuta(int rutaId);
    Task<RutaRepartoDetalleDto?> FinalizarRuta(int rutaId, string? observaciones = null);
    Task<(bool Ok, string? Error)> CancelarRuta(int rutaId);
    Task<(bool Ok, string? Error)> ReactivarRuta(int rutaId);
    // ─── Entregas ───
    Task<EntregaPaqueteDto?> AgregarEntregaARuta(int rutaId, AgregarEntregaDto dto);
    Task<EntregaPaqueteDto?> RegistrarEntrega(int entregaId, RegistrarEntregaDto dto);
    Task<AutoAsignacionEntregaResultDto> AutoAsignarEntregaDesdeAdmision(AutoAsignacionEntregaDesdeAdmisionDto dto);
    // ─── Tracking en tiempo real (JefeReparto) ───
    Task RegistrarUbicacionRepartidor(string identityUserId, double latitud, double longitud, int? rutaActivaId);
    Task<EntregaPaqueteDto?> ReasignarEntregaARuta(int entregaId, int nuevaRutaId);
}
