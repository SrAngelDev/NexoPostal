using FluentAssertions;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using Xunit;

namespace Nexopostal.Tests.Auth;

public class AdminUsuarioMapperTests
{
    [Fact]
    public void ToListItemDto_MapeaCampos_NoBloqueado()
    {
        var ahora = DateTimeOffset.UtcNow;
        var u = new ApplicationUser
        {
            Id = "u1",
            NombreCompleto = "N",
            Email = "n@x.es",
            CodigoEmpleado = "E001",
            PhoneNumber = "+34",
            Rol = Rol.OperarioOficina,
            FechaRegistro = DateTime.UtcNow,
            LockoutEnd = null,
            Eliminado = false,
            EliminadoEnUtc = null
        };

        var dto = u.ToListItemDto(ahora);
        dto.Id.Should().Be("u1");
        dto.NombreCompleto.Should().Be("N");
        dto.Rol.Should().Be("OperarioOficina");
        dto.CodigoEmpleado.Should().Be("E001");
        dto.Bloqueado.Should().BeFalse();
        dto.Eliminado.Should().BeFalse();
    }

    [Fact]
    public void ToListItemDto_LockoutEndFuturo_BloqueadoTrue()
    {
        var ahora = DateTimeOffset.UtcNow;
        var u = new ApplicationUser { NombreCompleto = "X", LockoutEnd = ahora.AddHours(1), Rol = Rol.Cliente };
        u.ToListItemDto(ahora).Bloqueado.Should().BeTrue();
    }

    [Fact]
    public void ToListItemDto_LockoutEndPasado_BloqueadoFalse()
    {
        var ahora = DateTimeOffset.UtcNow;
        var u = new ApplicationUser { NombreCompleto = "X", LockoutEnd = ahora.AddHours(-1), Rol = Rol.Cliente };
        u.ToListItemDto(ahora).Bloqueado.Should().BeFalse();
    }

    [Fact]
    public void ToListItemDto_EmailNull_DevuelveStringVacio()
    {
        var u = new ApplicationUser { NombreCompleto = "X", Email = null, Rol = Rol.Cliente };
        u.ToListItemDto(DateTimeOffset.UtcNow).Email.Should().BeEmpty();
    }

    [Fact]
    public void ToListItemDtos_MapeaColeccion()
    {
        var ahora = DateTimeOffset.UtcNow;
        var lista = new[]
        {
            new ApplicationUser { Id = "1", NombreCompleto = "A", Rol = Rol.Cliente },
            new ApplicationUser { Id = "2", NombreCompleto = "B", Rol = Rol.Cliente }
        };
        var dtos = lista.ToListItemDtos(ahora);
        dtos.Should().HaveCount(2);
        dtos.Select(d => d.Id).Should().BeEquivalentTo(new[] { "1", "2" });
    }
}
