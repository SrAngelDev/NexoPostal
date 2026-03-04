using Nexopostal.Intranet.Models;
using Nexopostal.Intranet.Repositories;

namespace Nexopostal.Intranet.Services;

// ===== DTOs de informes =====

/// <summary>
/// Resumen diario de las operaciones logísticas de un CTA o del sistema completo.
/// </summary>
public class ResumenDiario
{
    public DateTime Fecha { get; set; }
    public int PaquetesRecibidos { get; set; }
    public int PaquetesExpedidos { get; set; }
    public int PaquetesEntregados { get; set; }
    public int IncidenciasCreadas { get; set; }
    public int IncidenciasResueltas { get; set; }
    public int OperariosActivos { get; set; }
    public double TasaEficiencia { get; set; }
}

/// <summary>
/// Resumen semanal con desglose diario y totales acumulados.
/// </summary>
public class ResumenSemanal
{
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public List<ResumenDiario> Dias { get; set; } = new();
    public int TotalPaquetesProcesados { get; set; }
    public double TasaEficienciaMedia { get; set; }
    public int TotalIncidencias { get; set; }
}

/// <summary>
/// Alerta operativa detectada automáticamente por el sistema.
/// </summary>
public class AlertaOperativa
{
    public string Tipo { get; set; } = string.Empty; // "PaqueteSinMovimiento", "IncidenciaNoResuelta", "SobrecargaOperario"
    public string Mensaje { get; set; } = string.Empty;
    public string Severidad { get; set; } = "Media"; // "Baja", "Media", "Alta", "Critica"
    public DateTime FechaDeteccion { get; set; }
    public string? NumeroExpedicion { get; set; }
}

// ===== Interfaz =====

/// <summary>
/// Servicio de generación de informes automáticos.
/// Proporciona resúmenes diarios/semanales y alertas operativas activas.
/// </summary>
public interface IInformesAutomaticosService
{
    /// <summary>Genera un resumen de las operaciones de un día concreto.</summary>
    Task<ResumenDiario> GenerarResumenDiario(DateTime fecha);

    /// <summary>Genera un resumen semanal a partir de la fecha de inicio indicada.</summary>
    Task<ResumenSemanal> GenerarResumenSemanal(DateTime fechaInicio);

    /// <summary>Obtiene las alertas operativas activas en el sistema.</summary>
    Task<List<AlertaOperativa>> ObtenerAlertasActivas();
}

// ===== Implementación =====

public class InformesAutomaticosService : IInformesAutomaticosService
{
    private readonly IMovimientoPaqueteRepository _movimientoRepo;
    private readonly IIncidenciaRepository _incidenciaRepo;
    private readonly IAsignacionPaqueteRepository _asignacionRepo;
    private readonly IOperarioCtaRepository _operarioRepo;
    private readonly ICentroTratamientoRepository _ctaRepo;
    private readonly ILogger<InformesAutomaticosService> _logger;

