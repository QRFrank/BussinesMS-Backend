using BussinesMS.Aplicacion.DTOs.Sistema;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Sistema;

public class CrearCategoriaGastoDtoValidator : AbstractValidator<CrearCategoriaGastoDto>
{
    public CrearCategoriaGastoDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");
    }
}

public class ActualizarCategoriaGastoDtoValidator : AbstractValidator<ActualizarCategoriaGastoDto>
{
    public ActualizarCategoriaGastoDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");
    }
}
