namespace Nexopostal.Reparto.DTOs;

// ============================================================
//  DTOs del módulo de Reparto
// ============================================================

// ─── Repartidor ───

public class RepartidorResumenDto
{
    public int Id { get; set; }
    public string IdentityUserId { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public string Rol { get; set; } = "Repartidor";
    public string? Telefono { get; set; }
    public int OficinaJsonId { get; set; }
    public string OficinaNombre { get; set; } = string.Empty;
    public string TipoVehiculo { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int RutasHoy { get; set; }
}

public class CrearRepartidorDto
{
    public string IdentityUserId { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    /// <summary>"JefeReparto" o "Repartidor". Por defecto "Repartidor".</summary>
    public string Rol { get; set; } = "Repartidor";
    public string? Telefono { get; set; }
    public int OficinaJsonId { get; set; }
    public string OficinaNombre { get; set; } = string.Empty;
    public string TipoVehiculo { get; set; } = "Furgoneta";
    public string? MatriculaVehiculo { get; set; }
}

/// <summary>
/// DTO para que un Admin/JefeReparto edite la ficha de un repartidor.
/// Permite ajustar oficina, datos de contacto y vehículo. No cambia el IdentityUserId.
/// </summary>
public class EditarRepartidorDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int OficinaJsonId { get; set; }
    public string OficinaNombre { get; set; } = string.Empty;
    public string TipoVehiculo { get; set; } = "Furgoneta";
    public string? MatriculaVehiculo { get; set; }
}

// ─── Ruta de Reparto ───

public class RutaRepartoResumenDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string FechaReparto { get; set; } = string.Empty;
    public string RepartidorNombre { get; set; } = string.Empty;
    public string OficinaOrigenNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int TotalEntregas { get; set; }
    public int Entregados { get; set; }
    public int Pendientes { get; set; }
    public int Fallidos { get; set; }
}

public class RutaRepartoDetalleDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string FechaReparto { get; set; } = string.Empty;
    public int RepartidorId { get; set; }
    public string RepartidorNombre { get; set; } = string.Empty;
    public int OficinaOrigenJsonId { get; set; }
    public string OficinaOrigenNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime? HoraSalida { get; set; }
    public DateTime? HoraRegreso { get; set; }
    public string? Observaciones { get; set; }
    public List<EntregaPaqueteDto> Entregas { get; set; } = [];
}

public class CrearRutaRepartoDto
{
    public int RepartidorId { get; set; }
    public string FechaReparto { get; set; } = string.Empty;
    public int OficinaOrigenJsonId { get; set; }
    public string OficinaOrigenNombre { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

// ─── Entrega de Paquete ───

public class EntregaPaqueteDto
{
    public int Id { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string DireccionEntrega { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string NombreDestinatario { get; set; } = string.Empty;
    public string? TelefonoDestinatario { get; set; }
    public int NumeroIntento { get; set; }
    public int OrdenEnRuta { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaIntento { get; set; }
    public string? ReceptorNombre { get; set; }
    public string? ReceptorDni { get; set; }
    public string? Observaciones { get; set; }
    public double? LatitudEntrega { get; set; }
    public double? LongitudEntrega { get; set; }
    public string? FirmaDigital { get; set; }
    public string? FotoEntrega { get; set; }
}

public class AgregarEntregaDto
{
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string DireccionEntrega { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string NombreDestinatario { get; set; } = string.Empty;
    public string? TelefonoDestinatario { get; set; }
}

public class RegistrarEntregaDto
{
    public string Estado { get; set; } = string.Empty;
    public string? ReceptorNombre { get; set; }
    public string? ReceptorDni { get; set; }
    public string? Observaciones { get; set; }
    public double? Latitud { get; set; }
    public double? Longitud { get; set; }
    public string? FirmaDigital { get; set; }
    public string? FotoEntrega { get; set; }
}

// ─── Dashboard ───

public class DashboardRepartoDto
{
    public int RutasHoy { get; set; }
    public int RutasEnCurso { get; set; }
    public int EntregasPendientes { get; set; }
    public int EntregasCompletadas { get; set; }
    public int EntregasFallidas { get; set; }
    public int RepartidoresActivos { get; set; }
    public double TasaEntregaExitosa { get; set; }
}

// ─── Orquestación interna (Intranet -> Reparto) ───

public class AutoAsignacionEntregaDesdeAdmisionDto
{
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;
    public string DireccionEntrega { get; set; } = string.Empty;
    public string CiudadDestino { get; set; } = string.Empty;
    public string NombreDestinatario { get; set; } = string.Empty;
    public string? TelefonoDestinatario { get; set; }
    public bool EsUrgente { get; set; }
    public int? OficinaPreferidaJsonId { get; set; }
    public string? OficinaPreferidaNombre { get; set; }
    public string? FechaReparto { get; set; }
    public string? Observaciones { get; set; }
}

public class AutoAsignacionEntregaResultDto
{
    public bool Success { get; set; }
    public bool Idempotente { get; set; }
    public bool CreadaRuta { get; set; }
    public int? RutaId { get; set; }
    public string? RutaCodigo { get; set; }
    public int? RepartidorId { get; set; }
    public string? RepartidorNombre { get; set; }
    public int? EntregaId { get; set; }
    public string Message { get; set; } = string.Empty;
}

// ─── Tracking en tiempo real del JefeReparto ───

/// <summary>
/// Última ubicación conocida de un repartidor para el mapa en tiempo real.
/// </summary>
public class UbicacionActivaDto
{
    public int RepartidorId { get; set; }
    public string NombreRepartidor { get; set; } = string.Empty;
    public string CodigoEmpleado { get; set; } = string.Empty;
    public int OficinaJsonId { get; set; }
    public string OficinaNombre { get; set; } = string.Empty;
    public double Latitud { get; set; }
    public double Longitud { get; set; }
    public DateTime ActualizadoEn { get; set; }
    public int SegundosDesdeActualizacion { get; set; }
    public int? RutaActivaId { get; set; }
    public string? RutaCodigo { get; set; }
    public string? RutaEstado { get; set; }
}

// ─── Asignación manual de paradas pendientes (JefeReparto) ───

/// <summary>
/// Entrega pendiente que el jefe puede reasignar entre repartidores
/// (todas las entregas de rutas en estado Planificada del día).
/// </summary>
public class EntregaPendienteAsignacionDto
{
    public int EntregaId { get; set; }
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string DireccionEntrega { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Ciudad { get; set; } = string.Empty;
    public string NombreDestinatario { get; set; } = string.Empty;
    public int RutaActualId { get; set; }
    public string RutaActualCodigo { get; set; } = string.Empty;
    public int RepartidorActualId { get; set; }
    public string RepartidorActualNombre { get; set; } = string.Empty;
    public int OficinaJsonId { get; set; }
    public string OficinaNombre { get; set; } = string.Empty;
    public string FechaReparto { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

/// <summary>Petición para reasignar una entrega a otra ruta planificada.</summary>
public class ReasignarEntregaDto
{
    public int NuevaRutaId { get; set; }
}
