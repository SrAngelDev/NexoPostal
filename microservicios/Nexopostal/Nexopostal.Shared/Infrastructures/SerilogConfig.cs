using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Nexopostal.Shared.Infrastructures;

/// <summary>
/// Helpers para configurar Serilog como provider unificado de logging en todos los microservicios.
/// </summary>
public static class SerilogConfig
{
    /// <summary>
    /// Crea un logger Serilog con sinks Console + File usando la configuración por defecto.
    /// </summary>
    public static Serilog.ILogger CreateDefaultLogger(string serviceName) =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] [{Service}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .CreateLogger();

    /// <summary>Registra Serilog como provider principal de logging en el host.</summary>
    public static WebApplicationBuilder AddNexopostalSerilog(this WebApplicationBuilder builder, string serviceName)
    {
        Log.Logger = CreateDefaultLogger(serviceName);
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger);
        builder.Host.UseSerilog(Log.Logger);
        return builder;
    }
}
