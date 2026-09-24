using Microsoft.Extensions.DependencyInjection;
using Nexopostal.Intranet.Services;
using Nexopostal.Shared.Cqrs;

namespace Nexopostal.Intranet.Application;
public static class IntranetCqrsRegistration
{
    public static IServiceCollection AddIntranetCqrs(this IServiceCollection services)
    {
        services.AddApplicationCqrs(typeof(IntranetCqrsRegistration).Assembly);
        services.AddScoped<Nexopostal.Intranet.Application.AdminCta.IAdminCtaCommands>(provider => provider.GetRequiredService<IAdminCtaService>());
        services.AddScoped<Nexopostal.Intranet.Application.Admision.IAdmisionCommands>(provider => provider.GetRequiredService<IAdmisionService>());
        services.AddScoped<Nexopostal.Intranet.Application.Asignacion.IAsignacionCommands>(provider => provider.GetRequiredService<IAsignacionService>());
        services.AddScoped<Nexopostal.Intranet.Application.Asignacion.IAsignacionQueries>(provider => provider.GetRequiredService<IAsignacionService>());
        services.AddScoped<Nexopostal.Intranet.Application.Broadcast.IBroadcastCommands>(provider => provider.GetRequiredService<IBroadcastService>());
        services.AddScoped<Nexopostal.Intranet.Application.Clasificacion.IClasificacionQueries>(provider => provider.GetRequiredService<IClasificacionService>());
        services.AddScoped<Nexopostal.Intranet.Application.Historial.IHistorialCommands>(provider => provider.GetRequiredService<IHistorialService>());
        services.AddScoped<Nexopostal.Intranet.Application.Historial.IHistorialQueries>(provider => provider.GetRequiredService<IHistorialService>());
        services.AddScoped<Nexopostal.Intranet.Application.Incidencia.IIncidenciaCommands>(provider => provider.GetRequiredService<IIncidenciaService>());
        services.AddScoped<Nexopostal.Intranet.Application.Incidencia.IIncidenciaQueries>(provider => provider.GetRequiredService<IIncidenciaService>());
        services.AddScoped<Nexopostal.Intranet.Application.Movimiento.IMovimientoCommands>(provider => provider.GetRequiredService<IMovimientoService>());
        services.AddScoped<Nexopostal.Intranet.Application.Movimiento.IMovimientoQueries>(provider => provider.GetRequiredService<IMovimientoService>());
        services.AddScoped<Nexopostal.Intranet.Application.OficinaPostal.IOficinaPostalCommands>(provider => provider.GetRequiredService<IOficinaPostalService>());
        services.AddScoped<Nexopostal.Intranet.Application.OficinaPostal.IOficinaPostalQueries>(provider => provider.GetRequiredService<IOficinaPostalService>());
        services.AddScoped<Nexopostal.Intranet.Application.Operario.IOperarioCommands>(provider => provider.GetRequiredService<IOperarioService>());
        services.AddScoped<Nexopostal.Intranet.Application.Operario.IOperarioQueries>(provider => provider.GetRequiredService<IOperarioService>());
        services.AddScoped<Nexopostal.Intranet.Application.ScanProcessor.IScanProcessorCommands>(provider => provider.GetRequiredService<IScanProcessorService>());
        return services;
    }
}
