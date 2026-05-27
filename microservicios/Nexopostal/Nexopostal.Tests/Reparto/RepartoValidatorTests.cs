using FluentValidation.TestHelper;
using Nexopostal.Reparto.DTOs;
using Nexopostal.Reparto.Models;
using Nexopostal.Reparto.Validators;
using Xunit;

namespace Nexopostal.Tests.Reparto;

/// <summary>
/// Tests para los validators de FluentValidation del módulo Reparto.
/// </summary>
public class RepartoValidatorTests
{
    // ─── CrearRepartidorDtoValidator ──────────────────────────────────────────
    private static CrearRepartidorDto RepartidorValido() => new()
    {
        IdentityUserId = "user-1",
        NombreCompleto = "Pepe Repartidor",
        CodigoEmpleado = "EMP-001",
        OficinaJsonId = 7,
        OficinaNombre = "Oficina Madrid",
        TipoVehiculo = "Furgoneta",
        Telefono = "600000000"
    };

    [Fact]
    public void CrearRepartidor_DatosValidos_DeberiaPasar()
    {
        var validator = new CrearRepartidorDtoValidator();
        var result = validator.TestValidate(RepartidorValido());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CrearRepartidor_IdentityUserIdVacio_DeberiaFallar()
    {
        var validator = new CrearRepartidorDtoValidator();
        var dto = RepartidorValido();
        dto.IdentityUserId = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.IdentityUserId);
    }

