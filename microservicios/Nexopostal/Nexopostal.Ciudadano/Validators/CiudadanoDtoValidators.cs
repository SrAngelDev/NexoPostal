using FluentValidation;
using Nexopostal.Ciudadano.DTOs;

namespace Nexopostal.Ciudadano.Validators;

/// <summary>
/// Comprueba los datos mínimos necesarios para calcular una tarifa pública.
/// </summary>
public class CotizarEnvioDtoValidator : AbstractValidator<CotizarEnvioDto>
{
    public CotizarEnvioDtoValidator()
    {
        RuleFor(x => x.Peso)
            .InclusiveBetween(0.1m, 30m).WithMessage("El peso debe estar entre 0.1 y 30 kg");

        RuleFor(x => x.CodigoPostalOrigen)
            .NotEmpty().WithMessage("Código postal de origen obligatorio")
            .Matches(@"^\d{5}$").WithMessage("Código postal de origen debe tener 5 dígitos");

        RuleFor(x => x.CodigoPostalDestino)
            .NotEmpty().WithMessage("Código postal de destino obligatorio")
            .Matches(@"^\d{5}$").WithMessage("Código postal de destino debe tener 5 dígitos");

        RuleFor(x => x.Dimensiones)
            .MaximumLength(50);
    }
}

/// <summary>
/// Valida la creación de un envío online antes de pasar al cálculo y al pago.
/// </summary>
public class CrearEnvioDtoValidator : AbstractValidator<CrearEnvioDto>
{
    private static readonly string[] TiposEntregaValidos = { "Domicilio", "Oficina" };

    public CrearEnvioDtoValidator()
    {
        RuleFor(x => x.Peso).InclusiveBetween(0.1m, 30m);
        RuleFor(x => x.Dimensiones).NotEmpty().MaximumLength(50);

        RuleFor(x => x.NombreRemitente).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Origen).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CodigoPostalOrigen).NotEmpty().Matches(@"^\d{5}$");

        RuleFor(x => x.NombreDestinatario).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Destino).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CodigoPostalDestino).NotEmpty().Matches(@"^\d{5}$");

        RuleFor(x => x.OficinaOrigenId).GreaterThan(0).WithMessage("Oficina de origen obligatoria");

        RuleFor(x => x.TipoEntrega)
            .NotEmpty()
            .Must(t => TiposEntregaValidos.Contains(t))
            .WithMessage("TipoEntrega debe ser 'Domicilio' o 'Oficina'");

        When(x => x.TipoEntrega == "Oficina", () =>
        {
            RuleFor(x => x.OficinaDestinoId)
                .NotNull().WithMessage("Oficina de destino obligatoria cuando TipoEntrega = 'Oficina'")
                .GreaterThan(0);
        });

        RuleFor(x => x.Observaciones).MaximumLength(1000);
    }
}

/// <summary>
/// Revisa los campos opcionales que un ciudadano puede completar en su perfil.
/// </summary>
public class ActualizarPerfilDtoValidator : AbstractValidator<ActualizarPerfilDto>
{
    public ActualizarPerfilDtoValidator()
    {
        RuleFor(x => x.DNI).MaximumLength(15);
        RuleFor(x => x.Telefono).MaximumLength(20);
        RuleFor(x => x.DireccionPredeterminada).MaximumLength(500);
    }
}

/// <summary>
/// Valida una dirección favorita antes de guardarla en la agenda del cliente.
/// </summary>
public class CrearDireccionFavoritaDtoValidator : AbstractValidator<CrearDireccionFavoritaDto>
{
    public CrearDireccionFavoritaDtoValidator()
    {
        RuleFor(x => x.Alias).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NombreDestinatario).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Direccion).NotEmpty().MaximumLength(500);
        RuleFor(x => x.CodigoPostal).NotEmpty().Matches(@"^\d{5}$");
        RuleFor(x => x.Ciudad).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Provincia).NotEmpty().MaximumLength(100);
    }
}
