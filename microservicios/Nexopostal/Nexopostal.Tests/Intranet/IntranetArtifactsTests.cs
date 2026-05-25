using FluentAssertions;
using FluentValidation.TestHelper;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Errors;
using Nexopostal.Intranet.Validators;
using Xunit;

namespace Nexopostal.Tests.Intranet;

public class IntranetErrorTests
{
    [Fact]
    public void ModoEscaneoInvalido_DeberiaSerValidation()
    {
        var error = IntranetError.ModoEscaneoInvalido("BlabBla");

        error.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().ContainKey("modo");
    }

    [Fact]
    public void PaqueteNotFound_DeberiaTenerCodigoEstable()
    {
        var error = IntranetError.PaqueteNotFound("NXI-1");
        error.Code.Should().Be("PAQUETE_NOT_FOUND");
    }

    [Fact]
    public void AsignacionDuplicada_DeberiaSerConflict()
    {
        var error = IntranetError.AsignacionYaExiste("NXI-9");
        error.Code.Should().Be("ASIGNACION_DUPLICADA");
    }
}

public class ScanRequestDtoValidatorTests
{
    private readonly ScanRequestDtoValidator _validator = new();

    [Fact]
    public void ModoValido_DeberiaPasar()
    {
        var dto = new ScanRequestDto
        {
            CodigoEscaneado = "NXI-00001",
            ModoOperacion = ModosEscaneo.RecepcionCta,
            CtaId = 1
        };

        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ModoInvalido_DeberiaFallar()
    {
        var dto = new ScanRequestDto
        {
            CodigoEscaneado = "NXI-1",
            ModoOperacion = "Inventado"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.ModoOperacion);
    }

    [Fact]
    public void CodigoVacio_DeberiaFallar()
    {
        var dto = new ScanRequestDto
        {
            CodigoEscaneado = "",
            ModoOperacion = ModosEscaneo.Clasificacion
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CodigoEscaneado);
    }
}
