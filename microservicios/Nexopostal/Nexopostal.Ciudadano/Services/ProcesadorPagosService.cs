using Nexopostal.Ciudadano.Repositories;

namespace Nexopostal.Ciudadano.Services;

/// <summary>
/// Servicio en segundo plano para verificar y procesar pagos pendientes.
/// Ejecuta cada minuto: verifica sesiones de Stripe y actualiza estados.
/// </summary>
public class ProcesadorPagosService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcesadorPagosService> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(1);

    public ProcesadorPagosService(
        IServiceScopeFactory scopeFactory,
        ILogger<ProcesadorPagosService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProcesadorPagosService iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_intervalo, stoppingToken);
                await VerificarPagosPendientes(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en ProcesadorPagosService");
            }
        }
    }

    private async Task VerificarPagosPendientes(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var envioRepo = scope.ServiceProvider.GetRequiredService<IEnvioRepository>();
        var stripeService = scope.ServiceProvider.GetRequiredService<IStripeService>();

        var pendientes = await envioRepo.GetByEstadoInternoAsync(
            Models.EstadoInterno.PendientePago, null);

        var conSesion = pendientes
            .Where(e => !string.IsNullOrEmpty(e.StripeSessionId) && !e.Pagado)
            .ToList();

        var procesados = 0;
        foreach (var envio in conSesion)
        {
            try
            {
                var pagado = await stripeService.VerificarPagoSesion(envio.StripeSessionId!);
                if (pagado)
                {
                    envio.Pagado = true;
                    envio.FechaPago = DateTime.UtcNow;
                    envio.EstadoActual = Models.EstadoEnvio.Admitido;
                    envio.EstadoInternoActual = Models.EstadoInterno.PendienteRecogida;
                    await envioRepo.UpdateAsync(envio);
                    procesados++;

                    _logger.LogInformation(
                        "Pago verificado automáticamente para envío {Tracking}",
                        envio.NumeroSeguimiento);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error al verificar pago de envío {Tracking}",
                    envio.NumeroSeguimiento);
            }
        }

        if (procesados > 0)
            _logger.LogInformation("ProcesadorPagos: {Count} pagos procesados", procesados);
    }
}
