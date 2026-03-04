using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

// ===== DTOs de resultado =====

/// <summary>
/// Resultado de la clasificación automática de un paquete.
/// Contiene el CTA de destino, la zona postal y la prioridad calculada.
/// </summary>
public class ResultadoClasificacion
{
    public string NumeroExpedicion { get; set; } = string.Empty;
    public int CtaDestinoId { get; set; }
    public string CtaDestinoNombre { get; set; } = string.Empty;
    public string ZonaPostal { get; set; } = string.Empty;
    public bool EsUrgente { get; set; }
    public int Prioridad { get; set; } // 1=urgente, 2=express, 3=estandar
}

/// <summary>
/// Agrupación de paquetes por zona postal y CTA de destino para expedición.
/// </summary>
public class PaqueteAgrupado
{
    public string ZonaPostal { get; set; } = string.Empty;
    public int CtaDestinoId { get; set; }
    public List<string> Expediciones { get; set; } = new();
    public int TotalPaquetes { get; set; }
}

// ===== Interfaz =====

/// <summary>
/// Servicio de clasificación automática de paquetes.
/// Determina el CTA de destino óptimo basándose en el código postal
/// y agrupa paquetes por ruta/zona para optimizar la expedición.
/// </summary>
public interface IClasificacionAutomaticaService
{
    /// <summary>
    /// Clasifica un paquete determinando el CTA de destino, zona postal y prioridad.
    /// </summary>
    Task<ResultadoClasificacion> ClasificarPaquete(string numeroExpedicion, string cpDestino, decimal pesoKg, bool esUrgente);

    /// <summary>
    /// Agrupa una lista de expediciones por ruta/zona postal para expedición conjunta.
    /// </summary>
    Task<List<PaqueteAgrupado>> AgruparPorRuta(List<string> expediciones);

    /// <summary>
    /// Determina qué CTA debe recibir un paquete basándose en el código postal de destino.
    /// </summary>
    Task<int> AsignarCTADestino(string cpDestino);
}

// ===== Implementación =====

public class ClasificacionAutomaticaService : IClasificacionAutomaticaService
{
    private readonly ICentroTratamientoRepository _ctaRepo;
    private readonly IRutaCtaRepository _rutaRepo;
    private readonly IMovimientoPaqueteRepository _movimientoRepo;
    private readonly ILogger<ClasificacionAutomaticaService> _logger;

    public ClasificacionAutomaticaService(
        ICentroTratamientoRepository ctaRepo,
        IRutaCtaRepository rutaRepo,
        IMovimientoPaqueteRepository movimientoRepo,
        ILogger<ClasificacionAutomaticaService> logger)
    {
        _ctaRepo = ctaRepo;
        _rutaRepo = rutaRepo;
        _movimientoRepo = movimientoRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResultadoClasificacion> ClasificarPaquete(
        string numeroExpedicion, string cpDestino, decimal pesoKg, bool esUrgente)
    {
        if (string.IsNullOrWhiteSpace(cpDestino) || cpDestino.Length < 2)
            throw new ArgumentException("El código postal de destino debe tener al menos 2 dígitos.", nameof(cpDestino));

        var prefijo = cpDestino[..2];

        // Resolver el CTA de destino a partir del prefijo del CP
        var ruta = await _rutaRepo.GetByPrefijoAsync(prefijo);
        if (ruta == null)
        {
            _logger.LogWarning(
                "No se encontró ruta para el prefijo CP {Prefijo} del paquete {Expedicion}",
                prefijo, numeroExpedicion);
            throw new InvalidOperationException($"No existe ruta configurada para el prefijo de CP '{prefijo}'.");
        }

        // Calcular prioridad: 1=urgente, 2=express (>5kg urgente-like), 3=estándar
        var prioridad = esUrgente ? 1 : (pesoKg > 5m ? 2 : 3);

        var resultado = new ResultadoClasificacion
        {
            NumeroExpedicion = numeroExpedicion,
            CtaDestinoId = ruta.Cta.Id,
            CtaDestinoNombre = ruta.Cta.Nombre,
            ZonaPostal = prefijo,
            EsUrgente = esUrgente,
            Prioridad = prioridad
        };

        _logger.LogInformation(
            "📦 Clasificación automática → {Expedicion}: CP {Cp} → zona {Zona} → {Cta} (prioridad: {Prioridad})",
            numeroExpedicion, cpDestino, prefijo, ruta.Cta.Nombre, prioridad);

        return resultado;
    }

    /// <inheritdoc />
    public async Task<List<PaqueteAgrupado>> AgruparPorRuta(List<string> expediciones)
    {
        if (expediciones == null || expediciones.Count == 0)
            return new List<PaqueteAgrupado>();

        // Agrupar por zona postal (CTA destino) usando los movimientos existentes
        var agrupaciones = new Dictionary<string, PaqueteAgrupado>();

        foreach (var expedicion in expediciones)
        {
            var movimientos = await _movimientoRepo.GetByExpedicionAsync(expedicion);

            // Tomar el último movimiento programado para determinar el destino
            var movimiento = movimientos
                .OrderByDescending(m => m.FechaCreacion)
                .FirstOrDefault();

            if (movimiento == null)
            {
                _logger.LogWarning("No se encontraron movimientos para la expedición {Expedicion}", expedicion);
                continue;
            }

            // Obtener el CTA destino para construir la clave de zona
            var ctaDestino = await _ctaRepo.GetByIdAsync(movimiento.CtaDestinoId);
            var zonaPostal = ctaDestino?.CodigoPostal?[..2] ?? "00";
            var clave = $"{zonaPostal}-{movimiento.CtaDestinoId}";

            if (!agrupaciones.ContainsKey(clave))
            {
                agrupaciones[clave] = new PaqueteAgrupado
                {
                    ZonaPostal = zonaPostal,
                    CtaDestinoId = movimiento.CtaDestinoId
                };
            }

            agrupaciones[clave].Expediciones.Add(expedicion);
            agrupaciones[clave].TotalPaquetes = agrupaciones[clave].Expediciones.Count;
        }

        var resultado = agrupaciones.Values.ToList();

        _logger.LogInformation(
            "📦 Agrupación por ruta → {TotalExpediciones} expediciones agrupadas en {TotalGrupos} lotes",
            expediciones.Count, resultado.Count);

        return resultado;
    }

    /// <inheritdoc />
    public async Task<int> AsignarCTADestino(string cpDestino)
    {
        if (string.IsNullOrWhiteSpace(cpDestino) || cpDestino.Length < 2)
            throw new ArgumentException("El código postal de destino debe tener al menos 2 dígitos.", nameof(cpDestino));

        var prefijo = cpDestino[..2];
        var ruta = await _rutaRepo.GetByPrefijoAsync(prefijo);

        if (ruta == null)
        {
            _logger.LogWarning("No se encontró CTA para el prefijo CP {Prefijo}", prefijo);
            throw new InvalidOperationException($"No existe ruta configurada para el prefijo de CP '{prefijo}'.");
        }

        _logger.LogInformation(
            "🏭 CTA asignado → CP {Cp} (prefijo {Prefijo}) → CTA {CtaId} ({CtaNombre})",
            cpDestino, prefijo, ruta.Cta.Id, ruta.Cta.Nombre);

        return ruta.Cta.Id;
    }
}