    public InformesAutomaticosService(
        IMovimientoPaqueteRepository movimientoRepo,
        IIncidenciaRepository incidenciaRepo,
        IAsignacionPaqueteRepository asignacionRepo,
        IOperarioCtaRepository operarioRepo,
        ICentroTratamientoRepository ctaRepo,
        ILogger<InformesAutomaticosService> logger)
    {
        _movimientoRepo = movimientoRepo;
        _incidenciaRepo = incidenciaRepo;
        _asignacionRepo = asignacionRepo;
        _operarioRepo = operarioRepo;
        _ctaRepo = ctaRepo;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ResumenDiario> GenerarResumenDiario(DateTime fecha)
    {
        var inicioDia = fecha.Date;
        var finDia = inicioDia.AddDays(1);

        // Obtener todos los CTAs para agregar estadísticas globales
        var ctas = await _ctaRepo.GetAllAsync();

        var paquetesRecibidos = 0;
        var paquetesExpedidos = 0;
        var paquetesEntregados = 0;
        var incidenciasCreadas = 0;
        var incidenciasResueltas = 0;
        var operariosActivos = 0;
        var tareasCompletadas = 0;
        var tareasTotales = 0;

        foreach (var cta in ctas)
        {
            // Contar movimientos recibidos en ese CTA en la fecha
            paquetesRecibidos += await _movimientoRepo.CountRecibidosHoyByCtaAsync(cta.Id);

            // Contar movimientos en tránsito (expedidos) y recibidos
            paquetesExpedidos += await _movimientoRepo.CountByCtaAndEstadoAsync(cta.Id, EstadoMovimiento.EnTransito);
            paquetesEntregados += await _movimientoRepo.CountByCtaAndEstadoAsync(cta.Id, EstadoMovimiento.Recibido);

            // Contar incidencias por CTA
            incidenciasCreadas += await _incidenciaRepo.CountByCtaAndEstadoAsync(cta.Id, EstadoIncidencia.Abierta);
            incidenciasResueltas += await _incidenciaRepo.CountByCtaAndEstadoAsync(cta.Id, EstadoIncidencia.Resuelta);

            // Contar operarios activos
            operariosActivos += await _operarioRepo.CountByCtaIdAsync(cta.Id, true);

            // Tareas completadas hoy
            tareasCompletadas += await _asignacionRepo.CountCompletadasHoyAsync(cta.Id);

            // Tareas totales (pendientes + en progreso + completadas)
            tareasTotales += await _asignacionRepo.CountByCtaAndEstadoAsync(cta.Id, EstadoTarea.Pendiente);
            tareasTotales += await _asignacionRepo.CountByCtaAndEstadoAsync(cta.Id, EstadoTarea.EnProgreso);
            tareasTotales += tareasCompletadas;
        }

        // Calcular tasa de eficiencia: tareas completadas / tareas totales
        var tasaEficiencia = tareasTotales > 0
            ? Math.Round((double)tareasCompletadas / tareasTotales * 100, 2)
            : 0.0;

        var resumen = new ResumenDiario
        {
            Fecha = inicioDia,
            PaquetesRecibidos = paquetesRecibidos,
            PaquetesExpedidos = paquetesExpedidos,
            PaquetesEntregados = paquetesEntregados,
            IncidenciasCreadas = incidenciasCreadas,
            IncidenciasResueltas = incidenciasResueltas,
            OperariosActivos = operariosActivos,
            TasaEficiencia = tasaEficiencia
        };

        _logger.LogInformation(
            "📊 Resumen diario generado → {Fecha:dd/MM/yyyy}: " +
            "Recibidos={Recib}, Expedidos={Exped}, Entregados={Entreg}, " +
            "Incidencias={Inc}, Eficiencia={Efic}%",
            inicioDia, paquetesRecibidos, paquetesExpedidos, paquetesEntregados,
            incidenciasCreadas, tasaEficiencia);

        return resumen;
    }

    /// <inheritdoc />
    public async Task<ResumenSemanal> GenerarResumenSemanal(DateTime fechaInicio)
    {
        var inicio = fechaInicio.Date;
        var fin = inicio.AddDays(7);

        var dias = new List<ResumenDiario>();

        for (var dia = inicio; dia < fin; dia = dia.AddDays(1))
        {
            var resumenDia = await GenerarResumenDiario(dia);
            dias.Add(resumenDia);
        }

        var totalProcesados = dias.Sum(d => d.PaquetesRecibidos + d.PaquetesExpedidos + d.PaquetesEntregados);
        var eficienciaMedia = dias.Count > 0
            ? Math.Round(dias.Average(d => d.TasaEficiencia), 2)
            : 0.0;
        var totalIncidencias = dias.Sum(d => d.IncidenciasCreadas);

        var resumen = new ResumenSemanal
        {
            FechaInicio = inicio,
            FechaFin = fin.AddDays(-1),
            Dias = dias,
            TotalPaquetesProcesados = totalProcesados,
            TasaEficienciaMedia = eficienciaMedia,
            TotalIncidencias = totalIncidencias
        };

        _logger.LogInformation(
            "📊 Resumen semanal generado → {Inicio:dd/MM/yyyy} a {Fin:dd/MM/yyyy}: " +
            "TotalProcesados={Total}, EficienciaMedia={Efic}%, Incidencias={Inc}",
            inicio, resumen.FechaFin, totalProcesados, eficienciaMedia, totalIncidencias);

        return resumen;
    }

    /// <inheritdoc />
    public async Task<List<AlertaOperativa>> ObtenerAlertasActivas()
    {
        var alertas = new List<AlertaOperativa>();

        // 1. Alertas por paquetes sin movimiento (en tránsito > 48h)
        var umbral48h = DateTime.UtcNow.AddHours(-48);
        var movimientosEstancados = await _movimientoRepo.GetEnTransitoAnterioresAAsync(umbral48h);

        foreach (var mov in movimientosEstancados)
        {
            var horasSinMovimiento = (DateTime.UtcNow - (mov.FechaSalida ?? mov.FechaCreacion)).TotalHours;
            var severidad = horasSinMovimiento switch
            {
                >= 168 => "Critica",  // > 7 días
                >= 72 => "Alta",      // > 3 días
                _ => "Media"          // > 48h
            };

            alertas.Add(new AlertaOperativa
            {
                Tipo = "PaqueteSinMovimiento",
                Mensaje = $"Paquete {mov.NumeroExpedicion} en tránsito sin actualización durante " +
                          $"{Math.Round(horasSinMovimiento, 0)}h (desde CTA origen Id:{mov.CtaOrigenId} hacia CTA destino Id:{mov.CtaDestinoId}).",
                Severidad = severidad,
                FechaDeteccion = DateTime.UtcNow,
                NumeroExpedicion = mov.NumeroExpedicion
            });
        }

        // 2. Alertas por incidencias no resueltas
        var ctas = await _ctaRepo.GetAllAsync();

        foreach (var cta in ctas)
        {
            var incidenciasAbiertas = await _incidenciaRepo.CountByCtaAndEstadoAsync(cta.Id, EstadoIncidencia.Abierta);
            var incidenciasEnRevision = await _incidenciaRepo.CountByCtaAndEstadoAsync(cta.Id, EstadoIncidencia.EnRevision);
            var totalNoResueltas = incidenciasAbiertas + incidenciasEnRevision;

            if (totalNoResueltas > 0)
            {
                var severidad = totalNoResueltas switch
                {
                    >= 10 => "Critica",
                    >= 5 => "Alta",
                    >= 2 => "Media",
                    _ => "Baja"
                };

                alertas.Add(new AlertaOperativa
                {
                    Tipo = "IncidenciaNoResuelta",
                    Mensaje = $"El CTA {cta.Codigo} ({cta.Nombre}) tiene {totalNoResueltas} incidencia(s) sin resolver " +
                              $"({incidenciasAbiertas} abiertas, {incidenciasEnRevision} en revisión).",
                    Severidad = severidad,
                    FechaDeteccion = DateTime.UtcNow
                });
            }

            // 3. Alertas por sobrecarga de operarios (muchas tareas pendientes)
            var tareasPendientes = await _asignacionRepo.CountByCtaAndEstadoAsync(cta.Id, EstadoTarea.Pendiente);
            var operariosActivos = await _operarioRepo.CountByCtaIdAsync(cta.Id, true);

            if (operariosActivos > 0)
            {
                var cargaMedia = (double)tareasPendientes / operariosActivos;

                if (cargaMedia > 5)
                {
                    var severidad = cargaMedia switch
                    {
                        >= 15 => "Critica",
                        >= 10 => "Alta",
                        _ => "Media"
                    };

                    alertas.Add(new AlertaOperativa
                    {
                        Tipo = "SobrecargaOperario",
                        Mensaje = $"El CTA {cta.Codigo} tiene una carga media de {Math.Round(cargaMedia, 1)} tareas " +
                                  $"pendientes por operario ({tareasPendientes} tareas, {operariosActivos} operarios activos).",
                        Severidad = severidad,
                        FechaDeteccion = DateTime.UtcNow
                    });
                }
            }
        }

        _logger.LogInformation(
            "🚨 Alertas operativas → {Total} alerta(s) activa(s) detectada(s)",
            alertas.Count);

        return alertas;
    }
}
