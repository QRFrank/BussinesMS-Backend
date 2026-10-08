using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearAporteCapitalDtoValidator : AbstractValidator<CrearAporteCapitalDto>
{
    public CrearAporteCapitalDtoValidator()
    {
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");
        RuleFor(x => x.PorcentajeComision)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje de comisión debe estar entre 0 y 100");
        RuleFor(x => x.PorcentajeComision)
            .Equal(0).When(x => x.InversorId == null)
            .WithMessage("El capital propio va con PorcentajeComision = 0");
        RuleFor(x => x.InversorId)
            .GreaterThan(0).When(x => x.InversorId.HasValue).WithMessage("El inversor es inválido");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}

public class ActualizarAporteCapitalDtoValidator : AbstractValidator<ActualizarAporteCapitalDto>
{
    public ActualizarAporteCapitalDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto debe ser mayor a 0");
        RuleFor(x => x.PorcentajeComision)
            .InclusiveBetween(0, 100).WithMessage("El porcentaje de comisión debe estar entre 0 y 100");
        RuleFor(x => x.PorcentajeComision)
            .Equal(0).When(x => x.InversorId == null)
            .WithMessage("El capital propio va con PorcentajeComision = 0");
        RuleFor(x => x.InversorId)
            .GreaterThan(0).When(x => x.InversorId.HasValue).WithMessage("El inversor es inválido");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}
