using Nexopostal.Shared.Errors;

namespace Nexopostal.Reparto.Errors;

/// <summary>
/// Factory de errores de dominio del módulo Reparto.
/// </summary>
public static class RepartoError
{
    // ─── Repartidor ───
    public static NotFoundError RepartidorNotFound(object id) =>
        new("REPARTIDOR_NOT_FOUND", $"Repartidor '{id}' no encontrado");

    public static ConflictError RepartidorYaExiste(string codigoEmpleado) =>
        new("REPARTIDOR_DUPLICADO", $"Ya existe un repartidor con código '{codigoEmpleado}'");

    public static BusinessRuleError RepartidorInactivo(string codigo) =>
        new("REPARTIDOR_INACTIVO", $"El repartidor '{codigo}' está inactivo");

    // ─── Ruta de reparto ───
    public static NotFoundError RutaNotFound(object id) =>
        new("RUTA_NOT_FOUND", $"Ruta de reparto '{id}' no encontrada");

    public static BusinessRuleError RutaYaCerrada() =>
        new("RUTA_YA_CERRADA", "La ruta ya está cerrada");

    public static BusinessRuleError RutaNoIniciada() =>
        new("RUTA_NO_INICIADA", "La ruta aún no se ha iniciado");

    public static BusinessRuleError RutaSinEntregas() =>
        new("RUTA_SIN_ENTREGAS", "No se puede iniciar una ruta sin entregas");

    public static ValidationError FechaRepartoInvalida(string fecha) =>
        ValidationError.Of("fechaReparto", $"Fecha de reparto '{fecha}' no válida (yyyy-MM-dd)");

    // ─── Entrega ───
    public static NotFoundError EntregaNotFound(object id) =>
        new("ENTREGA_NOT_FOUND", $"Entrega '{id}' no encontrada");

    public static BusinessRuleError EntregaYaCompletada() =>
        new("ENTREGA_YA_COMPLETADA", "La entrega ya está completada");

    public static BusinessRuleError EntregaMaxIntentosAlcanzados(int maxIntentos) =>
        new("ENTREGA_MAX_INTENTOS", $"Se alcanzó el máximo de intentos de entrega ({maxIntentos})");

    public static ValidationError EstadoEntregaInvalido(string estado) =>
        ValidationError.Of("estado", $"Estado de entrega '{estado}' no válido");

    public static ValidationError FirmaOFotoRequerida() =>
        ValidationError.Of("evidencia", "Se requiere firma o foto para confirmar la entrega");

    // ─── Vehículo ───
    public static NotFoundError VehiculoNotFound(object id) =>
        new("VEHICULO_NOT_FOUND", $"Vehículo '{id}' no encontrado");

    public static ConflictError VehiculoMatriculaDuplicada(string matricula) =>
        new("VEHICULO_MATRICULA_DUPLICADA", $"Ya existe un vehículo con matrícula '{matricula}'");

    public static BusinessRuleError VehiculoYaAsignado(string matricula, string repartidor) =>
        new("VEHICULO_YA_ASIGNADO", $"El vehículo '{matricula}' ya está asignado a '{repartidor}'");

    public static BusinessRuleError VehiculoInactivo(string matricula) =>
        new("VEHICULO_INACTIVO", $"El vehículo '{matricula}' está inactivo");

    // ─── Ubicación ───
    public static ValidationError CoordenadasInvalidas() =>
        ValidationError.Of("coordenadas", "Latitud o longitud fuera del rango admitido");
}
