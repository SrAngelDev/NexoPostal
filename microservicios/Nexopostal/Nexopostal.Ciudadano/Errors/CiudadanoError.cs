using Nexopostal.Shared.Errors;

namespace Nexopostal.Ciudadano.Errors;

/// <summary>
/// Factory de errores de dominio del módulo Ciudadano. Centraliza códigos estables.
/// </summary>
public static class CiudadanoError
{
    // ─── Envío ───
    public static NotFoundError EnvioNotFound(object id) =>
        new("ENVIO_NOT_FOUND", $"Envío '{id}' no encontrado");

    public static NotFoundError EnvioPorTrackingNotFound(string tracking) =>
        new("ENVIO_NOT_FOUND", $"No existe un envío con número de seguimiento '{tracking}'");

    public static BusinessRuleError EnvioYaPagado(string tracking) =>
        new("ENVIO_YA_PAGADO", $"El envío '{tracking}' ya ha sido pagado");

    public static BusinessRuleError EnvioNoCancelable(string estado) =>
        new("ENVIO_NO_CANCELABLE", $"El envío en estado '{estado}' no se puede cancelar");

    public static BusinessRuleError EnvioNoDevolvible(string estado) =>
        new("ENVIO_NO_DEVOLVIBLE", $"El envío en estado '{estado}' no admite devolución");

    public static ValidationError OficinaDestinoRequerida() =>
        ValidationError.Of("oficinaDestinoId", "Oficina de destino obligatoria cuando TipoEntrega = 'Oficina'");

    public static ValidationError TipoEntregaInvalido(string valor) =>
        ValidationError.Of("tipoEntrega", $"TipoEntrega '{valor}' no es válido. Valores aceptados: Domicilio, Oficina");

    // ─── Pago ───
    public static NotFoundError PagoNotFound(object id) =>
        new("PAGO_NOT_FOUND", $"Pago '{id}' no encontrado");

    public static BusinessRuleError PagoYaProcesado() =>
        new("PAGO_YA_PROCESADO", "El pago ya ha sido procesado previamente");

    public static InfrastructureError StripeError(string detalle) =>
        new("STRIPE_ERROR", $"Error al procesar el pago: {detalle}");

    public static BusinessRuleError WebhookFirmaInvalida() =>
        new("WEBHOOK_INVALID_SIGNATURE", "La firma del webhook de Stripe no es válida");

    // ─── Oficina ───
    public static NotFoundError OficinaNotFound(object id) =>
        new("OFICINA_NOT_FOUND", $"Oficina '{id}' no encontrada");

    public static ValidationError CodigoPostalInvalido(string cp) =>
        ValidationError.Of("codigoPostal", $"Código postal '{cp}' no válido (debe tener 5 dígitos)");

    // ─── Perfil ───
    public static NotFoundError PerfilNotFound(string userId) =>
        new("PERFIL_NOT_FOUND", $"Perfil del usuario '{userId}' no encontrado");

    public static ConflictError DireccionFavoritaDuplicada(string alias) =>
        new("DIRECCION_FAVORITA_DUPLICADA", $"Ya existe una dirección favorita con el alias '{alias}'");

    public static NotFoundError DireccionFavoritaNotFound(object id) =>
        new("DIRECCION_FAVORITA_NOT_FOUND", $"Dirección favorita '{id}' no encontrada");

    // ─── Tarifa ───
    public static ValidationError PesoFueraDeRango(decimal peso) =>
        ValidationError.Of("peso", $"El peso '{peso}' está fuera del rango admitido (0.1 - 30 kg)");

    public static ValidationError DimensionesInvalidas(string dimensiones) =>
        ValidationError.Of("dimensiones", $"Las dimensiones '{dimensiones}' no son válidas (formato: LxAxH en cm)");
}
