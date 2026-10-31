using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class EnviarPruebaWhatsAppDtoValidator : AbstractValidator<EnviarPruebaWhatsAppDto>
{
    public EnviarPruebaWhatsAppDtoValidator()
    {
        RuleFor(x => x.Telefono)
            .NotEmpty().WithMessage("El teléfono es obligatorio")
            .MaximumLength(30).WithMessage("El teléfono no puede superar 30 caracteres");
    }
}
