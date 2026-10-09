using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearRecepcionNavDtoValidator : AbstractValidator<CrearRecepcionNavDto>
{
    public CrearRecepcionNavDtoValidator()
    {
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El proveedor es obligatorio");
        RuleFor(x => x.Fecha)
            .NotEmpty().WithMessage("La fecha es obligatoria");
        RuleFor(x => x.NroFactura)
            .MaximumLength(50).WithMessage("El número de factura no puede superar 50 caracteres");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("Debe registrar al menos un producto");
        RuleFor(x => x.Detalles)
            .Must(d => d == null || d.Select(i => i.ProductoId).Distinct().Count() == d.Count)
            .WithMessage("Hay productos repetidos en la recepción");
        RuleForEach(x => x.Detalles)
            .SetValidator(new CrearRecepcionDetalleNavDtoValidator());
    }
}

public class CrearRecepcionDetalleNavDtoValidator : AbstractValidator<CrearRecepcionDetalleNavDto>
{
    public CrearRecepcionDetalleNavDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es obligatorio");
        RuleFor(x => x.Distribucion)
            .NotEmpty().WithMessage("Cada producto debe distribuirse al menos a un almacén");
        RuleFor(x => x.Distribucion)
            .Must(d => d == null || d.Select(i => i.AlmacenId).Distinct().Count() == d.Count)
            .WithMessage("Hay almacenes repetidos en la distribución de un producto");
        RuleForEach(x => x.Distribucion)
            .SetValidator(new CrearRecepcionDistribucionNavDtoValidator());
    }
}

public class CrearRecepcionDistribucionNavDtoValidator : AbstractValidator<CrearRecepcionDistribucionNavDto>
{
    public CrearRecepcionDistribucionNavDtoValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0).WithMessage("El almacén es obligatorio");
        RuleFor(x => x.CantidadUnidades)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");
    }
}
