using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearCategoriaGastoNavDtoValidator : AbstractValidator<CrearCategoriaGastoNavDto>
{
    public CrearCategoriaGastoNavDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");
    }
}

public class ActualizarCategoriaGastoNavDtoValidator : AbstractValidator<ActualizarCategoriaGastoNavDto>
{
    public ActualizarCategoriaGastoNavDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");
    }
}
