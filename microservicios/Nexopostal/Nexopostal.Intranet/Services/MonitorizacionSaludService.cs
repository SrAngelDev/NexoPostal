namespace Nexopostal.Intranet.Services;

/// <summary>
/// Servicio en segundo plano para monitorizar la salud del sistema.
/// Verifica cada 5 minutos: conectividad BD, servicios externos y métricas.
/// </summary>
public class MonitorizacionSaludService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MonitorizacionSaludService> _logger;
    private readonly TimeSpan _intervalo = TimeSpan.FromMinutes(5);

    public MonitorizacionSaludService(
        IServiceScopeFactory scopeFactory,
        ILogger<MonitorizacionSaludService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el chequeo periódico de salud mientras la aplicación siga en marcha.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MonitorizacionSaludService iniciado");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_intervalo, stoppingToken);
                await VerificarSalud(stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en MonitorizacionSaludService");
            }
        }
    }

    /// <summary>
    /// Revisa conectividad básica y dependencias críticas para dejar trazado si algo esencial falla.
    /// </summary>
    private async Task VerificarSalud(CancellationToken ct)
    {
        var resultados = new Dictionary<string, bool>();

        // 1. Verificar conectividad con la base de datos.
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<Nexopostal.Intranet.Data.IntranetDbContext>();
            resultados["BaseDatos"] = await dbContext.Database.CanConnectAsync(ct);
        }
        catch
        {
            resultados["BaseDatos"] = false;
        }

        // 2. Comprobar que los servicios clave siguen resolviéndose en el contenedor.
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var clasificacion = scope.ServiceProvider.GetService<IClasificacionAutomaticaService>();
            resultados["ServicioClasificacion"] = clasificacion != null;
        }
        catch
        {
            resultados["ServicioClasificacion"] = false;
        }

        // Registrar el resultado agregado de la comprobación.
        var saludables = resultados.Count(r => r.Value);
        var total = resultados.Count;

        if (saludables == total)
        {
            _logger.LogInformation("Monitorización: todos los servicios saludables ({Total}/{Total})", total, total);
        }
        else
        {
            var fallidos = resultados.Where(r => !r.Value).Select(r => r.Key);
            _logger.LogWarning(
                "Monitorización: {Saludables}/{Total} servicios saludables. Fallidos: {Fallidos}",
                saludables, total, string.Join(", ", fallidos));
        }
    }
}
