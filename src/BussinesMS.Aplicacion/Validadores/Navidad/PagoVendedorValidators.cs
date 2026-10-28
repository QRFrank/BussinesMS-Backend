using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class RegistrarPagoVendedorDtoValidator : AbstractValidator<RegistrarPagoVendedorDto>
{
    public RegistrarPagoVendedorDtoValidator()
    {
        RuleFor(x => x.VendedorId).GreaterThan(0).WithMessage("El vendedor es obligatorio");
        RuleFor(x => x.Tipo).IsInEnum().WithMessage("El tipo de pago es inválido");
        RuleFor(x => x.Medio).IsInEnum().WithMessage("El medio de pago es inválido");
        RuleFor(x => x.Observacion).MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
        RuleFor(x => x.Modo).IsInEnum().When(x => x.Modo.HasValue).WithMessage("El modo de comisión es inválido");
        RuleForEach(x => x.Lineas).ChildRules(l =>
        {
            l.RuleFor(x => x.ProductoId).GreaterThan(0).WithMessage("El producto es obligatorio");
            l.RuleFor(x => x.ComisionUnidad).GreaterThanOrEqualTo(0).WithMessage("La comisión por unidad no puede ser negativa");
        });
    }
}
