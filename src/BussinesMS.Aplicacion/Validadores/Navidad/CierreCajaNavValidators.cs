using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class RegistrarCierreCajaNavDtoValidator : AbstractValidator<RegistrarCierreCajaNavDto>
{
    public RegistrarCierreCajaNavDtoValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0).WithMessage("La tienda es obligatoria");
        RuleFor(x => x.EfectivoEntregado)
            .GreaterThanOrEqualTo(0).WithMessage("El efectivo entregado no puede ser negativo");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}
