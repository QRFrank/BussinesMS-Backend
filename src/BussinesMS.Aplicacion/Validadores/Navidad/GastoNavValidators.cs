using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearGastoNavDtoValidator : AbstractValidator<CrearGastoNavDto>
{
    public CrearGastoNavDtoValidator()
    {
        RuleFor(x => x.CategoriaId)
            .GreaterThan(0).WithMessage("La categoría es obligatoria");
        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria")
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres");
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");
    }
}

public class ActualizarGastoNavDtoValidator : AbstractValidator<ActualizarGastoNavDto>
{
    public ActualizarGastoNavDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.CategoriaId)
            .GreaterThan(0).WithMessage("La categoría es obligatoria");
        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria")
            .MaximumLength(500).WithMessage("La descripción no puede superar 500 caracteres");
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");
    }
}
