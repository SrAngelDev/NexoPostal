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

    [Fact]
    public void EnvioPorTrackingNotFound_ContieneTracking()
    {
        var error = CiudadanoError.EnvioPorTrackingNotFound("NXP-TRACK");
        error.Code.Should().Be("ENVIO_NOT_FOUND");
        error.Message.Should().Contain("NXP-TRACK");
    }

    [Fact]
    public void EnvioNoCancelable_ContieneEstado()
    {
        var error = CiudadanoError.EnvioNoCancelable("Entregado");
        error.Code.Should().Be("ENVIO_NO_CANCELABLE");
        error.Message.Should().Contain("Entregado");
    }

    [Fact]
    public void EnvioNoDevolvible_ContieneEstado()
    {
        var error = CiudadanoError.EnvioNoDevolvible("PendientePago");
        error.Code.Should().Be("ENVIO_NO_DEVOLVIBLE");
        error.Message.Should().Contain("PendientePago");
    }

    [Fact]
    public void TipoEntregaInvalido_ContieneValor()
    {
        var error = CiudadanoError.TipoEntregaInvalido("Drone");
        error.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().ContainKey("tipoEntrega");
    }

    [Fact]
    public void PagoNotFound_ContieneId()
    {
        var error = CiudadanoError.PagoNotFound(99);
        error.Code.Should().Be("PAGO_NOT_FOUND");
        error.Message.Should().Contain("99");
    }

    [Fact]
    public void PagoYaProcesado_CodigoEstable()
    {
        var error = CiudadanoError.PagoYaProcesado();
        error.Code.Should().Be("PAGO_YA_PROCESADO");
    }

    [Fact]
    public void WebhookFirmaInvalida_CodigoEstable()
    {
        var error = CiudadanoError.WebhookFirmaInvalida();
        error.Code.Should().Be("WEBHOOK_INVALID_SIGNATURE");
    }

    [Fact]
    public void OficinaNotFound_ContieneId()
    {
        var error = CiudadanoError.OficinaNotFound(7);
        error.Code.Should().Be("OFICINA_NOT_FOUND");
        error.Message.Should().Contain("7");
    }

    [Fact]
    public void CodigoPostalInvalido_ContieneCp()
    {
        var error = CiudadanoError.CodigoPostalInvalido("ABCDE");
        error.Code.Should().Be("VALIDATION_ERROR");
        error.ValidationErrors.Should().ContainKey("codigoPostal");
    }

    [Fact]
    public void PerfilNotFound_ContieneUserId()
    {
        var error = CiudadanoError.PerfilNotFound("user-123");
        error.Code.Should().Be("PERFIL_NOT_FOUND");
        error.Message.Should().Contain("user-123");
    }

    [Fact]
    public void DireccionFavoritaDuplicada_ContieneAlias()
    {
        var error = CiudadanoError.DireccionFavoritaDuplicada("Casa");
        error.Code.Should().Be("DIRECCION_FAVORITA_DUPLICADA");
        error.Message.Should().Contain("Casa");
    }

    [Fact]
    public void DireccionFavoritaNotFound_ContieneId()
    {
        var error = CiudadanoError.DireccionFavoritaNotFound(42);
        error.Code.Should().Be("DIRECCION_FAVORITA_NOT_FOUND");
        error.Message.Should().Contain("42");
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
