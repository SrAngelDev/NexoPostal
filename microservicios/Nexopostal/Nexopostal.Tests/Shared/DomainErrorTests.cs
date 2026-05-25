using FluentAssertions;
using Nexopostal.Shared.Errors;
using Xunit;

namespace Nexopostal.Tests.Shared;

/// <summary>
/// Tests para los factories de <see cref="DomainError"/> de la librería Shared.
/// Garantizan estabilidad de los códigos de error consumidos por el frontend.
/// </summary>
public class DomainErrorTests
{
    [Fact]
    public void NotFoundError_Of_DeberiaIncluirEntidadEId()
    {
        var error = NotFoundError.Of("Usuario", 42);

        error.Code.Should().Be("USUARIO_NOT_FOUND");
        error.Message.Should().Contain("Usuario").And.Contain("42");
    }

    [Fact]
    public void ValidationError_Of_ConDictionary_DeberiaContenerErrores()
    {
        var detalles = new Dictionary<string, string[]>
        {
            ["email"] = new[] { "El email es obligatorio" },
            ["password"] = new[] { "Mínimo 6 caracteres" }
        };

        var error = ValidationError.Of("Datos inválidos", detalles);

        error.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().HaveCount(2);
        error.ValidationErrors["email"].Should().Contain("El email es obligatorio");
    }

    [Fact]
    public void ValidationError_Of_SingleField_DeberiaCrearDictionary()
    {
        var error = ValidationError.Of("email", "Formato inválido");

        error.ValidationErrors.Should().ContainKey("email");
        error.ValidationErrors["email"].Should().Contain("Formato inválido");
    }

    [Fact]
    public void ConflictError_Of_DeberiaPreservarCodigo()
    {
        var error = ConflictError.Of("DUPLICATE", "Ya existe");

        error.Code.Should().Be("DUPLICATE");
        error.Message.Should().Be("Ya existe");
    }

    [Fact]
    public void UnauthorizedError_InvalidCredentials_DeberiaSerSingleton()
    {
        UnauthorizedError.InvalidCredentials.Should().BeSameAs(UnauthorizedError.InvalidCredentials);
        UnauthorizedError.InvalidCredentials.Code.Should().Be("INVALID_CREDENTIALS");
    }
}
