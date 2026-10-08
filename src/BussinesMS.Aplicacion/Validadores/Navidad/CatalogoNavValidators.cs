using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CopiarCatalogoDtoValidator : AbstractValidator<CopiarCatalogoDto>
{
    public CopiarCatalogoDtoValidator()
    {
        RuleFor(x => x.TemporadaOrigenId)
            .GreaterThan(0).WithMessage("La temporada origen es obligatoria");
    }
}
