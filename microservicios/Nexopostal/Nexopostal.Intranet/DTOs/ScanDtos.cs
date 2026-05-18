namespace Nexopostal.Intranet.DTOs;

// ============================================================
//  DTOs para el procesador de escaneo de códigos de barras
// ============================================================

/// <summary>
/// Modos de escaneo disponibles para operarios en la intranet.
/// Cada modo determina qué acción se ejecuta automáticamente al escanear.
/// </summary>
public static class ModosEscaneo
{
    /// <summary>Paquete recibido en oficina de origen</summary>
    public const string RecepcionOficina = "RecepcionOficina";

    /// <summary>Paquete recibido en CTA (admisión)</summary>
    public const string RecepcionCta = "RecepcionCta";

    /// <summary>Paquete clasificado para expedición</summary>
    public const string Clasificacion = "Clasificacion";

    /// <summary>Paquete despachado en movimiento troncal (CTA → CTA)</summary>
    public const string DespachoTroncal = "DespachoTroncal";

    /// <summary>Paquete recibido tras movimiento troncal</summary>
    public const string RecepcionTroncal = "RecepcionTroncal";

    /// <summary>Paquete entregado a oficina de destino</summary>
    public const string EntregaOficinaDestino = "EntregaOficinaDestino";

    /// <summary>Paquete sale de oficina para reparto a domicilio</summary>
    public const string SalidaAReparto = "SalidaAReparto";

    public static readonly string[] Todos =
    [
        RecepcionOficina,
        RecepcionCta,
        Clasificacion,
        DespachoTroncal,
        RecepcionTroncal,
        EntregaOficinaDestino,
        SalidaAReparto
    ];

    /// <summary>Modos exclusivos de oficina postal (OperarioOficina)</summary>
    public static readonly string[] ModosOficina =
    [
        RecepcionOficina,
        EntregaOficinaDestino,
        SalidaAReparto
    ];

    /// <summary>Modos exclusivos de CTA / nave logística (OperarioCTA)</summary>
    public static readonly string[] ModosCta =
    [
        RecepcionCta,
        Clasificacion,
        DespachoTroncal,
        RecepcionTroncal
    ];

    public static bool EsValido(string modo) => Todos.Contains(modo);
}

/// <summary>
/// Petición de escaneo: el operario escanea un código y el sistema
/// resuelve automáticamente la siguiente acción.
/// </summary>
public class ScanRequestDto
{
    /// <summary>
    /// Código escaneado del paquete (NumeroExpedicion: NXI-XXXXXXXX).
    /// </summary>
    public string CodigoEscaneado { get; set; } = string.Empty;

    /// <summary>
    /// Modo de operación actual (RecepcionCta, Clasificacion, etc.).
    /// </summary>
    public string ModoOperacion { get; set; } = string.Empty;

    /// <summary>
    /// ID del CTA donde se realiza el escaneo (obligatorio para modos CTA).
    /// </summary>
    public int? CtaId { get; set; }

    /// <summary>
    /// Código del CTA (desnormalizado, para historial).
    /// </summary>
    public string? CtaCodigo { get; set; }

    /// <summary>
    /// ID de la oficina JSON donde se realiza el escaneo (para modos oficina).
    /// </summary>
    public int? OficinaJsonId { get; set; }

    /// <summary>
    /// Nombre de la oficina (desnormalizado, para historial).
    /// </summary>
    public string? OficinaNombre { get; set; }

    /// <summary>
    /// CP de destino del paquete (necesario para RecepcionCta y Clasificacion).
    /// </summary>
    public string? CodigoPostalDestino { get; set; }

    /// <summary>
    /// CP de origen del paquete (para resolver ruta).
    /// </summary>
    public string? CodigoPostalOrigen { get; set; }

    /// <summary>
    /// Nombre del operario que escanea.
    /// </summary>
    public string? OperarioNombre { get; set; }

    /// <summary>
    /// Si el envío es urgente.
    /// </summary>
    public bool EsUrgente { get; set; }

    /// <summary>
    /// Observaciones adicionales del operario.
    /// </summary>
    public string? Observaciones { get; set; }
}

/// <summary>
/// Resultado del procesamiento de un escaneo.
/// </summary>
public class ScanResultDto
{
    /// <summary>Si la operación fue exitosa</summary>
    public bool Exito { get; set; }

    /// <summary>Número de expedición procesado</summary>
    public string NumeroExpedicion { get; set; } = string.Empty;

    /// <summary>Modo de operación ejecutado</summary>
    public string ModoOperacion { get; set; } = string.Empty;

    /// <summary>Descripción legible del modo</summary>
    public string ModoDescripcion { get; set; } = string.Empty;

    /// <summary>Estado interno previo del paquete (si se conoce)</summary>
    public string? EstadoAnterior { get; set; }

    /// <summary>Nuevo estado interno asignado</summary>
    public string EstadoNuevo { get; set; } = string.Empty;

    /// <summary>Nombre de la ubicación donde se procesó</summary>
    public string? UbicacionNombre { get; set; }

    /// <summary>Mensaje descriptivo del resultado</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Detalles adicionales de la operación</summary>
    public string? Detalles { get; set; }

    /// <summary>Fecha/hora del procesamiento</summary>
    public DateTime FechaProcesado { get; set; } = DateTime.UtcNow;

    /// <summary>Si se creó un movimiento troncal automáticamente</summary>
    public bool MovimientoTroncalCreado { get; set; }

    /// <summary>Si se envió notificación SignalR</summary>
    public bool NotificacionEnviada { get; set; }
}

/// <summary>
/// DTO para escaneo masivo (batch): múltiples paquetes con el mismo modo.
/// </summary>
public class ScanBatchRequestDto
{
    /// <summary>Lista de códigos escaneados</summary>
    public List<string> CodigosEscaneados { get; set; } = [];

    /// <summary>Modo de operación (mismo para todos)</summary>
    public string ModoOperacion { get; set; } = string.Empty;

    /// <summary>ID del CTA</summary>
    public int? CtaId { get; set; }

    /// <summary>Código del CTA</summary>
    public string? CtaCodigo { get; set; }

    /// <summary>ID de la oficina JSON</summary>
    public int? OficinaJsonId { get; set; }

    /// <summary>Nombre de la oficina</summary>
    public string? OficinaNombre { get; set; }

    /// <summary>Nombre del operario</summary>
    public string? OperarioNombre { get; set; }
}

/// <summary>
/// Resultado del escaneo masivo.
/// </summary>
public class ScanBatchResultDto
{
    public int TotalEscaneados { get; set; }
    public int Exitosos { get; set; }
    public int Fallidos { get; set; }
    public List<ScanResultDto> Resultados { get; set; } = [];
}
