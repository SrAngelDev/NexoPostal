using FluentAssertions;
using Nexopostal.Shared.Exceptions;
using Xunit;

namespace Nexopostal.Tests.Shared;

/// <summary>
/// Tests para las excepciones de dominio del Shared (clases a 0% de cobertura).
/// </summary>
public class DomainExceptionsTests
{
    [Fact]
    public void NotFoundException_DeberiaPreservarMensaje()
    {
        var ex = new NotFoundException("Usuario 42 no encontrado");
        ex.Message.Should().Be("Usuario 42 no encontrado");
        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void BusinessException_DeberiaPreservarMensaje()
    {
        var ex = new BusinessException("Regla de negocio violada");
        ex.Message.Should().Be("Regla de negocio violada");
    }

    [Fact]
    public void ConflictException_DeberiaPreservarMensaje()
    {
        var ex = new ConflictException("Estado duplicado");
        ex.Message.Should().Be("Estado duplicado");
    }

    [Fact]
    public void ValidationException_SinErrores_DeberiaInicializarDictionaryVacio()
    {
        var ex = new ValidationException("Datos inválidos");
        ex.Message.Should().Be("Datos inválidos");
        ex.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidationException_ConErrores_DeberiaPreservarlos()
    {
        var detalles = new Dictionary<string, string[]>
        {
            ["email"] = ["obligatorio"],
            ["pwd"] = ["mínimo 6 caracteres", "sin espacios"]
        };
        var ex = new ValidationException("Datos inválidos", detalles);

        ex.Errors.Should().HaveCount(2);
        ex.Errors["email"].Should().ContainSingle().Which.Should().Be("obligatorio");
        ex.Errors["pwd"].Should().HaveCount(2);
    }

    [Fact]
    public void ValidationException_ErroresNull_DeberiaInicializarVacio()
    {
        var ex = new ValidationException("X", null);
        ex.Errors.Should().NotBeNull().And.BeEmpty();
    }
}
