using FluentAssertions;
using FluentValidation.TestHelper;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Errors;
using Nexopostal.Ciudadano.Validators;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

public class CiudadanoErrorTests
{
    [Fact]
    public void EnvioNotFound_DeberiaTenerCodigoEstable()
    {
        var error = CiudadanoError.EnvioNotFound(42);

        error.Code.Should().Be("ENVIO_NOT_FOUND");
        error.Message.Should().Contain("42");
    }

    [Fact]
    public void OficinaDestinoRequerida_DeberiaSerValidationError()
    {
        var error = CiudadanoError.OficinaDestinoRequerida();

        error.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().ContainKey("oficinaDestinoId");
    }

    [Fact]
    public void StripeError_DeberiaSerInfrastructure()
    {
        var error = CiudadanoError.StripeError("timeout");

        error.Code.Should().Be("STRIPE_ERROR");
    }

    [Fact]
    public void EnvioYaPagado_DeberiaSerBusinessRule()
    {
        var error = CiudadanoError.EnvioYaPagado("NX12345");

        error.Code.Should().Be("ENVIO_YA_PAGADO");
        error.Message.Should().Contain("NX12345");
    }
}

public class CrearEnvioDtoValidatorTests
{
    private readonly CrearEnvioDtoValidator _validator = new();

    private static CrearEnvioDto ValidoDomicilio() => new()
    {
        Peso = 1.5m,
        Dimensiones = "20x15x10",
        NombreRemitente = "Ana",
        Origen = "Calle 1",
        CodigoPostalOrigen = "28001",
        NombreDestinatario = "Luis",
        Destino = "Calle 2",
        CodigoPostalDestino = "08002",
        OficinaOrigenId = 1,
        TipoEntrega = "Domicilio"
    };

    [Fact]
    public void DtoValido_DeberiaPasar()
    {
        var result = _validator.TestValidate(ValidoDomicilio());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void TipoEntregaOficina_SinOficinaDestino_DeberiaFallar()
    {
        var dto = ValidoDomicilio();
        dto.TipoEntrega = "Oficina";
        dto.OficinaDestinoId = null;

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.OficinaDestinoId);
    }

    [Fact]
    public void CodigoPostalNoNumerico_DeberiaFallar()
    {
        var dto = ValidoDomicilio();
        dto.CodigoPostalOrigen = "ABC12";

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CodigoPostalOrigen);
    }

    [Fact]
    public void TipoEntregaInvalido_DeberiaFallar()
    {
        var dto = ValidoDomicilio();
        dto.TipoEntrega = "Drone";

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.TipoEntrega);
    }

    [Fact]
    public void PesoFueraRango_DeberiaFallar()
    {
        var dto = ValidoDomicilio();
        dto.Peso = 50m;

        var result = _validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Peso);
    }
}
