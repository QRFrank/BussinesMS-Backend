using BussinesMS.Aplicacion.DTOs.Sistema;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Sistema;

public class CrearClienteDtoValidator : AbstractValidator<CrearClienteDto>
{
    public CrearClienteDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres");

        RuleFor(x => x.NumeroCarnet)
            .MaximumLength(20).WithMessage("El número de carnet no puede superar 20 caracteres");

        RuleFor(x => x.Telefono)
            .MaximumLength(20).WithMessage("El teléfono no puede superar 20 caracteres");
    }
}

public class ActualizarClienteDtoValidator : AbstractValidator<ActualizarClienteDto>
{
    public ActualizarClienteDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres");

        RuleFor(x => x.NumeroCarnet)
            .MaximumLength(20).WithMessage("El número de carnet no puede superar 20 caracteres");

        RuleFor(x => x.Telefono)
            .MaximumLength(20).WithMessage("El teléfono no puede superar 20 caracteres");
    }
}
