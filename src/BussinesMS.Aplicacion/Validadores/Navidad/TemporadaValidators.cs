using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearTemporadaDtoValidator : AbstractValidator<CrearTemporadaDto>
{
    public CrearTemporadaDtoValidator()
    {
        RuleFor(x => x.Anio)
            .InclusiveBetween(2000, 2100).WithMessage("El año debe estar entre 2000 y 2100");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");

        RuleForEach(x => x.AlmacenesConConteoIds)
            .GreaterThan(0).WithMessage("Los almacenes con conteo deben tener un Id válido")
            .When(x => x.AlmacenesConConteoIds != null);
    }
}

public class ActualizarTemporadaDtoValidator : AbstractValidator<ActualizarTemporadaDto>
{
    public ActualizarTemporadaDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");

        RuleFor(x => x.Anio)
            .InclusiveBetween(2000, 2100).WithMessage("El año debe estar entre 2000 y 2100");

        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(100).WithMessage("El nombre no puede superar 100 caracteres");

        RuleForEach(x => x.AlmacenesConConteoIds)
            .GreaterThan(0).WithMessage("Los almacenes con conteo deben tener un Id válido")
            .When(x => x.AlmacenesConConteoIds != null);
    }
}
