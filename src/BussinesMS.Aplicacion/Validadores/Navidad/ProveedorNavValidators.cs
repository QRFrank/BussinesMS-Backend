using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearProveedorNavDtoValidator : AbstractValidator<CrearProveedorNavDto>
{
    public CrearProveedorNavDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres");
        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono no puede superar 50 caracteres");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}

public class ActualizarProveedorNavDtoValidator : AbstractValidator<ActualizarProveedorNavDto>
{
    public ActualizarProveedorNavDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres");
        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono no puede superar 50 caracteres");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}
