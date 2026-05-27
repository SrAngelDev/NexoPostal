using FluentAssertions;
using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.DTOs.Admin;
using NexoPostal.Auth.Mappers;
using NexoPostal.Auth.Models;
using NexoPostal.Auth.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace Nexopostal.Tests.Auth;

/// <summary>
/// Tests adicionales para validators y mappers de Auth que estaban a 0% de cobertura:
/// ActualizarUsuarioDtoValidator, RefreshTokenRequestDtoValidator, RegisterDtoValidator
/// y AdminUsuarioMapper.
/// </summary>
public class AuthExtraCoverageTests
{
    // ─── ActualizarUsuarioDtoValidator ────────────────────────────────────────
    [Fact]
    public void ActualizarUsuario_DatosValidos_DeberiaPasar()
    {
        var validator = new ActualizarUsuarioDtoValidator();
        var result = validator.TestValidate(new ActualizarUsuarioDto
        {
            NombreCompleto = "Pepe",
            Email = "pepe@test.com"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ActualizarUsuario_NombreVacio_DeberiaFallar()
    {
        var validator = new ActualizarUsuarioDtoValidator();
        var result = validator.TestValidate(new ActualizarUsuarioDto
        {
            NombreCompleto = "",
            Email = "pepe@test.com"
        });
        result.ShouldHaveValidationErrorFor(x => x.NombreCompleto);
    }

    [Fact]
    public void ActualizarUsuario_EmailVacio_DeberiaFallar()
    {
        var validator = new ActualizarUsuarioDtoValidator();
        var result = validator.TestValidate(new ActualizarUsuarioDto
        {
            NombreCompleto = "Pepe",
            Email = ""
        });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void ActualizarUsuario_EmailInvalido_DeberiaFallar()
    {
        var validator = new ActualizarUsuarioDtoValidator();
        var result = validator.TestValidate(new ActualizarUsuarioDto
        {
            NombreCompleto = "Pepe",
            Email = "no-email"
        });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // ─── RefreshTokenRequestDtoValidator ──────────────────────────────────────
    [Fact]
    public void RefreshToken_TokenVacio_DeberiaFallar()
    {
        var validator = new RefreshTokenRequestDtoValidator();
        var result = validator.TestValidate(new RefreshTokenRequestDto { RefreshToken = "" });
        result.ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }

    [Fact]
    public void RefreshToken_TokenValido_DeberiaPasar()
    {
        var validator = new RefreshTokenRequestDtoValidator();
        var result = validator.TestValidate(new RefreshTokenRequestDto { RefreshToken = "abc.def.ghi" });
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── RegisterDtoValidator (rutas no cubiertas) ────────────────────────────
    [Fact]
    public void Register_EmailVacio_DeberiaFallar()
    {
        var validator = new RegisterDtoValidator();
        var result = validator.TestValidate(new RegisterDto
        {
            Email = "", Password = "secreto1", NombreCompleto = "Pepe"
        });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Register_EmailInvalido_DeberiaFallar()
    {
        var validator = new RegisterDtoValidator();
        var result = validator.TestValidate(new RegisterDto
        {
            Email = "no-email", Password = "secreto1", NombreCompleto = "Pepe"
        });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Register_PasswordVacia_DeberiaFallar()
    {
        var validator = new RegisterDtoValidator();
        var result = validator.TestValidate(new RegisterDto
        {
            Email = "ok@test.com", Password = "", NombreCompleto = "Pepe"
        });
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Register_NombreVacio_DeberiaFallar()
    {
        var validator = new RegisterDtoValidator();
        var result = validator.TestValidate(new RegisterDto
        {
            Email = "ok@test.com", Password = "secreto1", NombreCompleto = ""
        });
        result.ShouldHaveValidationErrorFor(x => x.NombreCompleto);
    }

    [Fact]
    public void Register_DatosValidos_DeberiaPasar()
    {
        var validator = new RegisterDtoValidator();
        var result = validator.TestValidate(new RegisterDto
        {
            Email = "ok@test.com", Password = "secreto1", NombreCompleto = "Pepe Test"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── CambiarPasswordDtoValidator (rutas extra) ────────────────────────────
    [Fact]
    public void CambiarPassword_PasswordActualVacio_DeberiaFallar()
    {
        var validator = new CambiarPasswordDtoValidator();
        var result = validator.TestValidate(new CambiarPasswordDto
        {
            PasswordActual = "", NuevaPassword = "secreto1"
        });
        result.ShouldHaveValidationErrorFor(x => x.PasswordActual);
    }

    [Fact]
    public void CambiarPassword_DatosValidos_DeberiaPasar()
    {
        var validator = new CambiarPasswordDtoValidator();
        var result = validator.TestValidate(new CambiarPasswordDto
        {
            PasswordActual = "viejo123", NuevaPassword = "secreto1"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── SolicitarResetPasswordDtoValidator ───────────────────────────────────
    [Fact]
    public void SolicitarReset_EmailValido_DeberiaPasar()
    {
        var validator = new SolicitarResetPasswordDtoValidator();
        var result = validator.TestValidate(new SolicitarResetPasswordDto { Email = "ok@test.com" });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void SolicitarReset_EmailVacio_DeberiaFallar()
    {
        var validator = new SolicitarResetPasswordDtoValidator();
        var result = validator.TestValidate(new SolicitarResetPasswordDto { Email = "" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void SolicitarReset_EmailInvalido_DeberiaFallar()
    {
        var validator = new SolicitarResetPasswordDtoValidator();
        var result = validator.TestValidate(new SolicitarResetPasswordDto { Email = "no-email" });
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    // ─── ResetPasswordDtoValidator (rutas extra) ──────────────────────────────
    [Fact]
    public void ResetPassword_NuevaPasswordCorta_DeberiaFallar()
    {
        var validator = new ResetPasswordDtoValidator();
        var result = validator.TestValidate(new ResetPasswordDto
        {
            Email = "ok@test.com", Token = "tok", NuevaPassword = "123"
        });
        result.ShouldHaveValidationErrorFor(x => x.NuevaPassword);
    }

    [Fact]
    public void ResetPassword_DatosValidos_DeberiaPasar()
    {
        var validator = new ResetPasswordDtoValidator();
        var result = validator.TestValidate(new ResetPasswordDto
        {
            Email = "ok@test.com", Token = "token-xyz", NuevaPassword = "secreto1"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    // ─── AdminUsuarioMapper ───────────────────────────────────────────────────
    private static ApplicationUser BuildUser(DateTimeOffset? lockoutEnd = null, bool eliminado = false)
        => new()
        {
            Id = "user-1",
            NombreCompleto = "Pepe Test",
            Email = "pepe@test.com",
            CodigoEmpleado = "EMP-001",
            PhoneNumber = "600000000",
            Rol = Rol.OperarioOficina,
            FechaRegistro = new DateTime(2024, 1, 1),
            LockoutEnd = lockoutEnd,
            Eliminado = eliminado,
            EliminadoEnUtc = eliminado ? new DateTime(2024, 6, 1) : null
        };

    [Fact]
    public void ToListItemDto_UsuarioActivo_DeberiaMapearTodosLosCampos()
    {
        var ahora = new DateTimeOffset(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var u = BuildUser();
        var dto = u.ToListItemDto(ahora);

        dto.Id.Should().Be(u.Id);
        dto.NombreCompleto.Should().Be(u.NombreCompleto);
        dto.Email.Should().Be(u.Email);
        dto.CodigoEmpleado.Should().Be(u.CodigoEmpleado);
        dto.PhoneNumber.Should().Be(u.PhoneNumber);
        dto.Rol.Should().Be("OperarioOficina");
        dto.FechaRegistro.Should().Be(u.FechaRegistro);
        dto.Bloqueado.Should().BeFalse();
        dto.Eliminado.Should().BeFalse();
        dto.EliminadoEnUtc.Should().BeNull();
    }

    [Fact]
    public void ToListItemDto_LockoutEndFuturo_DeberiaMarcarBloqueado()
    {
        var ahora = new DateTimeOffset(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var u = BuildUser(lockoutEnd: ahora.AddHours(1));
        var dto = u.ToListItemDto(ahora);
        dto.Bloqueado.Should().BeTrue();
    }

    [Fact]
    public void ToListItemDto_LockoutEndPasado_NoDeberiaMarcarBloqueado()
    {
        var ahora = new DateTimeOffset(2024, 5, 1, 12, 0, 0, TimeSpan.Zero);
        var u = BuildUser(lockoutEnd: ahora.AddHours(-1));
        var dto = u.ToListItemDto(ahora);
        dto.Bloqueado.Should().BeFalse();
    }

    [Fact]
    public void ToListItemDto_EmailNull_DeberiaUsarVacio()
    {
        var ahora = DateTimeOffset.UtcNow;
        var u = BuildUser();
        u.Email = null;
        var dto = u.ToListItemDto(ahora);
        dto.Email.Should().Be(string.Empty);
    }

    [Fact]
    public void ToListItemDto_UsuarioEliminado_DeberiaIncluirFlags()
    {
        var ahora = DateTimeOffset.UtcNow;
        var u = BuildUser(eliminado: true);
        var dto = u.ToListItemDto(ahora);
        dto.Eliminado.Should().BeTrue();
        dto.EliminadoEnUtc.Should().NotBeNull();
    }

    [Fact]
    public void ToListItemDtos_VariosUsuarios_DeberiaMapearTodos()
    {
        var ahora = DateTimeOffset.UtcNow;
        var users = new[]
        {
            BuildUser(),
            BuildUser(eliminado: true),
            BuildUser(lockoutEnd: ahora.AddHours(2))
        };
        var dtos = users.ToListItemDtos(ahora);
        dtos.Should().HaveCount(3);
        dtos[1].Eliminado.Should().BeTrue();
        dtos[2].Bloqueado.Should().BeTrue();
    }
}
