using FluentValidation;
using Nexopostal.Intranet.DTOs;

namespace Nexopostal.Intranet.Validators;

public class ScanRequestDtoValidator : AbstractValidator<ScanRequestDto>
{
    public ScanRequestDtoValidator()
    {
        RuleFor(x => x.CodigoEscaneado)
            .NotEmpty().WithMessage("Código escaneado obligatorio")
            .MaximumLength(40);

        RuleFor(x => x.ModoOperacion)
            .NotEmpty().WithMessage("Modo de operación obligatorio")
            .Must(ModosEscaneo.EsValido).WithMessage("Modo de escaneo no válido");
    }
}

public class CrearAsignacionDtoValidator : AbstractValidator<CrearAsignacionDto>
{
    private static readonly string[] TiposValidos =
    {
        "Recepcion", "Clasificacion", "CargaTransporte", "DescargaTransporte", "Expedicion"
    };

    public CrearAsignacionDtoValidator()
    {
        RuleFor(x => x.NumeroExpedicion).NotEmpty().MaximumLength(20);
        RuleFor(x => x.OperarioAsignadoId).GreaterThan(0);
        RuleFor(x => x.TipoTarea)
            .NotEmpty()
            .Must(t => TiposValidos.Contains(t))
            .WithMessage("TipoTarea no válido");
        RuleFor(x => x.Observaciones).MaximumLength(500);
    }
}
