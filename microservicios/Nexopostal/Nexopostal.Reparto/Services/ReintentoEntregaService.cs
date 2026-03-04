using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Repositories;

namespace Nexopostal.Reparto.Services;

// ============================================================
//  Interfaz del servicio de Reintento de Entregas
// ============================================================

public interface IReintentoEntregaService
{
    /// <summary>
    /// Programa un reintento de entrega para un paquete cuyo intento previo falló.
    /// </summary>
    Task<bool> ProgramarReintento(int entregaId, string motivo);

    /// <summary>
    /// Obtiene todas las entregas que requieren reintento
    /// (primer o segundo intento fallido en la fecha actual).
    /// </summary>
    Task<List<EntregaPaquete>> ObtenerEntregasParaReintento();

    /// <summary>
    /// Cancela los reintentos pendientes para una entrega concreta.
    /// </summary>
    Task<bool> CancelarReintentos(int entregaId);

    /// <summary>
    /// Determina la acción a seguir según el número de intentos:
    /// "Reintentar", "DepositarOficina" o "Devolver".
    /// </summary>
    Task<string> DeterminarAccion(int entregaId);
}

// ============================================================
//  Implementación del servicio de Reintento de Entregas
// ============================================================

public class ReintentoEntregaService : IReintentoEntregaService
{
    private const int MaxIntentosReintento = 2;
    private const int DiasLimiteRecogidaOficina = 5;

    private readonly IEntregaPaqueteRepository _entregaRepo;
    private readonly IRutaRepartoRepository _rutaRepo;
    private readonly ILogger<ReintentoEntregaService> _logger;

    public ReintentoEntregaService(
        IEntregaPaqueteRepository entregaRepo,
        IRutaRepartoRepository rutaRepo,
        ILogger<ReintentoEntregaService> logger)
    {
        _entregaRepo = entregaRepo;
        _rutaRepo = rutaRepo;
        _logger = logger;
    }

    // ═══════════════════════════════════════════
    //  PROGRAMAR REINTENTO
    // ═══════════════════════════════════════════

    public async Task<bool> ProgramarReintento(int entregaId, string motivo)
    {
        var entrega = await _entregaRepo.GetByIdAsync(entregaId);
        if (entrega == null)
        {
            _logger.LogWarning("No se encontró la entrega {EntregaId} para programar reintento", entregaId);
            return false;
        }

        // Solo se puede reintentar si el estado es Ausente, DireccionIncorrecta o Rechazado
        if (entrega.Estado != EstadoEntrega.Ausente &&
            entrega.Estado != EstadoEntrega.DireccionIncorrecta &&
            entrega.Estado != EstadoEntrega.Rechazado)
        {
            _logger.LogWarning(
                "Entrega {EntregaId} no es apta para reintento. Estado actual: {Estado}",
                entregaId, entrega.Estado);
            return false;
        }

        var accion = await DeterminarAccionInterna(entrega);

        if (accion != "Reintentar")
        {
            _logger.LogInformation(
                "Entrega {EntregaId} no se reintenta. Acción determinada: {Accion}",
                entregaId, accion);
            return false;
        }

        // Crear un nuevo registro de entrega para el reintento
        var nuevoIntento = new EntregaPaquete
        {
            RutaRepartoId = entrega.RutaRepartoId,
            NumeroExpedicion = entrega.NumeroExpedicion,
            NumeroSeguimiento = entrega.NumeroSeguimiento,
            DireccionEntrega = entrega.DireccionEntrega,
            CodigoPostal = entrega.CodigoPostal,
            Ciudad = entrega.Ciudad,
            NombreDestinatario = entrega.NombreDestinatario,
            TelefonoDestinatario = entrega.TelefonoDestinatario,
            NumeroIntento = entrega.NumeroIntento + 1,
            OrdenEnRuta = 0, // Se reasignará al optimizar la ruta
            Estado = EstadoEntrega.Pendiente,
            Observaciones = $"Reintento automático. Motivo del fallo anterior: {motivo}",
            FechaCreacion = DateTime.UtcNow
        };

        await _entregaRepo.CreateAsync(nuevoIntento);

        _logger.LogInformation(
            "Reintento programado para entrega {EntregaId} (expedición {Expedicion}). Intento #{Intento}",
            entregaId, entrega.NumeroExpedicion, nuevoIntento.NumeroIntento);

        return true;
    }

    // ═══════════════════════════════════════════
    //  OBTENER ENTREGAS PARA REINTENTO
    // ═══════════════════════════════════════════

