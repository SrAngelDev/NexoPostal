using FluentValidation.TestHelper;
using Nexopostal.Intranet.DTOs;
using Nexopostal.Intranet.Validators;
using Xunit;

namespace Nexopostal.Tests.Intranet;

/// <summary>
/// Tests para los validators de FluentValidation del módulo Intranet.
/// </summary>
public class IntranetValidatorTests
{
    // ─── ScanRequestDtoValidator ──────────────────────────────────────────────
    [Fact]
    public void ScanRequest_DatosValidos_DeberiaPasar()
    {
        var validator = new ScanRequestDtoValidator();
        var result = validator.TestValidate(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-123",
            ModoOperacion = ModosEscaneo.RecepcionOficina
        });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ScanRequest_CodigoVacio_DeberiaFallar()
    {
        var validator = new ScanRequestDtoValidator();
        var result = validator.TestValidate(new ScanRequestDto
        {
            CodigoEscaneado = "",
            ModoOperacion = ModosEscaneo.RecepcionOficina
        });
        result.ShouldHaveValidationErrorFor(x => x.CodigoEscaneado);
    }

    [Fact]
    public void ScanRequest_CodigoDemasiadoLargo_DeberiaFallar()
    {
        var validator = new ScanRequestDtoValidator();
        var result = validator.TestValidate(new ScanRequestDto
        {
            CodigoEscaneado = new string('A', 41),
            ModoOperacion = ModosEscaneo.RecepcionOficina
        });
        result.ShouldHaveValidationErrorFor(x => x.CodigoEscaneado);
    }

    [Theory]
    [InlineData(ModosEscaneo.RecepcionOficina)]
    [InlineData(ModosEscaneo.RecepcionCta)]
    [InlineData(ModosEscaneo.Clasificacion)]
    [InlineData(ModosEscaneo.DespachoTroncal)]
    [InlineData(ModosEscaneo.RecepcionTroncal)]
    [InlineData(ModosEscaneo.EntregaOficinaDestino)]
    [InlineData(ModosEscaneo.SalidaAReparto)]
    [InlineData(ModosEscaneo.SalidaOficinaACta)]
    [InlineData(ModosEscaneo.DisponibleParaReparto)]
    public void ModosEscaneo_EsValido_ConTodosLosModos_DeberiaSerVerdadero(string modo)
    {
        Assert.True(ModosEscaneo.EsValido(modo));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Inexistente")]
    [InlineData("recepcionoficina")] // case-sensitive
    public void ModosEscaneo_EsValido_ConModosInvalidos_DeberiaSerFalso(string modo)
    {
        Assert.False(ModosEscaneo.EsValido(modo));
    }

    [Fact]
    public void ScanRequest_ModoOperacionVacio_DeberiaFallar()
    {
        var validator = new ScanRequestDtoValidator();
        var result = validator.TestValidate(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-1",
            ModoOperacion = ""
        });
        result.ShouldHaveValidationErrorFor(x => x.ModoOperacion);
    }

    [Fact]
    public void ScanRequest_ModoOperacionInvalido_DeberiaFallar()
    {
        var validator = new ScanRequestDtoValidator();
        var result = validator.TestValidate(new ScanRequestDto
        {
            CodigoEscaneado = "NXI-1",
            ModoOperacion = "ModoInexistente"
        });
        result.ShouldHaveValidationErrorFor(x => x.ModoOperacion);
    }

    // ─── CrearAsignacionDtoValidator ──────────────────────────────────────────
    private static CrearAsignacionDto AsignacionValida() => new()
    {
        NumeroExpedicion = "NXI-100",
        OperarioAsignadoId = 5,
        TipoTarea = "Clasificacion"
    };

    [Fact]
    public void CrearAsignacion_DatosValidos_DeberiaPasar()
    {
        var validator = new CrearAsignacionDtoValidator();
        var result = validator.TestValidate(AsignacionValida());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Recepcion")]
    [InlineData("Clasificacion")]
    [InlineData("CargaTransporte")]
    [InlineData("DescargaTransporte")]
    [InlineData("Expedicion")]
    public void CrearAsignacion_TodosLosTiposValidos_DeberianPasar(string tipo)
    {
        var validator = new CrearAsignacionDtoValidator();
        var dto = AsignacionValida();
        dto.TipoTarea = tipo;
        var result = validator.TestValidate(dto);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CrearAsignacion_NumeroExpedicionVacio_DeberiaFallar()
    {
        var validator = new CrearAsignacionDtoValidator();
        var dto = AsignacionValida();
        dto.NumeroExpedicion = "";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NumeroExpedicion);
    }

    [Fact]
    public void CrearAsignacion_NumeroExpedicionDemasiadoLargo_DeberiaFallar()
    {
        var validator = new CrearAsignacionDtoValidator();
        var dto = AsignacionValida();
        dto.NumeroExpedicion = new string('N', 21);
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.NumeroExpedicion);
    }

    [Fact]
    public void CrearAsignacion_OperarioIdCero_DeberiaFallar()
    {
        var validator = new CrearAsignacionDtoValidator();
        var dto = AsignacionValida();
        dto.OperarioAsignadoId = 0;
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.OperarioAsignadoId);
    }

    [Fact]
    public void CrearAsignacion_TipoTareaInvalido_DeberiaFallar()
    {
        var validator = new CrearAsignacionDtoValidator();
        var dto = AsignacionValida();
        dto.TipoTarea = "Inventado";
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.TipoTarea);
    }

    [Fact]
    public void CrearAsignacion_ObservacionesDemasiadoLargas_DeberiaFallar()
    {
        var validator = new CrearAsignacionDtoValidator();
        var dto = AsignacionValida();
        dto.Observaciones = new string('x', 501);
        var result = validator.TestValidate(dto);
        result.ShouldHaveValidationErrorFor(x => x.Observaciones);
    }
}
