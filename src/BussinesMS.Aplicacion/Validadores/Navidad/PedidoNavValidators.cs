using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearPedidoNavDtoValidator : AbstractValidator<CrearPedidoNavDto>
{
    public CrearPedidoNavDtoValidator()
    {
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El proveedor es obligatorio");
        RuleFor(x => x.Fecha)
            .NotEmpty().WithMessage("La fecha es obligatoria");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
        RuleFor(x => x.MontoTotalProveedor)
            .GreaterThanOrEqualTo(0).When(x => x.MontoTotalProveedor.HasValue)
            .WithMessage("El monto total del proveedor no puede ser negativo");
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("Debe registrar al menos un producto");
        RuleFor(x => x.Detalles)
            .Must(d => d == null || d.Select(i => i.ProductoId).Distinct().Count() == d.Count)
            .WithMessage("Hay productos repetidos en el pedido");
        RuleForEach(x => x.Detalles)
            .SetValidator(new CrearPedidoDetalleNavDtoValidator());
    }
}

public class ActualizarPedidoNavDtoValidator : AbstractValidator<ActualizarPedidoNavDto>
{
    public ActualizarPedidoNavDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El ID es obligatorio");
        Include(new CrearPedidoNavDtoValidator());
    }
}

public class CrearPedidoDetalleNavDtoValidator : AbstractValidator<CrearPedidoDetalleNavDto>
{
    public CrearPedidoDetalleNavDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es obligatorio");
        RuleFor(x => x.CantidadUnidades)
            .GreaterThanOrEqualTo(0).WithMessage("La cantidad no puede ser negativa");
        RuleFor(x => x.PrecioCompraUnidad)
            .GreaterThanOrEqualTo(0).When(x => x.PrecioCompraUnidad.HasValue)
            .WithMessage("El precio de compra no puede ser negativo");
    }
}
