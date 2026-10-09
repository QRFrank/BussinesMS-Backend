using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearCompraNavDtoValidator : AbstractValidator<CrearCompraNavDto>
{
    public CrearCompraNavDtoValidator()
    {
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El proveedor es obligatorio");
        RuleFor(x => x.Fecha)
            .NotEmpty().WithMessage("La fecha es obligatoria");
        RuleFor(x => x.NroNota)
            .MaximumLength(50).WithMessage("El número de nota no puede superar 50 caracteres");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
        RuleFor(x => x.MedioPago)
            .NotNull().When(x => x.PagadaAlContado)
            .WithMessage("El medio de pago es obligatorio para una compra al contado");
        RuleFor(x => x.MedioPago)
            .IsInEnum().When(x => x.MedioPago.HasValue)
            .WithMessage("El medio de pago es inválido");
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("Debe registrar al menos un producto");
        RuleFor(x => x.Detalles)
            .Must(d => d == null || d.Select(i => i.ProductoId).Distinct().Count() == d.Count)
            .WithMessage("Hay productos repetidos en la compra");
        RuleForEach(x => x.Detalles)
            .SetValidator(new CrearCompraDetalleNavDtoValidator());
    }
}

public class CrearCompraDetalleNavDtoValidator : AbstractValidator<CrearCompraDetalleNavDto>
{
    public CrearCompraDetalleNavDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es obligatorio");
        RuleFor(x => x.PrecioCompraUnidad)
            .GreaterThan(0).WithMessage("El precio de compra debe ser mayor a 0");
        RuleFor(x => x.Distribucion)
            .NotEmpty().WithMessage("Cada producto debe distribuirse al menos a un almacén");
        RuleFor(x => x.Distribucion)
            .Must(d => d == null || d.Select(i => i.AlmacenId).Distinct().Count() == d.Count)
            .WithMessage("Hay almacenes repetidos en la distribución de un producto");
        RuleForEach(x => x.Distribucion)
            .SetValidator(new CrearCompraDistribucionNavDtoValidator());
    }
}

public class CrearCompraDistribucionNavDtoValidator : AbstractValidator<CrearCompraDistribucionNavDto>
{
    public CrearCompraDistribucionNavDtoValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0).WithMessage("El almacén es obligatorio");
        RuleFor(x => x.CantidadUnidades)
            .GreaterThan(0).WithMessage("La cantidad debe ser mayor a 0");
    }
}
