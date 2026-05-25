using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Nexopostal.Shared.Errors;
using Nexopostal.Shared.Results;
using Xunit;

namespace Nexopostal.Tests.Shared;

/// <summary>
/// Tests del puente Result -> IActionResult. Garantiza el mapping de cada DomainError
/// al código HTTP correcto, evitando regresiones en los contratos del API.
/// </summary>
public class ResultExtensionsTests
{
    [Fact]
    public void ToActionResult_SuccessResult_DeberiaDevolver200OK()
    {
        var result = Result.Success<string, DomainError>("hola");

        var action = result.ToActionResult();

        action.Should().BeOfType<OkObjectResult>()
            .Which.Value.Should().Be("hola");
    }

    [Fact]
    public void ToActionResult_NotFoundError_DeberiaDevolver404()
    {
        var result = Result.Failure<string, DomainError>(NotFoundError.Of("Envio", "abc"));

        var action = result.ToActionResult();

        action.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public void ToActionResult_ValidationError_DeberiaDevolver400()
    {
        var result = Result.Failure<string, DomainError>(
            ValidationError.Of("campo", "obligatorio"));

        var action = result.ToActionResult();

        action.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public void ToActionResult_ConflictError_DeberiaDevolver409()
    {
        var result = Result.Failure<string, DomainError>(
            ConflictError.Of("DUP", "ya existe"));

        var action = result.ToActionResult();

        action.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(409);
    }

    [Fact]
    public void ToActionResult_UnauthorizedError_DeberiaDevolver401()
    {
        var result = Result.Failure<string, DomainError>(UnauthorizedError.InvalidCredentials);

        var action = result.ToActionResult();

        action.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(401);
    }

    [Fact]
    public void ToActionResult_ForbiddenError_DeberiaDevolver403()
    {
        var result = Result.Failure<string, DomainError>(
            new ForbiddenError("BLOCKED", "Usuario bloqueado"));

        var action = result.ToActionResult();

        action.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(403);
    }

    [Fact]
    public void ToActionResult_BusinessRuleError_DeberiaDevolver400()
    {
        var result = Result.Failure<string, DomainError>(
            BusinessRuleError.Of("RULE", "no se puede"));

        var action = result.ToActionResult();

        action.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public void UnitResult_Success_ConFactory_DeberiaInvocarOnSuccess()
    {
        var result = UnitResult.Success<DomainError>();

        var action = result.ToActionResult(() => new OkObjectResult(new { ok = true }));

        action.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public void UnitResult_Failure_DeberiaDevolverErrorMapeado()
    {
        var result = UnitResult.Failure<DomainError>(UnauthorizedError.InvalidCredentials);

        var action = result.ToActionResult();

        action.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(401);
    }
}
