using Nexopostal.Shared.Errors;

namespace Nexopostal.Intranet.Errors;

/// <summary>
/// Factory de errores de dominio del módulo Intranet (oficinas, CTA, operarios, escaneo, asignaciones).
/// </summary>
public static class IntranetError
{
    // ─── Escaneo ───
    public static ValidationError ModoEscaneoInvalido(string modo) =>
        ValidationError.Of("modo", $"Modo de escaneo '{modo}' no es válido");

    public static BusinessRuleError ModoEscaneoNoPermitido(string modo, string rol) =>
        new("MODO_ESCANEO_NO_PERMITIDO", $"El rol '{rol}' no puede ejecutar el modo '{modo}'");

    public static NotFoundError PaqueteNotFound(string identificador) =>
        new("PAQUETE_NOT_FOUND", $"No se encontró paquete con identificador '{identificador}'");

    public static BusinessRuleError EscanDuplicadoMismoEstado(string estadoActual) =>
        new("ESCAN_DUPLICADO", $"El paquete ya se encuentra en estado '{estadoActual}'");

    public static BusinessRuleError EscanFueraDeFlujo(string estadoActual, string accion) =>
        new("ESCAN_FUERA_DE_FLUJO", $"No se puede ejecutar '{accion}' sobre un paquete en estado '{estadoActual}'");

    // ─── Asignaciones ───
    public static NotFoundError AsignacionNotFound(object id) =>
        new("ASIGNACION_NOT_FOUND", $"Asignación '{id}' no encontrada");

    public static BusinessRuleError AsignacionYaCompletada() =>
        new("ASIGNACION_YA_COMPLETADA", "La asignación ya está completada");

    public static BusinessRuleError AsignacionYaExiste(string numeroExpedicion) =>
        new("ASIGNACION_DUPLICADA", $"Ya existe una asignación activa para '{numeroExpedicion}'");

    public static ValidationError TipoTareaInvalido(string tipo) =>
        ValidationError.Of("tipoTarea", $"TipoTarea '{tipo}' no válido");

    // ─── Operarios ───
    public static NotFoundError OperarioNotFound(object id) =>
        new("OPERARIO_NOT_FOUND", $"Operario '{id}' no encontrado");

    public static NotFoundError OperarioCtaNotFound(object id) =>
        new("OPERARIO_CTA_NOT_FOUND", $"OperarioCTA '{id}' no encontrado");

    public static NotFoundError OperarioOficinaNotFound(object id) =>
        new("OPERARIO_OFICINA_NOT_FOUND", $"OperarioOficina '{id}' no encontrado");

    public static ConflictError OperarioYaAsignadoACta(string identityUserId, int ctaId) =>
        new("OPERARIO_YA_ASIGNADO", $"El usuario '{identityUserId}' ya está asignado al CTA '{ctaId}'");

    // ─── Oficina / CTA ───
    public static NotFoundError OficinaNotFound(object id) =>
        new("OFICINA_NOT_FOUND", $"Oficina '{id}' no encontrada");

    public static NotFoundError CtaNotFound(object id) =>
        new("CTA_NOT_FOUND", $"CTA '{id}' no encontrado");

    public static NotFoundError RutaCtaNotFound(string prefijo) =>
        new("RUTA_CTA_NOT_FOUND", $"No hay ruta CTA configurada para el prefijo '{prefijo}'");

    public static BusinessRuleError CtaInactivo(string codigo) =>
        new("CTA_INACTIVO", $"El CTA '{codigo}' está inactivo");

    // ─── Incidencias ───
    public static NotFoundError IncidenciaNotFound(object id) =>
        new("INCIDENCIA_NOT_FOUND", $"Incidencia '{id}' no encontrada");

    public static BusinessRuleError IncidenciaYaResuelta() =>
        new("INCIDENCIA_YA_RESUELTA", "La incidencia ya está resuelta");

    // ─── Movimientos troncales ───
    public static NotFoundError MovimientoNotFound(object id) =>
        new("MOVIMIENTO_NOT_FOUND", $"Movimiento troncal '{id}' no encontrado");

    public static BusinessRuleError MovimientoYaCerrado() =>
        new("MOVIMIENTO_YA_CERRADO", "El movimiento troncal ya está cerrado");
}