    [Fact]
    public void CrearRepartidor_NombreVacio_DeberiaFallar()
    {
        var validator = new CrearRepartidorDtoValidator();
        var dto = RepartidorValido();
        dto.NombreCompleto = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NombreCompleto);
    }

    [Fact]
    public void CrearRepartidor_CodigoEmpleadoVacio_DeberiaFallar()
    {
        var validator = new CrearRepartidorDtoValidator();
        var dto = RepartidorValido();
        dto.CodigoEmpleado = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.CodigoEmpleado);
    }

    [Fact]
    public void CrearRepartidor_OficinaJsonIdCero_DeberiaFallar()
    {
        var validator = new CrearRepartidorDtoValidator();
        var dto = RepartidorValido();
        dto.OficinaJsonId = 0;
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.OficinaJsonId);
    }

    [Fact]
    public void CrearRepartidor_OficinaNombreVacio_DeberiaFallar()
    {
        var validator = new CrearRepartidorDtoValidator();
        var dto = RepartidorValido();
        dto.OficinaNombre = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.OficinaNombre);
    }

    [Fact]
    public void CrearRepartidor_TipoVehiculoVacio_DeberiaFallar()
    {
        var validator = new CrearRepartidorDtoValidator();
        var dto = RepartidorValido();
        dto.TipoVehiculo = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.TipoVehiculo);
    }

    [Fact]
    public void CrearRepartidor_TelefonoDemasiadoLargo_DeberiaFallar()
    {
        var validator = new CrearRepartidorDtoValidator();
        var dto = RepartidorValido();
        dto.Telefono = new string('1', 21);
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Telefono);
    }

    // ─── EditarRepartidorDtoValidator ─────────────────────────────────────────
    [Fact]
    public void EditarRepartidor_DatosValidos_DeberiaPasar()
    {
        var validator = new EditarRepartidorDtoValidator();
        var result = validator.TestValidate(new EditarRepartidorDto
        {
            NombreCompleto = "Pepe Cambiado",
            OficinaJsonId = 1,
            OficinaNombre = "Oficina X",
            TipoVehiculo = "Moto"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void EditarRepartidor_NombreVacio_DeberiaFallar()
    {
        var validator = new EditarRepartidorDtoValidator();
        var result = validator.TestValidate(new EditarRepartidorDto
        {
            NombreCompleto = "",
            OficinaJsonId = 1,
            OficinaNombre = "Of",
            TipoVehiculo = "Moto"
        });
        result.ShouldHaveValidationErrorFor(x => x.NombreCompleto);
    }

    [Fact]
    public void EditarRepartidor_OficinaJsonIdCero_DeberiaFallar()
    {
        var validator = new EditarRepartidorDtoValidator();
        var result = validator.TestValidate(new EditarRepartidorDto
        {
            NombreCompleto = "Pepe",
            OficinaJsonId = 0,
            OficinaNombre = "Of",
            TipoVehiculo = "Moto"
        });
        result.ShouldHaveValidationErrorFor(x => x.OficinaJsonId);
    }

    [Fact]
    public void EditarRepartidor_TipoVehiculoVacio_DeberiaFallar()
    {
        var validator = new EditarRepartidorDtoValidator();
        var result = validator.TestValidate(new EditarRepartidorDto
        {
            NombreCompleto = "Pepe",
            OficinaJsonId = 1,
            OficinaNombre = "Of",
            TipoVehiculo = ""
        });
        result.ShouldHaveValidationErrorFor(x => x.TipoVehiculo);
    }

    // ─── CrearRutaRepartoDtoValidator ─────────────────────────────────────────
    [Fact]
    public void CrearRuta_DatosValidos_DeberiaPasar()
    {
        var validator = new CrearRutaRepartoDtoValidator();
        var result = validator.TestValidate(new CrearRutaRepartoDto
        {
            RepartidorId = 1,
            FechaReparto = "2026-05-26",
            OficinaOrigenJsonId = 1,
            OficinaOrigenNombre = "Madrid Central"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("2026/05/26")]
    [InlineData("26-05-2026")]
    [InlineData("hoy")]
    public void CrearRuta_FechaInvalida_DeberiaFallar(string fecha)
    {
        var validator = new CrearRutaRepartoDtoValidator();
        var result = validator.TestValidate(new CrearRutaRepartoDto
        {
            RepartidorId = 1,
            FechaReparto = fecha,
            OficinaOrigenJsonId = 1,
            OficinaOrigenNombre = "Madrid"
        });
        result.ShouldHaveValidationErrorFor(x => x.FechaReparto);
    }

    [Fact]
    public void CrearRuta_RepartidorIdCero_DeberiaFallar()
    {
        var validator = new CrearRutaRepartoDtoValidator();
        var result = validator.TestValidate(new CrearRutaRepartoDto
        {
            RepartidorId = 0,
            FechaReparto = "2026-05-26",
            OficinaOrigenJsonId = 1,
            OficinaOrigenNombre = "Madrid"
        });
        result.ShouldHaveValidationErrorFor(x => x.RepartidorId);
    }

    [Fact]
    public void CrearRuta_OficinaOrigenJsonIdCero_DeberiaFallar()
    {
        var validator = new CrearRutaRepartoDtoValidator();
        var result = validator.TestValidate(new CrearRutaRepartoDto
        {
            RepartidorId = 1,
            FechaReparto = "2026-05-26",
            OficinaOrigenJsonId = 0,
            OficinaOrigenNombre = "Madrid"
        });
        result.ShouldHaveValidationErrorFor(x => x.OficinaOrigenJsonId);
    }

    [Fact]
    public void CrearRuta_ObservacionesDemasiadoLargas_DeberiaFallar()
    {
        var validator = new CrearRutaRepartoDtoValidator();
        var result = validator.TestValidate(new CrearRutaRepartoDto
        {
            RepartidorId = 1,
            FechaReparto = "2026-05-26",
            OficinaOrigenJsonId = 1,
            OficinaOrigenNombre = "Madrid",
            Observaciones = new string('x', 501)
        });
        result.ShouldHaveValidationErrorFor(x => x.Observaciones);
    }

    // ─── CrearVehiculoDtoValidator ────────────────────────────────────────────
    [Fact]
    public void CrearVehiculo_DatosValidos_DeberiaPasar()
    {
        var validator = new CrearVehiculoDtoValidator();
        var result = validator.TestValidate(new CrearVehiculoDto
        {
            Matricula = "1234ABC",
            Tipo = TipoVehiculo.Furgoneta,
            Marca = "Ford",
            Modelo = "Transit",
            Color = "Blanco"
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CrearVehiculo_MatriculaVacia_DeberiaFallar()
    {
        var validator = new CrearVehiculoDtoValidator();
        var result = validator.TestValidate(new CrearVehiculoDto
        {
            Matricula = "",
            Tipo = TipoVehiculo.Furgoneta
        });
        result.ShouldHaveValidationErrorFor(x => x.Matricula);
    }

    [Fact]
    public void CrearVehiculo_MatriculaDemasiadoLarga_DeberiaFallar()
    {
        var validator = new CrearVehiculoDtoValidator();
        var result = validator.TestValidate(new CrearVehiculoDto
        {
            Matricula = new string('A', 21),
            Tipo = TipoVehiculo.Furgoneta
        });
        result.ShouldHaveValidationErrorFor(x => x.Matricula);
    }

    [Fact]
    public void CrearVehiculo_MarcaDemasiadoLarga_DeberiaFallar()
    {
        var validator = new CrearVehiculoDtoValidator();
        var result = validator.TestValidate(new CrearVehiculoDto
        {
            Matricula = "1234ABC",
            Tipo = TipoVehiculo.Furgoneta,
            Marca = new string('M', 61)
        });
        result.ShouldHaveValidationErrorFor(x => x.Marca);
    }

    [Fact]
    public void CrearVehiculo_NotasDemasiadoLargas_DeberiaFallar()
    {
        var validator = new CrearVehiculoDtoValidator();
        var result = validator.TestValidate(new CrearVehiculoDto
        {
            Matricula = "1234ABC",
            Tipo = TipoVehiculo.Furgoneta,
            Notas = new string('x', 501)
        });
        result.ShouldHaveValidationErrorFor(x => x.Notas);
    }
}