    public async Task<List<EntregaPaquete>> ObtenerEntregasParaReintento()
    {
        var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
        var rutasHoy = await _rutaRepo.GetByFechaAsync(hoy);

        if (rutasHoy.Count == 0)
        {
            _logger.LogInformation("No hay rutas para la fecha {Fecha}", hoy);
            return new List<EntregaPaquete>();
        }

        var rutaIds = rutasHoy.Select(r => r.Id).ToList();
        var todasEntregas = await _entregaRepo.GetByRutaIdsAsync(rutaIds);

        // Filtrar entregas fallidas aptas para reintento (intento 1 o 2 fallidos)
        var entregasParaReintento = todasEntregas
            .Where(e => (e.Estado == EstadoEntrega.Ausente ||
                         e.Estado == EstadoEntrega.DireccionIncorrecta ||
                         e.Estado == EstadoEntrega.Rechazado) &&
                        e.NumeroIntento <= MaxIntentosReintento)
            .ToList();

        _logger.LogInformation(
            "Se encontraron {Count} entregas aptas para reintento en la fecha {Fecha}",
            entregasParaReintento.Count, hoy);

        return entregasParaReintento;
    }

    // ═══════════════════════════════════════════
    //  CANCELAR REINTENTOS
    // ═══════════════════════════════════════════

    public async Task<bool> CancelarReintentos(int entregaId)
    {
        var entrega = await _entregaRepo.GetByIdAsync(entregaId);
        if (entrega == null)
        {
            _logger.LogWarning("No se encontró la entrega {EntregaId} para cancelar reintentos", entregaId);
            return false;
        }

        if (entrega.Estado != EstadoEntrega.Pendiente)
        {
            _logger.LogWarning(
                "No se puede cancelar reintento de entrega {EntregaId}: estado {Estado} no es Pendiente",
                entregaId, entrega.Estado);
            return false;
        }

        entrega.Estado = EstadoEntrega.DevueltoAOficina;
        entrega.Observaciones = $"Reintento cancelado manualmente. {entrega.Observaciones}";
        await _entregaRepo.UpdateAsync(entrega);

        _logger.LogInformation(
            "Reintentos cancelados para entrega {EntregaId} (expedición {Expedicion})",
            entregaId, entrega.NumeroExpedicion);

        return true;
    }

    // ═══════════════════════════════════════════
    //  DETERMINAR ACCIÓN
    // ═══════════════════════════════════════════

    public async Task<string> DeterminarAccion(int entregaId)
    {
        var entrega = await _entregaRepo.GetByIdAsync(entregaId);
        if (entrega == null)
        {
            _logger.LogWarning("No se encontró la entrega {EntregaId} para determinar acción", entregaId);
            return "Devolver";
        }

        return await DeterminarAccionInterna(entrega);
    }

    // ═══════════════════════════════════════════
    //  MÉTODOS PRIVADOS
    // ═══════════════════════════════════════════

    /// <summary>
    /// Lógica interna de determinación de acción:
    ///   - Intento 1 fallido → "Reintentar" (programar siguiente día hábil)
    ///   - Intento 2 fallido → "DepositarOficina" (notificar al cliente para recogida)
    ///   - Más de 5 días desde creación → "Devolver" (devolver al remitente)
    /// </summary>
    private Task<string> DeterminarAccionInterna(EntregaPaquete entrega)
    {
        // Si han pasado más de 5 días desde la creación, devolver al remitente
        var diasTranscurridos = (DateTime.UtcNow - entrega.FechaCreacion).TotalDays;
        if (diasTranscurridos > DiasLimiteRecogidaOficina)
        {
            _logger.LogInformation(
                "Entrega {EntregaId}: han pasado {Dias:F0} días → Devolver al remitente",
                entrega.Id, diasTranscurridos);
            return Task.FromResult("Devolver");
        }

        // Intento 1 fallido → Reintentar
        if (entrega.NumeroIntento == 1)
        {
            _logger.LogInformation(
                "Entrega {EntregaId}: intento 1 fallido → Reintentar",
                entrega.Id);
            return Task.FromResult("Reintentar");
        }

        // Intento 2 fallido → Depositar en oficina
        if (entrega.NumeroIntento == 2)
        {
            _logger.LogInformation(
                "Entrega {EntregaId}: intento 2 fallido → Depositar en oficina para recogida",
                entrega.Id);
            return Task.FromResult("DepositarOficina");
        }

        // Intento 3+ → Devolver al remitente
        _logger.LogInformation(
            "Entrega {EntregaId}: intento {Intento} fallido → Devolver al remitente",
            entrega.Id, entrega.NumeroIntento);
        return Task.FromResult("Devolver");
    }
}
