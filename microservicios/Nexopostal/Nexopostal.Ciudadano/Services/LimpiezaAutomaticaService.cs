using Nexopostal.Ciudadano.Repositories;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio en segundo plano para limpieza periódica de datos obsoletos.
/// Ejecuta cada 6 horas: limpia sesiones de pago abandonadas y datos temporales.
/// </summary>
public class LimpiezaAutomaticaService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LimpiezaAutomaticaService> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromHours(6);

    public LimpiezaAutomaticaService(
        IServiceScopeFactory scopeFactory,
        ILogger<LimpiezaAutomaticaService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LimpiezaAutomaticaService iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_intervalo, stoppingToken);
                await EjecutarLimpieza(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en LimpiezaAutomaticaService");
            }
        }
    }

    private async Task EjecutarLimpieza(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var envioRepo = scope.ServiceProvider.GetRequiredService<IEnvioRepository>();

        // 1. Find and clean abandoned payment sessions (>24h in PendientePago without StripeSessionId)
        var enviosPendientes = await envioRepo.GetByEstadoInternoAsync(
            Models.EstadoInterno.PendientePago, null);

        var limite = DateTime.UtcNow.AddHours(-24);
        var abandonados = enviosPendientes
            .Where(e => e.FechaCreacion < limite && !e.Pagado)
            .ToList();

        foreach (var envio in abandonados)
        {
            envio.Observaciones = string.IsNullOrEmpty(envio.Observaciones)
                ? "[Auto] Sesión de pago expirada - limpieza automática"
                : $"{envio.Observaciones}\n[Auto] Sesión de pago expirada - limpieza automática";
            await envioRepo.UpdateAsync(envio);
        }

        _logger.LogInformation(
            "Limpieza completada: {AbandonadosCount} sesiones de pago abandonadas marcadas",
            abandonados.Count);
    }
}
