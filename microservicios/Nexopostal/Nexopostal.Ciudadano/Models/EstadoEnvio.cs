namespace Nexopostal.Ciudadano.Models;

/// <summary>
/// Estado público del envío — visible para el cliente en la web.
/// Información simplificada consultable con el número de seguimiento (NX...ES).
/// </summary>
public enum EstadoEnvio
{
    PendientePago = -1, // Envío creado pero pendiente de pago
    Admitido = 0,       // Envío pagado, etiqueta generada
    EnTransito = 1,     // El paquete está en movimiento
    EnOficina = 2,      // El paquete llegó a una oficina
    EnReparto = 3,      // El conductor lo tiene para entrega
    Entregado = 4,      // Entregado al destinatario
    Incidencia = 5,     // Problema con el envío
    Devuelto = 6        // Devuelto al remitente
}

/// <summary>
/// Estado interno detallado del envío — visible solo en la intranet y driver-app.
/// Información operativa consultable con el número de expedición interno (NXI-...).
/// Los operarios y repartidores trabajan sobre este estado para la gestión completa.
/// </summary>
public enum EstadoInterno
{
    // --- Fase de admisión ---
    PendientePago = -1,           // Pendiente de confirmación de pago
    PendienteRecogida = 0,        // Pagado, esperando recogida en origen
    RecogidoEnOrigen = 1,         // Recogido por el repartidor en dirección de origen

    // --- Fase de clasificación origen ---
    RecibidoEnCentroOrigen = 10,  // Llegó al centro de clasificación de origen
    EnClasificacionOrigen = 11,   // Siendo clasificado en centro de origen
    ClasificadoParaExpedicion = 12, // Clasificado y listo para expedición

    // --- Fase de tránsito ---
    EnTransitoHaciaCentroDestino = 20, // En ruta hacia centro de destino
    EnTransitoIntermedio = 21,         // En tránsito por centro intermedio

    // --- Fase de clasificación destino ---
    RecibidoEnCentroDestino = 30, // Llegó al centro de clasificación de destino
    EnClasificacionDestino = 31,  // Siendo clasificado en centro de destino
    AsignadoARuta = 32,           // Asignado a un repartidor y ruta de entrega

    // --- Fase de reparto ---
    EnReparto = 40,               // El repartidor lo lleva en su ruta
    PrimerIntentoFallido = 41,    // Primer intento de entrega fallido (ausente)
    SegundoIntentoFallido = 42,   // Segundo intento fallido
    DepositadoEnOficina = 43,     // Depositado en oficina para recogida del destinatario

    // --- Fase de entrega ---
    EntregadoEnDomicilio = 50,    // Entregado en el domicilio del destinatario
    EntregadoEnOficina = 51,      // Recogido por el destinatario en oficina
    EntregadoAAutorizado = 52,    // Entregado a persona autorizada

    // --- Incidencias ---
    IncidenciaDireccionIncorrecta = 60,  // Dirección incorrecta o incompleta
    IncidenciaPaqueteDanado = 61,        // Paquete dañado durante el transporte
    IncidenciaDestinatarioRechaza = 62,  // El destinatario rechaza el envío
    IncidenciaOtra = 63,                 // Otra incidencia

    // --- Devolución ---
    EnDevolucionAlRemitente = 70, // En proceso de devolución al remitente
    DevueltoAlRemitente = 71      // Devuelto y entregado al remitente
}
