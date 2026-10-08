using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearCodigoClienteDtoValidator : AbstractValidator<CrearCodigoClienteDto>
{
    public CrearCodigoClienteDtoValidator()
    {
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El proveedor es obligatorio");
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es obligatorio")
            .MaximumLength(50).WithMessage("El código no puede superar 50 caracteres");
        RuleFor(x => x.Titular)
            .NotEmpty().WithMessage("El titular es obligatorio")
            .MaximumLength(150).WithMessage("El titular no puede superar 150 caracteres");
    }
}

public class ActualizarCodigoClienteDtoValidator : AbstractValidator<ActualizarCodigoClienteDto>
{
    public ActualizarCodigoClienteDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("El código es obligatorio")
            .MaximumLength(50).WithMessage("El código no puede superar 50 caracteres");
        RuleFor(x => x.Titular)
            .NotEmpty().WithMessage("El titular es obligatorio")
            .MaximumLength(150).WithMessage("El titular no puede superar 150 caracteres");
    }
}
