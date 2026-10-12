using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearTrasladoNavDtoValidator : AbstractValidator<CrearTrasladoNavDto>
{
    public CrearTrasladoNavDtoValidator()
    {
        RuleFor(x => x.AlmacenOrigenId)
            .GreaterThan(0).WithMessage("El almacén de origen es obligatorio");
        RuleFor(x => x.AlmacenDestinoId)
            .GreaterThan(0).WithMessage("El almacén de destino es obligatorio");
        RuleFor(x => x)
            .Must(x => x.AlmacenOrigenId != x.AlmacenDestinoId)
            .When(x => x.AlmacenOrigenId > 0 && x.AlmacenDestinoId > 0)
            .WithMessage("El almacén de origen y el de destino deben ser distintos");
        RuleFor(x => x.Motivo)
            .MaximumLength(500).WithMessage("El motivo no puede superar 500 caracteres");
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("Debe registrar al menos un producto");
        RuleFor(x => x.Detalles)
            .Must(d => d == null || d.Select(i => i.ProductoId).Distinct().Count() == d.Count)
            .WithMessage("Hay productos repetidos en el traslado");
        RuleForEach(x => x.Detalles)
            .SetValidator(new CrearTrasladoDetalleNavDtoValidator());
    }
}

public class CrearTrasladoDetalleNavDtoValidator : AbstractValidator<CrearTrasladoDetalleNavDto>
{
    public CrearTrasladoDetalleNavDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es obligatorio");
        RuleFor(x => x.CantidadUnidades)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");
    }
}
