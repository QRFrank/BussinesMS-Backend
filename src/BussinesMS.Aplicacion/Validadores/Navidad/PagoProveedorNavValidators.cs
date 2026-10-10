using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearPagoProveedorNavDtoValidator : AbstractValidator<CrearPagoProveedorNavDto>
{
    public CrearPagoProveedorNavDtoValidator()
    {
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El proveedor es obligatorio");
        RuleFor(x => x.Fecha)
            .NotEmpty().WithMessage("La fecha es obligatoria");
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");
        RuleFor(x => x.Medio)
            .IsInEnum().WithMessage("El medio de pago es inválido");
        RuleFor(x => x.Comprobante)
            .MaximumLength(100).WithMessage("El comprobante no puede superar 100 caracteres");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}

public class ActualizarPagoProveedorNavDtoValidator : AbstractValidator<ActualizarPagoProveedorNavDto>
{
    public ActualizarPagoProveedorNavDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.Fecha)
            .NotEmpty().WithMessage("La fecha es obligatoria");
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");
        RuleFor(x => x.Medio)
            .IsInEnum().WithMessage("El medio de pago es inválido");
        RuleFor(x => x.Comprobante)
            .MaximumLength(100).WithMessage("El comprobante no puede superar 100 caracteres");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}
