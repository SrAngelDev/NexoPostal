using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Nexopostal.Shared.Cqrs;

namespace Nexopostal.Tests.Cqrs;

/// <summary>Los tests de controladores atraviesan MediatR real y mantienen sus mocks de dominio.</summary>
internal static class CqrsTestServices
{
    public static ISender CreateSender(params object[] services)
    {
        var collection = new ServiceCollection();
        collection.AddLogging();
        collection.AddApplicationCqrs(
            typeof(NexoPostal.Auth.Services.IAuthService).Assembly,
            typeof(Nexopostal.Ciudadano.Services.IAdminEnviosService).Assembly,
            typeof(Nexopostal.Intranet.Services.IAsignacionService).Assembly,
            typeof(Nexopostal.Reparto.Services.IRepartoService).Assembly);
        foreach (var service in services)
            foreach (var contract in service.GetType().GetInterfaces().Where(i =>
                i.Namespace?.StartsWith("Nexopostal", StringComparison.OrdinalIgnoreCase) == true))
                collection.AddSingleton(contract, service);
        // Solo mocks/singletons sin recursos externos; MediatR mantiene este proveedor durante el test.
        return collection.BuildServiceProvider().GetRequiredService<ISender>();
    }
}
