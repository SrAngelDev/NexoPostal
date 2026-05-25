using FluentAssertions;
using FluentValidation.TestHelper;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Errors;
using Nexopostal.Reparto.Validators;
using Xunit;

namespace Nexopostal.Tests.Reparto;

public class RepartoErrorTests
{
    [Fact]
    public void RepartidorNotFound_DeberiaTenerCodigoEstable()
    {
        var error = RepartoError.RepartidorNotFound(1);
        error.Code.Should().Be("REPARTIDOR_NOT_FOUND");
    }

    [Fact]
    public void EntregaMaxIntentos_DeberiaSerBusinessRule()
    {
        var error = RepartoError.EntregaMaxIntentosAlcanzados(3);
        error.Code.Should().Be("ENTREGA_MAX_INTENTOS");
        error.Message.Should().Contain("3");
    }

    [Fact]
    public void VehiculoMatriculaDuplicada_DeberiaSerConflict()
    {
        var error = RepartoError.VehiculoMatriculaDuplicada("1234ABC");
        error.Code.Should().Be("VEHICULO_MATRICULA_DUPLICADA");
    }
}

public class CrearRutaRepartoDtoValidatorTests
{
    private readonly CrearRutaRepartoDtoValidator _validator = new();

    [Fact]
    public void DtoValido_DeberiaPasar()
    {
        var dto = new CrearRutaRepartoDto
        {
            RepartidorId = 1,
            FechaReparto = "2025-01-15",
            OficinaOrigenJsonId = 5,
            OficinaOrigenNombre = "Oficina Centro"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void FechaFormatoInvalido_DeberiaFallar()
    {
        var dto = new CrearRutaRepartoDto
        {
            RepartidorId = 1,
            FechaReparto = "15/01/2025",
            OficinaOrigenJsonId = 5,
            OficinaOrigenNombre = "Oficina"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.FechaReparto);
    }

    [Fact]
    public void RepartidorIdCero_DeberiaFallar()
    {
        var dto = new CrearRutaRepartoDto
        {
            RepartidorId = 0,
            FechaReparto = "2025-01-15",
            OficinaOrigenJsonId = 5,
            OficinaOrigenNombre = "Oficina"
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.RepartidorId);
    }
}

public class CrearVehiculoDtoValidatorTests
{
    private readonly CrearVehiculoDtoValidator _validator = new();

    [Fact]
    public void DtoValido_DeberiaPasar()
    {
        var dto = new CrearVehiculoDto
        {
            Matricula = "1234ABC",
            Tipo = Nexopostal.Reparto.Models.TipoVehiculo.Furgoneta
        };

        var result = _validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void MatriculaVacia_DeberiaFallar()
    {
        var dto = new CrearVehiculoDto
        {
            Matricula = "",
            Tipo = Nexopostal.Reparto.Models.TipoVehiculo.Furgoneta
        };

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Matricula);
    }
}
