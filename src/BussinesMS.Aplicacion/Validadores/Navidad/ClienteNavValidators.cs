using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearClienteNavDtoValidator : AbstractValidator<CrearClienteNavDto>
{
    public CrearClienteNavDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres");
        RuleFor(x => x.Documento)
            .MaximumLength(50).WithMessage("El documento no puede superar 50 caracteres");
        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono no puede superar 50 caracteres");
        RuleFor(x => x.Direccion)
            .MaximumLength(250).WithMessage("La dirección no puede superar 250 caracteres");
    }
}

public class ActualizarClienteNavDtoValidator : AbstractValidator<ActualizarClienteNavDto>
{
    public ActualizarClienteNavDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres");
        RuleFor(x => x.Documento)
            .MaximumLength(50).WithMessage("El documento no puede superar 50 caracteres");
        RuleFor(x => x.Telefono)
            .MaximumLength(50).WithMessage("El teléfono no puede superar 50 caracteres");
        RuleFor(x => x.Direccion)
            .MaximumLength(250).WithMessage("La dirección no puede superar 250 caracteres");
    }
}
