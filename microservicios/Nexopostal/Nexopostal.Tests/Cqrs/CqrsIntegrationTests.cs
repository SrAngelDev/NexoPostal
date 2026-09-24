using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Nexopostal.Ciudadano.Application.Perfil;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Tests.Ciudadano;
using Xunit;

namespace Nexopostal.Tests.Cqrs;

/// <summary>Commands and queries cross independent scopes and the same real PostgreSQL database.</summary>
public class CqrsIntegrationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private async Task<T> Send<T>(IRequest<T> request)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    [Fact]
    public async Task Profile_CreateAndPartialUpdate_AreImmediatelyVisibleFromANewScope()
    {
        var user = Guid.NewGuid().ToString();
        var created = await Send(new GuardarPerfilCommand(user,
            new ActualizarPerfilDto { DNI = "12345678Z", Telefono = "600000001" }));
        Assert.True(created.Creado);
        var read = await Send(new ObtenerPerfilQuery(user));
        Assert.Equal("600000001", read.Telefono);
        // PostgreSQL timestamps preserve microseconds; DateTime also has sub-microsecond ticks.
        Assert.Equal(created.Perfil.FechaCreacion.Ticks / 10, read.FechaCreacion.Ticks / 10);

        var updated = await Send(new GuardarPerfilCommand(user, new ActualizarPerfilDto { Telefono = "600000002" }));
        Assert.False(updated.Creado);
        read = await Send(new ObtenerPerfilQuery(user));
        Assert.Equal("600000002", read.Telefono);
        Assert.Equal("12345678Z", read.DNI);
    }

    [Fact]
    public async Task Address_OwnershipIsPreserved_AndDeleteIsImmediatelyVisible()
    {
        var owner = Guid.NewGuid().ToString();
        var other = Guid.NewGuid().ToString();
        var input = new CrearDireccionFavoritaDto { Alias = "Casa", NombreDestinatario = "Prueba",
            Direccion = "Calle Prueba 1", CodigoPostal = "28001", Ciudad = "Madrid", Provincia = "Madrid" };
        var address = await Send(new AgregarDireccionCommand(owner, input));
        Assert.Contains(await Send(new ObtenerDireccionesQuery(owner)), d => d.Id == address.Id);
        Assert.Empty(await Send(new ObtenerDireccionesQuery(other)));
        Assert.Null(await Send(new ActualizarDireccionCommand(other, address.Id, input)));
        Assert.False(await Send(new EliminarDireccionCommand(other, address.Id)));
        Assert.Single(await Send(new ObtenerDireccionesQuery(owner)));
        Assert.True(await Send(new EliminarDireccionCommand(owner, address.Id)));
        Assert.Empty(await Send(new ObtenerDireccionesQuery(owner)));
    }
}
