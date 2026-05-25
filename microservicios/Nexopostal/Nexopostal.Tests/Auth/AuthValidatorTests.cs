using FluentAssertions;
using FluentValidation.TestHelper;
using NexoPostal.Auth.DTOs;
using NexoPostal.Auth.Validators;
using Xunit;

namespace Nexopostal.Tests.Auth;

/// <summary>
/// Tests para los validators de FluentValidation del módulo Auth.
/// </summary>
public class AuthValidatorTests
{
    [Fact]
    public void LoginDtoValidator_EmailVacio_DeberiaFallar()
    {
        var validator = new LoginDtoValidator();
        var result = validator.TestValidate(new LoginDto { Email = "", Password = "ok" });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void LoginDtoValidator_EmailInvalido_DeberiaFallar()
    {
        var validator = new LoginDtoValidator();
        var result = validator.TestValidate(new LoginDto { Email = "no-es-email", Password = "ok" });

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void LoginDtoValidator_DatosValidos_DeberiaPasar()
    {
        var validator = new LoginDtoValidator();
        var result = validator.TestValidate(new LoginDto { Email = "ok@test.com", Password = "ok" });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RegisterDtoValidator_PasswordCorta_DeberiaFallar()
    {
        var validator = new RegisterDtoValidator();
        var result = validator.TestValidate(new RegisterDto
        {
            Email = "ok@test.com", Password = "123", NombreCompleto = "Test"
        });

        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RegisterDtoValidator_NombreCorto_DeberiaFallar()
    {
        var validator = new RegisterDtoValidator();
        var result = validator.TestValidate(new RegisterDto
        {
            Email = "ok@test.com", Password = "secreto1", NombreCompleto = "A"
        });

        result.ShouldHaveValidationErrorFor(x => x.NombreCompleto);
    }

    [Fact]
    public void CambiarPasswordDtoValidator_NuevaPasswordCorta_DeberiaFallar()
    {
        var validator = new CambiarPasswordDtoValidator();
        var result = validator.TestValidate(new CambiarPasswordDto
        {
            PasswordActual = "viejo", NuevaPassword = "abc"
        });

        result.ShouldHaveValidationErrorFor(x => x.NuevaPassword);
    }

    [Fact]
    public void ResetPasswordDtoValidator_TokenVacio_DeberiaFallar()
    {
        var validator = new ResetPasswordDtoValidator();
        var result = validator.TestValidate(new ResetPasswordDto
        {
            Email = "ok@test.com", Token = "", NuevaPassword = "secreto1"
        });

        result.ShouldHaveValidationErrorFor(x => x.Token);
    }
}
