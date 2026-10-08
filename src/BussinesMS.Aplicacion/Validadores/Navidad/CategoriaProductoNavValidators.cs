using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearCategoriaProductoNavDtoValidator : AbstractValidator<CrearCategoriaProductoNavDto>
{
    public CrearCategoriaProductoNavDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");
    }
}

public class ActualizarCategoriaProductoNavDtoValidator : AbstractValidator<ActualizarCategoriaProductoNavDto>
{
    public ActualizarCategoriaProductoNavDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");
    }
}
