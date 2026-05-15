using System.ComponentModel.DataAnnotations;

namespace Nexopostal.Ciudadano.DTOs;

/// <summary>
/// DTO para cotizar un envío (sin necesidad de estar autenticado)
/// </summary>
public class CotizarEnvioDto
{
    [Required]
    [Range(0.1, 30, ErrorMessage = "El peso debe estar entre 0.1 y 30 kg")]
    public decimal Peso { get; set; }

    [MaxLength(50)]
    public string? Dimensiones { get; set; }

    [Required]
    [MaxLength(10)]
    public string CodigoPostalOrigen { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalDestino { get; set; } = string.Empty;
}

/// <summary>
/// Resultado de la cotización
/// </summary>
public class CotizacionResultadoDto
{
    public decimal Precio { get; set; }
    public string Moneda { get; set; } = "EUR";
    public int TiempoEstimadoDias { get; set; }
    public string Observaciones { get; set; } = string.Empty;
}

/// <summary>
/// DTO para crear un nuevo envío (requiere autenticación)
/// </summary>
public class CrearEnvioDto
{
    // Datos del paquete
    [Required]
    [Range(0.1, 30)]
    public decimal Peso { get; set; }

    [Required]
    [MaxLength(50)]
    public string Dimensiones { get; set; } = string.Empty;

    // Datos del remitente
    [Required]
    [MaxLength(200)]
    public string NombreRemitente { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Origen { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalOrigen { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? TelefonoRemitente { get; set; }

    // Datos del destinatario
    [Required]
    [MaxLength(200)]
    public string NombreDestinatario { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Destino { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string CodigoPostalDestino { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? TelefonoDestinatario { get; set; }

    // Observaciones
    [MaxLength(1000)]
    public string? Observaciones { get; set; }
}

/// <summary>
/// DTO de respuesta al crear un envío
/// </summary>
public class EnvioCreadoDto
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public decimal CosteCalculado { get; set; }
    public string EstadoActual { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string UrlEtiqueta { get; set; } = string.Empty;
}

/// <summary>
/// DTO para consultar el tracking público de un envío.
/// NO incluye datos sensibles (origen, destino, peso, observaciones).
/// Solo muestra el estado de progreso simplificado del envío.
/// Se consulta con el NumeroSeguimiento (NX...ES) — el que aparece en el QR de la etiqueta.
/// </summary>
public class EnvioTrackingDto
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string EstadoActual { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public int NumeroBultos { get; set; } = 1;
}

/// <summary>
/// DTO para consultar el tracking INTERNO detallado de un envío.
/// Incluye TODA la información operativa del envío.
/// Se consulta con el NumeroExpedicion (NXI-...) — el código de barras interno de la etiqueta.
/// Solo accesible desde intranet y driver-app (roles: Admin, Operario*, Repartidor*).
/// </summary>
public class EnvioInternoDetalladoDto
{
    // Identificadores
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string NumeroExpedicion { get; set; } = string.Empty;

    // Estado público y estado interno detallado
    public string EstadoPublico { get; set; } = string.Empty;
    public string EstadoInterno { get; set; } = string.Empty;
    public string DescripcionEstadoInterno { get; set; } = string.Empty;

    // Datos del paquete
    public decimal PesoKg { get; set; }
    public string Dimensiones { get; set; } = string.Empty;

    // Datos logísticos
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string CodigoPostalOrigen { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;

    // Datos del remitente
    public string NombreRemitente { get; set; } = string.Empty;
    public string ApellidosRemitente { get; set; } = string.Empty;
    public string TelefonoRemitente { get; set; } = string.Empty;
    public string? EmailRemitente { get; set; }
    public string? DniRemitente { get; set; }

    // Datos del destinatario
    public string NombreDestinatario { get; set; } = string.Empty;
    public string ApellidosDestinatario { get; set; } = string.Empty;
    public string TelefonoDestinatario { get; set; } = string.Empty;
    public string? EmailDestinatario { get; set; }
    public string? DniDestinatario { get; set; }

    // Datos administrativos
    public string TipoTarifa { get; set; } = string.Empty;
    public string TiempoEntregaEstimado { get; set; } = string.Empty;
    public decimal CosteCalculado { get; set; }
    public bool Pagado { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaPago { get; set; }
    public string? Observaciones { get; set; }
}

/// <summary>
/// DTO para que un operario o repartidor actualice el estado interno de un envío.
/// </summary>
public class ActualizarEstadoInternoDto
{
    /// <summary>
    /// Nuevo estado interno (nombre del enum EstadoInterno)
    /// </summary>
    [Required]
    public string NuevoEstadoInterno { get; set; } = string.Empty;

    /// <summary>
    /// Observaciones opcionales del cambio de estado
    /// </summary>
    [MaxLength(500)]
    public string? Observaciones { get; set; }
}

/// <summary>
/// DTO con información resumida del envío para listados
/// </summary>
public class EnvioResumenDto
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string Destino { get; set; } = string.Empty;
    public decimal Precio { get; set; }
    public bool Pagado { get; set; }
    public string TipoTarifa { get; set; } = string.Empty;
}

/// <summary>
/// DTO con información resumida interna del envío para listados en intranet/driver-app
/// </summary>
public class EnvioResumenInternoDto
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string EstadoPublico { get; set; } = string.Empty;
    public string EstadoInterno { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public string Origen { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public string CodigoPostalDestino { get; set; } = string.Empty;
    public decimal PesoKg { get; set; }
    public string TipoTarifa { get; set; } = string.Empty;
    public bool Pagado { get; set; }
}

/// <summary>
/// DTO interno para notificar ubicación del repartidor asociada a un envío.
/// Se usa para emitir eventos SignalR al tracking público.
/// </summary>
public class TrackingUbicacionRepartoDto
{
    [Required]
    [MaxLength(20)]
    public string NumeroSeguimiento { get; set; } = string.Empty;

    [Range(-90, 90)]
    public double Latitud { get; set; }

    [Range(-180, 180)]
    public double Longitud { get; set; }

    [MaxLength(120)]
    public string TipoUbicacion { get; set; } = "RepartidorEnRuta";

    [MaxLength(200)]
    public string? Ubicacion { get; set; }

    [MaxLength(300)]
    public string? Descripcion { get; set; }
}

/// <summary>
/// DTO interno para sincronizar eventos operativos de entrega desde Reparto.
/// Permite unificar estado interno/publico y notificaciones realtime en Ciudadano.
/// </summary>
public class TrackingEventoEntregaDto
{
    [Required]
    [MaxLength(20)]
    public string NumeroSeguimiento { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? NumeroExpedicion { get; set; }

    [Required]
    [MaxLength(40)]
    public string EstadoEntrega { get; set; } = string.Empty;

    [Range(1, 10)]
    public int NumeroIntento { get; set; } = 1;

    [MaxLength(500)]
    public string? Observaciones { get; set; }

    [MaxLength(200)]
    public string? ReceptorNombre { get; set; }

    [MaxLength(15)]
    public string? ReceptorDni { get; set; }

    [Range(-90, 90)]
    public double? Latitud { get; set; }

    [Range(-180, 180)]
    public double? Longitud { get; set; }

    [MaxLength(500)]
    public string? FotoEntrega { get; set; }

    public string? FirmaDigital { get; set; }
}
