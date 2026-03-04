using Nexopostal.Ciudadano.Models;
using Nexopostal.Ciudadano.Repositories;

namespace Nexopostal.Ciudadano.Services;

// ===== DTO =====

/// <summary>
/// Representa una devolución pendiente de ser procesada.
/// </summary>
public class DevolucionPendiente
{
    public string NumeroSeguimiento { get; set; } = string.Empty;
    public string NumeroExpedicion { get; set; } = string.Empty;
    public string Motivo { get; set; } = string.Empty;
    public decimal CosteOriginal { get; set; }
    public decimal ReembolsoEstimado { get; set; }
    public DateTime FechaInicio { get; set; }
    public string EstadoInterno { get; set; } = string.Empty;
}

// ===== INTERFAZ =====

/// <summary>
/// Servicio para gestionar el proceso automatizado de devolución de paquetes.
/// Controla el ciclo de vida de una devolución: inicio → recepción → reembolso.
/// </summary>
public interface IProcesoDevolucionService
{
    /// <summary>
    /// Inicia el proceso de devolución de un envío, cambiando su estado a EnDevolucionAlRemitente.
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    /// <param name="motivo">Motivo de la devolución</param>
    /// <returns>true si la devolución se inició correctamente; false si no se pudo iniciar</returns>
    Task<bool> IniciarDevolucion(string numeroSeguimiento, string motivo);

    /// <summary>
    /// Procesa la recepción de un paquete devuelto, cambiando su estado a DevueltoAlRemitente.
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    /// <returns>true si se procesó correctamente; false si no se encontró o no estaba en devolución</returns>
    Task<bool> ProcesarDevolucionRecibida(string numeroSeguimiento);

    /// <summary>
    /// Calcula el importe de reembolso para un envío (80% del coste original, mínimo 2€ de comisión).
    /// </summary>
    /// <param name="numeroSeguimiento">Número de seguimiento público (NX...ES)</param>
    /// <returns>Importe estimado de reembolso en EUR</returns>
    Task<decimal> CalcularReembolso(string numeroSeguimiento);

    /// <summary>
    /// Obtiene todas las devoluciones actualmente en proceso (estado EnDevolucionAlRemitente).
    /// </summary>
    Task<List<DevolucionPendiente>> ObtenerDevolucionesPendientes();
}

// ===== IMPLEMENTACIÓN =====

public class ProcesoDevolucionService : IProcesoDevolucionService
{
    private readonly IEnvioRepository _envioRepository;
    private readonly ILogger<ProcesoDevolucionService> _logger;

    /// <summary>
    /// Comisión administrativa mínima que se retiene en cada reembolso.
    /// </summary>
    private const decimal ComisionAdminMinima = 2.00m;

    /// <summary>
    /// Porcentaje del coste original que se devuelve al cliente.
    /// </summary>
    private const decimal PorcentajeReembolso = 0.80m;

