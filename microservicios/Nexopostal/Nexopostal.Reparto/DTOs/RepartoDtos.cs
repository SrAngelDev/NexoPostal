namespace Nexopostal.Reparto.DTOs;

// DTOs que cubren la operativa diaria de reparto, rutas y entregas.

/// <summary>Resumen de un repartidor para listados de administración y supervisión.</summary>
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
    public string? MatriculaVehiculo { get; set; }
    public bool Activo { get; set; }
    public int RutasHoy { get; set; }
}

/// <summary>Datos necesarios para dar de alta a un repartidor en una oficina concreta.</summary>
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
/// Cuando se envía <see cref="VehiculoId"/> el servicio sincroniza automáticamente
/// TipoVehiculo y MatriculaVehiculo desde la entidad Vehiculo, por lo que no es
/// necesario enviarlos explícitamente.
/// </summary>
public class EditarRepartidorDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public int OficinaJsonId { get; set; }
    public string OficinaNombre { get; set; } = string.Empty;
    /// <summary>
    /// Tipo de vehículo embebido. Opcional cuando se proporciona <see cref="VehiculoId"/>
    /// (el servicio lo toma del vehículo de flota). Si se omite sin VehiculoId se mantiene
    /// el valor actual del repartidor.
    /// </summary>
    public string? TipoVehiculo { get; set; }
    public string? MatriculaVehiculo { get; set; }
    /// <summary>Id del vehículo de la flota a asignar. Null = dejar como está. 0 = desasignar.</summary>
    public int? VehiculoId { get; set; }
}

/// <summary>Resumen de una ruta de reparto para listados del día.</summary>
public class RutaRepartoResumenDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string FechaReparto { get; set; } = string.Empty;
    public string RepartidorNombre { get; set; } = string.Empty;
    public int OficinaOrigenJsonId { get; set; }
    public string OficinaOrigenNombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int TotalEntregas { get; set; }
    public int Entregados { get; set; }
    public int Pendientes { get; set; }
    public int Fallidos { get; set; }
}

/// <summary>Vista completa de una ruta con sus entregas y tiempos clave.</summary>
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

/// <summary>Petición para planificar una nueva ruta sobre una oficina y un repartidor.</summary>
public class CrearRutaRepartoDto
{
    public int RepartidorId { get; set; }
    public string FechaReparto { get; set; } = string.Empty;
    public int OficinaOrigenJsonId { get; set; }
    public string OficinaOrigenNombre { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
}

/// <summary>Detalle de una parada o entrega incluida dentro de una ruta.</summary>
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

/// <summary>Datos mínimos para añadir un paquete a una ruta ya planificada.</summary>
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

/// <summary>Resultado operativo de un intento de entrega realizado por el repartidor.</summary>
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

/// <summary>KPIs de reparto para el panel de control de última milla.</summary>
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

/// <summary>
/// Payload interno que usa Intranet para pedir a Reparto la autoasignación inicial de una entrega.
/// </summary>
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

/// <summary>Resultado de la autoasignación disparada desde admisión.</summary>
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
