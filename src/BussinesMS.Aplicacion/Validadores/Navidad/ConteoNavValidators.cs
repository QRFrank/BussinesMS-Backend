using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearConteoNavDtoValidator : AbstractValidator<CrearConteoNavDto>
{
    public CrearConteoNavDtoValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0).WithMessage("El almacén es obligatorio");
        RuleFor(x => x.Tipo)
            .Must(t => Enum.IsDefined(typeof(TipoConteoInventario), t)).WithMessage("El tipo de conteo es inválido");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("Debe registrar al menos un producto");
        RuleFor(x => x.Detalles)
            .Must(d => d == null || d.Select(i => i.ProductoId).Distinct().Count() == d.Count)
            .WithMessage("Hay productos repetidos en el conteo");
        RuleForEach(x => x.Detalles)
            .SetValidator(new CrearConteoDetalleNavDtoValidator());
    }
}

public class CrearConteoDetalleNavDtoValidator : AbstractValidator<CrearConteoDetalleNavDto>
{
    public CrearConteoDetalleNavDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es obligatorio");
        RuleFor(x => x.Contado)
            .GreaterThanOrEqualTo(0).WithMessage("La cantidad contada no puede ser negativa");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}
