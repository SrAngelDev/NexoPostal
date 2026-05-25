using FluentAssertions;
using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using Xunit;

namespace Nexopostal.Tests.Auth;

/// <summary>
/// Tests para los mappers manuales de Auth. Aseguran que el paso de
/// ApplicationUser -> UsuarioInfoDto/TokenResponseDto preserva los campos.
/// </summary>
public class UsuarioMapperTests
{
    [Fact]
    public void ToInfoDto_DeberiaCopiarTodosLosCampos()
    {
        var user = new ApplicationUser
        {
            Id = "u-1",
            Email = "test@example.com",
            NombreCompleto = "Test User",
            PhoneNumber = "+34 600 000 000",
            FechaRegistro = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            Rol = Rol.OperarioOficina
        };

        var dto = user.ToInfoDto();

        dto.Id.Should().Be("u-1");
        dto.Email.Should().Be("test@example.com");
        dto.NombreCompleto.Should().Be("Test User");
        dto.PhoneNumber.Should().Be("+34 600 000 000");
        dto.FechaRegistro.Should().Be(user.FechaRegistro);
        dto.Rol.Should().Be("OperarioOficina");
    }

    [Fact]
    public void ToInfoDto_ConEmailNulo_DeberiaUsarStringVacio()
    {
        var user = new ApplicationUser { Id = "u-2", NombreCompleto = "Sin Email" };

        var dto = user.ToInfoDto();

        dto.Email.Should().BeEmpty();
    }

    [Fact]
    public void ToTokenResponseDto_DeberiaConstruirRespuestaCompleta()
    {
        var user = new ApplicationUser { Id = "u-3", NombreCompleto = "Token User", Rol = Rol.Cliente };
        var accessExp = DateTime.UtcNow.AddMinutes(60);
        var refreshExp = DateTime.UtcNow.AddDays(14);

        var token = user.ToTokenResponseDto("AT", accessExp, "RT", refreshExp);

        token.Token.Should().Be("AT");
        token.RefreshToken.Should().Be("RT");
        token.Expiration.Should().Be(accessExp);
        token.RefreshTokenExpiration.Should().Be(refreshExp);
        token.User.Should().Be("Token User");
        token.Rol.Should().Be("Cliente");
    }
}
