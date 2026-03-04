namespace Nexopostal.Reparto.Models;

// ============================================================
//  Enums del módulo de Reparto (última milla)
// ============================================================

/// <summary>
/// Estado de una ruta de reparto.
/// </summary>
public enum EstadoRuta
{
    /// <summary>Ruta creada pero no iniciada</summary>
    Planificada = 0,

    /// <summary>El repartidor ha salido de la oficina</summary>
    EnCurso = 1,

    /// <summary>Todas las entregas han sido procesadas</summary>
    Completada = 2,

    /// <summary>Ruta cancelada antes de completarse</summary>
    Cancelada = 3,

    /// <summary>Ruta parcialmente completada (quedan entregas sin procesar)</summary>
    CompletadaParcial = 4
}

/// <summary>
/// Estado de un intento de entrega individual.
/// </summary>
public enum EstadoEntrega
{
    /// <summary>Pendiente de reparto</summary>
    Pendiente = 0,

    /// <summary>Repartidor en camino al domicilio</summary>
    EnCamino = 1,

    /// <summary>Entregado exitosamente al destinatario</summary>
    Entregado = 2,

    /// <summary>Nadie en el domicilio, se deja aviso</summary>
    Ausente = 3,

    /// <summary>Dirección incorrecta o no localizable</summary>
    DireccionIncorrecta = 4,

    /// <summary>Destinatario rechazó el paquete</summary>
    Rechazado = 5,

    /// <summary>Devuelto a la oficina tras intento fallido</summary>
    DevueltoAOficina = 6,

    /// <summary>Entregado en punto de recogida alternativo</summary>
    EntregadoPuntoAlternativo = 7
}

/// <summary>
/// Tipo de vehículo del repartidor.
/// </summary>
public enum TipoVehiculo
{
    Furgoneta = 0,
    Moto = 1,
    Bicicleta = 2,
    APie = 3,
    Camion = 4
}
