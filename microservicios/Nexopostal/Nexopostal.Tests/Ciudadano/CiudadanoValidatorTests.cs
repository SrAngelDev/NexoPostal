using FluentValidation.TestHelper;
using Nexopostal.Ciudadano.DTOs;
using Nexopostal.Ciudadano.Validators;
using Xunit;

namespace Nexopostal.Tests.Ciudadano;

/// <summary>
/// Tests para los validators de FluentValidation del módulo Ciudadano.
/// Cubre las clases de Nexopostal.Ciudadano.Validators que estaban a 0% de cobertura.
/// </summary>
public class CiudadanoValidatorTests
{
    // ─── CotizarEnvioDtoValidator ─────────────────────────────────────────────
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.05)]
    [InlineData(31.0)]
    [InlineData(100.0)]
    public void CotizarEnvio_PesoFueraDeRango_DeberiaFallar(double peso)
    {
        var validator = new CotizarEnvioDtoValidator();
        var result = validator.TestValidate(new CotizarEnvioDto
        {
            Peso = (decimal)peso,
            CodigoPostalOrigen = "28001",
            CodigoPostalDestino = "08001"
        });
        result.ShouldHaveValidationErrorFor(x => x.Peso);
    }

    [Fact]
    public void CotizarEnvio_DatosValidos_DeberiaPasar()
    {
        var validator = new CotizarEnvioDtoValidator();
        var result = validator.TestValidate(new CotizarEnvioDto
        {
            Peso = 5m,
            CodigoPostalOrigen = "28001",
            CodigoPostalDestino = "08001"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234")]
    [InlineData("ABCDE")]
    [InlineData("123456")]
    public void CotizarEnvio_CodigoPostalOrigenInvalido_DeberiaFallar(string cp)
    {
        var validator = new CotizarEnvioDtoValidator();
        var result = validator.TestValidate(new CotizarEnvioDto
        {
            Peso = 5m,
            CodigoPostalOrigen = cp,
            CodigoPostalDestino = "08001"
        });
        result.ShouldHaveValidationErrorFor(x => x.CodigoPostalOrigen);
    }

    [Fact]
    public void CotizarEnvio_CodigoPostalDestinoInvalido_DeberiaFallar()
    {
        var validator = new CotizarEnvioDtoValidator();
        var result = validator.TestValidate(new CotizarEnvioDto
        {
            Peso = 5m,
            CodigoPostalOrigen = "28001",
            CodigoPostalDestino = "ABCDE"
        });
        result.ShouldHaveValidationErrorFor(x => x.CodigoPostalDestino);
    }

    [Fact]
    public void CotizarEnvio_DimensionesDemasiadoLargo_DeberiaFallar()
    {
        var validator = new CotizarEnvioDtoValidator();
        var result = validator.TestValidate(new CotizarEnvioDto
        {
            Peso = 5m,
            CodigoPostalOrigen = "28001",
            CodigoPostalDestino = "08001",
            Dimensiones = new string('x', 51)
        });
        result.ShouldHaveValidationErrorFor(x => x.Dimensiones);
    }

    // ─── CrearEnvioDtoValidator ───────────────────────────────────────────────
    private static CrearEnvioDto EnvioValido() => new()
    {
        Peso = 2m,
        Dimensiones = "30x20x10",
        NombreRemitente = "Juan Pérez",
        Origen = "C/ Mayor 1",
        CodigoPostalOrigen = "28001",
        NombreDestinatario = "Ana López",
        Destino = "C/ Sol 2",
        CodigoPostalDestino = "08001",
        OficinaOrigenId = 1,
        TipoEntrega = "Domicilio"
    };

    [Fact]
    public void CrearEnvio_DatosValidosDomicilio_DeberiaPasar()
    {
        var validator = new CrearEnvioDtoValidator();
        var result = validator.TestValidate(EnvioValido());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CrearEnvio_TipoEntregaOficinaSinOficinaDestino_DeberiaFallar()
    {
        var validator = new CrearEnvioDtoValidator();
        var dto = EnvioValido();
        dto.TipoEntrega = "Oficina";
        dto.OficinaDestinoId = null;
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.OficinaDestinoId);
    }

    [Fact]
    public void CrearEnvio_TipoEntregaOficinaConOficinaDestino_DeberiaPasar()
    {
        var validator = new CrearEnvioDtoValidator();
        var dto = EnvioValido();
        dto.TipoEntrega = "Oficina";
        dto.OficinaDestinoId = 7;
        var result = validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CrearEnvio_TipoEntregaInvalido_DeberiaFallar()
    {
        var validator = new CrearEnvioDtoValidator();
        var dto = EnvioValido();
        dto.TipoEntrega = "Drone";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.TipoEntrega);
    }

    [Fact]
    public void CrearEnvio_OficinaOrigenIdCero_DeberiaFallar()
    {
        var validator = new CrearEnvioDtoValidator();
        var dto = EnvioValido();
        dto.OficinaOrigenId = 0;
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.OficinaOrigenId);
    }

    [Fact]
    public void CrearEnvio_NombreRemitenteVacio_DeberiaFallar()
    {
        var validator = new CrearEnvioDtoValidator();
        var dto = EnvioValido();
        dto.NombreRemitente = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NombreRemitente);
    }

    [Fact]
    public void CrearEnvio_DestinoVacio_DeberiaFallar()
    {
        var validator = new CrearEnvioDtoValidator();
        var dto = EnvioValido();
        dto.Destino = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Destino);
    }

    [Fact]
    public void CrearEnvio_ObservacionesDemasiadoLargo_DeberiaFallar()
    {
        var validator = new CrearEnvioDtoValidator();
        var dto = EnvioValido();
        dto.Observaciones = new string('x', 1001);
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Observaciones);
    }

    // ─── ActualizarPerfilDtoValidator ─────────────────────────────────────────
    [Fact]
    public void ActualizarPerfil_DatosValidos_DeberiaPasar()
    {
        var validator = new ActualizarPerfilDtoValidator();
        var result = validator.TestValidate(new ActualizarPerfilDto
        {
            DNI = "12345678A",
            Telefono = "600000000",
            DireccionPredeterminada = "C/ Mayor 1"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ActualizarPerfil_DniDemasiadoLargo_DeberiaFallar()
    {
        var validator = new ActualizarPerfilDtoValidator();
        var result = validator.TestValidate(new ActualizarPerfilDto
        {
            DNI = new string('1', 16)
        });
        result.ShouldHaveValidationErrorFor(x => x.DNI);
    }

    [Fact]
    public void ActualizarPerfil_TelefonoDemasiadoLargo_DeberiaFallar()
    {
        var validator = new ActualizarPerfilDtoValidator();
        var result = validator.TestValidate(new ActualizarPerfilDto
        {
            Telefono = new string('1', 21)
        });
        result.ShouldHaveValidationErrorFor(x => x.Telefono);
    }

    [Fact]
    public void ActualizarPerfil_DireccionDemasiadoLarga_DeberiaFallar()
    {
        var validator = new ActualizarPerfilDtoValidator();
        var result = validator.TestValidate(new ActualizarPerfilDto
        {
            DireccionPredeterminada = new string('x', 501)
        });
        result.ShouldHaveValidationErrorFor(x => x.DireccionPredeterminada);
    }

    // ─── CrearDireccionFavoritaDtoValidator ───────────────────────────────────
    private static CrearDireccionFavoritaDto DireccionValida() => new()
    {
        Alias = "Casa",
        NombreDestinatario = "Ana López",
        Direccion = "C/ Sol 2",
        CodigoPostal = "08001",
        Ciudad = "Barcelona",
        Provincia = "Barcelona"
    };

    [Fact]
    public void CrearDireccion_DatosValidos_DeberiaPasar()
    {
        var validator = new CrearDireccionFavoritaDtoValidator();
        var result = validator.TestValidate(DireccionValida());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CrearDireccion_AliasVacio_DeberiaFallar()
    {
        var validator = new CrearDireccionFavoritaDtoValidator();
        var dto = DireccionValida();
        dto.Alias = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Alias);
    }

    [Fact]
    public void CrearDireccion_CodigoPostalInvalido_DeberiaFallar()
    {
        var validator = new CrearDireccionFavoritaDtoValidator();
        var dto = DireccionValida();
        dto.CodigoPostal = "ABCDE";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CodigoPostal);
    }

    [Fact]
    public void CrearDireccion_CiudadVacia_DeberiaFallar()
    {
        var validator = new CrearDireccionFavoritaDtoValidator();
        var dto = DireccionValida();
        dto.Ciudad = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Ciudad);
    }

    [Fact]
    public void CrearDireccion_ProvinciaVacia_DeberiaFallar()
    {
        var validator = new CrearDireccionFavoritaDtoValidator();
        var dto = DireccionValida();
        dto.Provincia = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Provincia);
    }

    [Fact]
    public void CrearDireccion_NombreDestinatarioVacio_DeberiaFallar()
    {
        var validator = new CrearDireccionFavoritaDtoValidator();
        var dto = DireccionValida();
        dto.NombreDestinatario = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NombreDestinatario);
    }

    [Fact]
    public void CrearDireccion_DireccionVacia_DeberiaFallar()
    {
        var validator = new CrearDireccionFavoritaDtoValidator();
        var dto = DireccionValida();
        dto.Direccion = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Direccion);
    }
}
