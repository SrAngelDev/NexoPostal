namespace Nexopostal.Intranet.Models;

/// <summary>
/// Las 7 Áreas Zonales en las que NexoPostal divide España
/// para la gestión logística del transporte postal.
/// Cada área agrupa varias provincias y dispone de uno o más CTAs.
/// </summary>
public enum AreaZonal
{
    /// <summary>Galicia, Asturias, León, Zamora</summary>
    Noroeste = 0,

    /// <summary>País Vasco, Cantabria, Navarra, La Rioja, norte de Castilla y León</summary>
    Norte = 1,

    /// <summary>Cataluña, Aragón</summary>
    Noreste = 2,

    /// <summary>Madrid, Castilla-La Mancha, centro de Castilla y León</summary>
    Centro = 3,

    /// <summary>Comunidad Valenciana, Murcia</summary>
    Este = 4,

    /// <summary>Andalucía, Extremadura, Ceuta, Melilla</summary>
    Sur = 5,

    /// <summary>Canarias, Baleares</summary>
    Insular = 6
}

/// <summary>
/// Rol del operario dentro de un Centro de Tratamiento Automatizado (CTA) u oficina postal.
/// </summary>
public enum RolOperario
{
    /// <summary>Operario de oficina: atiende ventanilla, escanea recepciones y salidas a reparto</summary>
    OperarioOficina = 0,

    /// <summary>Operario CTA: trabaja en la nave, consolida paquetes y gestiona movimientos troncales</summary>
    OperarioCTA = 1,

    /// <summary>Supervisor: gestiona incidencias, altas/bajas de personal y revisa métricas. No opera paquetes directamente</summary>
    Supervisor = 2
}

/// <summary>
/// Tipo de tarea asignada a un operario dentro de un CTA.
/// Cada tipo corresponde a una fase del proceso de clasificación.
/// </summary>
public enum TipoTarea
{
    /// <summary>Recibir paquete que llega al CTA (descarga de furgón/camión)</summary>
    Recepcion = 0,

    /// <summary>Clasificar paquete según código postal de destino</summary>
    Clasificacion = 1,

    /// <summary>Cargar paquete clasificado al transporte de salida</summary>
    CargaTransporte = 2,

    /// <summary>Descargar paquetes del transporte que llega de otro CTA</summary>
    DescargaTransporte = 3,

    /// <summary>Registrar la expedición/salida del paquete del CTA</summary>
    Expedicion = 4
}

/// <summary>
/// Estado de una tarea asignada a un operario.
/// </summary>
public enum EstadoTarea
{
    /// <summary>Tarea creada, pendiente de ser iniciada por el operario</summary>
    Pendiente = 0,

    /// <summary>El operario está trabajando en la tarea</summary>
    EnProgreso = 1,

    /// <summary>Tarea finalizada correctamente</summary>
    Completada = 2,

    /// <summary>Tarea cancelada (por incidencia o reasignación)</summary>
    Cancelada = 3
}

/// <summary>
/// Estado de un movimiento de paquete entre CTAs (ruta troncal).
/// </summary>
public enum EstadoMovimiento
{
    /// <summary>Movimiento programado, paquete pendiente de despacho</summary>
    Programado = 0,

    /// <summary>Paquete en ruta hacia el CTA de destino</summary>
    EnTransito = 1,

    /// <summary>Paquete recibido en el CTA de destino</summary>
    Recibido = 2,

    /// <summary>Movimiento cancelado</summary>
    Cancelado = 3
}

/// <summary>
/// Tipo de transporte utilizado en la ruta troncal entre CTAs.
/// </summary>
public enum TipoTransporte
{
    /// <summary>Camiones de gran tonelaje (rutas peninsulares nocturnas)</summary>
    Terrestre = 0,

    /// <summary>Avión (destinos insulares y urgentes de larga distancia)</summary>
    Aereo = 1,

    /// <summary>Barco (Canarias, Baleares, Ceuta, Melilla para paquetes normales)</summary>
    Maritimo = 2
}

/// <summary>
/// Tipo de incidencia que puede ocurrir en un CTA.
/// Gestionadas exclusivamente por el OperarioJefe.
/// </summary>
public enum TipoIncidencia
{
    /// <summary>Paquete con daños físicos durante el transporte/manipulación</summary>
    PaqueteDanado = 0,

    /// <summary>Paquete no localizado en el sistema o físicamente</summary>
    PaqueteExtraviado = 1,

    /// <summary>Dirección de destino incorrecta o incompleta</summary>
    DireccionIncorrecta = 2,

    /// <summary>Paquete retenido por motivos legales o aduaneros</summary>
    PaqueteRetenido = 3,

    /// <summary>Error en la clasificación (enviado a CTA equivocado)</summary>
    ErrorClasificacion = 4,

    /// <summary>Otra incidencia no categorizada</summary>
    Otra = 5
}

/// <summary>
/// Estado del ciclo de vida de una incidencia.
/// </summary>
public enum EstadoIncidencia
{
    /// <summary>Incidencia recién reportada</summary>
    Abierta = 0,

    /// <summary>El OperarioJefe está investigando</summary>
    EnRevision = 1,

    /// <summary>Incidencia resuelta con acción correctiva</summary>
    Resuelta = 2,

    /// <summary>Incidencia cerrada definitivamente</summary>
    Cerrada = 3
}

/// <summary>
/// Tipo de ubicación donde ocurre un evento de trazabilidad.
/// Permite distinguir entre nodos logísticos en el HistorialEstado.
/// </summary>
public enum TipoUbicacion
{
    /// <summary>Oficina Postal (punto de contacto con el ciudadano)</summary>
    Oficina = 0,

    /// <summary>Centro de Tratamiento Automatizado (nodo de clasificación)</summary>
    Cta = 1,

    /// <summary>En ruta de reparto (última milla)</summary>
    EnReparto = 2,

    /// <summary>Domicilio del remitente o destinatario</summary>
    Domicilio = 3,

    /// <summary>Ubicación no especificada o evento del sistema</summary>
    Sistema = 4
}
