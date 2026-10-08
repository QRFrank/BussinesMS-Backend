using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearPagoInversorDtoValidator : AbstractValidator<CrearPagoInversorDto>
{
    public CrearPagoInversorDtoValidator()
    {
        RuleFor(x => x.AporteCapitalId)
            .GreaterThan(0).WithMessage("El aporte de capital es obligatorio");
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");
        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de pago es inválido");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}
