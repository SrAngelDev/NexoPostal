using FluentValidation;
using Nexopostal.Reparto.DTOs;

namespace Nexopostal.Reparto.Validators;

/// <summary>
/// Valida el alta de un repartidor antes de asignarlo a una oficina y a un tipo de vehículo.
/// </summary>
public class CrearRepartidorDtoValidator : AbstractValidator<CrearRepartidorDto>
{
    public CrearRepartidorDtoValidator()
    {
        RuleFor(x => x.IdentityUserId).NotEmpty();
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CodigoEmpleado).NotEmpty().MaximumLength(20);
        RuleFor(x => x.OficinaJsonId).GreaterThan(0);
        RuleFor(x => x.OficinaNombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TipoVehiculo).NotEmpty();
        RuleFor(x => x.Telefono).MaximumLength(20);
    }
}

/// <summary>
/// Revisa la edición de la ficha de un repartidor sin tocar su identidad base.
/// TipoVehiculo es opcional cuando se envía VehiculoId (el servicio lo toma del vehículo de flota).
/// </summary>
public class EditarRepartidorDtoValidator : AbstractValidator<EditarRepartidorDto>
{
    public EditarRepartidorDtoValidator()
    {
        RuleFor(x => x.NombreCompleto).NotEmpty().MaximumLength(200);
        RuleFor(x => x.OficinaJsonId).GreaterThan(0);
        RuleFor(x => x.OficinaNombre).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TipoVehiculo)
            .NotEmpty()
            .When(x => !x.VehiculoId.HasValue);
        RuleFor(x => x.Telefono).MaximumLength(20);
    }
}

/// <summary>
/// Comprueba que la creación de una ruta lleve repartidor, fecha y oficina de salida válidos.
/// </summary>
public class CrearRutaRepartoDtoValidator : AbstractValidator<CrearRutaRepartoDto>
{
    public CrearRutaRepartoDtoValidator()
    {
        RuleFor(x => x.RepartidorId).GreaterThan(0);
        RuleFor(x => x.FechaReparto)
            .NotEmpty()
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("Formato: yyyy-MM-dd");
        RuleFor(x => x.OficinaOrigenJsonId).GreaterThan(0);
        RuleFor(x => x.OficinaOrigenNombre).NotEmpty();
        RuleFor(x => x.Observaciones).MaximumLength(500);
    }
}

/// <summary>
/// Valida el alta de un vehículo de flota antes de persistirlo o asignarlo.
/// </summary>
public class CrearVehiculoDtoValidator : AbstractValidator<CrearVehiculoDto>
{
    public CrearVehiculoDtoValidator()
    {
        RuleFor(x => x.Matricula).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Marca).MaximumLength(60);
        RuleFor(x => x.Modelo).MaximumLength(60);
        RuleFor(x => x.Color).MaximumLength(40);
        RuleFor(x => x.Notas).MaximumLength(500);
    }
}