    public ProcesoDevolucionService(
        IEnvioRepository envioRepository,
        ILogger<ProcesoDevolucionService> logger)
    {
        _envioRepository = envioRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IniciarDevolucion(string numeroSeguimiento, string motivo)
    {
        var envio = await _envioRepository.GetByTrackingAsync(numeroSeguimiento);

        if (envio is null)
        {
            _logger.LogWarning(
                "Devolución rechazada: envío {NumeroSeguimiento} no encontrado",
                numeroSeguimiento);
            return false;
        }

        // No se puede devolver un envío ya devuelto o ya entregado
        if (envio.EstadoInternoActual == Models.EstadoInterno.DevueltoAlRemitente ||
            envio.EstadoInternoActual == Models.EstadoInterno.EnDevolucionAlRemitente)
        {
            _logger.LogWarning(
                "Devolución rechazada: envío {NumeroSeguimiento} ya está en proceso de devolución o devuelto (Estado: {Estado})",
                numeroSeguimiento, envio.EstadoInternoActual);
            return false;
        }

        if (envio.EstadoInternoActual == Models.EstadoInterno.EntregadoEnDomicilio ||
            envio.EstadoInternoActual == Models.EstadoInterno.EntregadoEnOficina ||
            envio.EstadoInternoActual == Models.EstadoInterno.EntregadoAAutorizado)
        {
            _logger.LogWarning(
                "Devolución rechazada: envío {NumeroSeguimiento} ya fue entregado (Estado: {Estado})",
                numeroSeguimiento, envio.EstadoInternoActual);
            return false;
        }

        // Actualizar estados
        envio.EstadoInternoActual = Models.EstadoInterno.EnDevolucionAlRemitente;
        envio.EstadoActual = EstadoEnvio.Devuelto;

        // Añadir nota a Observaciones con el motivo
        var nota = $"[DEVOLUCIÓN {DateTime.UtcNow:dd/MM/yyyy HH:mm}] Motivo: {motivo}";
        envio.Observaciones = string.IsNullOrEmpty(envio.Observaciones)
            ? nota
            : $"{envio.Observaciones} | {nota}";

        await _envioRepository.UpdateAsync(envio);

        _logger.LogInformation(
            "📦 Devolución iniciada · {NumeroSeguimiento} · Motivo: {Motivo}",
            numeroSeguimiento, motivo);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ProcesarDevolucionRecibida(string numeroSeguimiento)
    {
        var envio = await _envioRepository.GetByTrackingAsync(numeroSeguimiento);

        if (envio is null)
        {
            _logger.LogWarning(
                "Recepción de devolución rechazada: envío {NumeroSeguimiento} no encontrado",
                numeroSeguimiento);
            return false;
        }

        if (envio.EstadoInternoActual != Models.EstadoInterno.EnDevolucionAlRemitente)
        {
            _logger.LogWarning(
                "Recepción de devolución rechazada: envío {NumeroSeguimiento} no está en proceso de devolución (Estado: {Estado})",
                numeroSeguimiento, envio.EstadoInternoActual);
            return false;
        }

        envio.EstadoInternoActual = Models.EstadoInterno.DevueltoAlRemitente;

        var nota = $"[DEVOLUCIÓN RECIBIDA {DateTime.UtcNow:dd/MM/yyyy HH:mm}] Paquete devuelto al remitente";
        envio.Observaciones = string.IsNullOrEmpty(envio.Observaciones)
            ? nota
            : $"{envio.Observaciones} | {nota}";

        await _envioRepository.UpdateAsync(envio);

        _logger.LogInformation(
            "📦 Devolución completada · {NumeroSeguimiento} · Devuelto al remitente",
            numeroSeguimiento);

        return true;
    }

    /// <inheritdoc />
    public async Task<decimal> CalcularReembolso(string numeroSeguimiento)
    {
        var envio = await _envioRepository.GetByTrackingAsync(numeroSeguimiento);

        if (envio is null)
        {
            _logger.LogWarning(
                "Cálculo de reembolso: envío {NumeroSeguimiento} no encontrado",
                numeroSeguimiento);
            return 0m;
        }

        var costeOriginal = envio.CosteCalculado;

        // Reembolso = 80% del coste original, asegurando al menos 2€ de comisión
        var reembolso = costeOriginal * PorcentajeReembolso;
        var comisionReal = costeOriginal - reembolso;

        // Si la comisión calculada es menor que la mínima, ajustar el reembolso
        if (comisionReal < ComisionAdminMinima)
        {
            reembolso = costeOriginal - ComisionAdminMinima;
        }

        // El reembolso no puede ser negativo
        reembolso = Math.Max(reembolso, 0m);

        _logger.LogInformation(
            "💰 Reembolso calculado · {NumeroSeguimiento} · Original: {CosteOriginal}€ → Reembolso: {Reembolso}€",
            numeroSeguimiento, costeOriginal, reembolso);

        return Math.Round(reembolso, 2);
    }

    /// <inheritdoc />
    public async Task<List<DevolucionPendiente>> ObtenerDevolucionesPendientes()
    {
        var envios = await _envioRepository.GetByEstadoInternoAsync(
            Models.EstadoInterno.EnDevolucionAlRemitente, null);

        var pendientes = envios.Select(e => new DevolucionPendiente
        {
            NumeroSeguimiento = e.NumeroSeguimiento,
            NumeroExpedicion = e.NumeroExpedicion,
            Motivo = ExtraerMotivoDevolucion(e.Observaciones),
            CosteOriginal = e.CosteCalculado,
            ReembolsoEstimado = CalcularReembolsoInterno(e.CosteCalculado),
            FechaInicio = e.FechaCreacion,
            EstadoInterno = e.EstadoInternoActual.ToString()
        }).ToList();

        _logger.LogInformation(
            "📋 Devoluciones pendientes consultadas · Total: {Count}",
            pendientes.Count);

        return pendientes;
    }

    // ===== MÉTODOS PRIVADOS =====

    /// <summary>
    /// Calcula el reembolso de forma síncrona (para uso interno en proyecciones).
    /// </summary>
    private static decimal CalcularReembolsoInterno(decimal costeOriginal)
    {
        var reembolso = costeOriginal * PorcentajeReembolso;
        var comisionReal = costeOriginal - reembolso;

        if (comisionReal < ComisionAdminMinima)
        {
            reembolso = costeOriginal - ComisionAdminMinima;
        }

        return Math.Max(Math.Round(reembolso, 2), 0m);
    }

    /// <summary>
    /// Extrae el motivo de devolución de las observaciones del envío.
    /// </summary>
    private static string ExtraerMotivoDevolucion(string? observaciones)
    {
        if (string.IsNullOrEmpty(observaciones))
            return "Sin motivo especificado";

        // Buscar el último motivo de devolución en las observaciones
        const string marcador = "Motivo: ";
        var index = observaciones.LastIndexOf(marcador, StringComparison.Ordinal);

        if (index < 0)
            return "Sin motivo especificado";

        var inicio = index + marcador.Length;
        var fin = observaciones.IndexOf(" |", inicio, StringComparison.Ordinal);

        return fin > inicio
            ? observaciones[inicio..fin].Trim()
            : observaciones[inicio..].Trim();
    }
}
