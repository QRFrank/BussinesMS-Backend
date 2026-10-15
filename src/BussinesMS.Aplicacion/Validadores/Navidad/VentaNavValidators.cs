using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class VentaLineaDtoValidator : AbstractValidator<VentaLineaDto>
{
    public VentaLineaDtoValidator()
    {
        RuleFor(x => x.ProductoId)
            .GreaterThan(0).WithMessage("El producto es obligatorio");
        RuleFor(x => x.Unidades)
            .GreaterThan(0).WithMessage("Las unidades deben ser mayores a 0");
        RuleFor(x => x.PrecioUnitario)
            .GreaterThanOrEqualTo(0).WithMessage("El precio unitario no puede ser negativo");
    }
}

public class CrearVentaNavDtoValidator : AbstractValidator<CrearVentaNavDto>
{
    public CrearVentaNavDtoValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0).WithMessage("La tienda es obligatoria");
        RuleFor(x => x.Tipo)
            .Must(t => Enum.IsDefined(typeof(TipoVentaNav), t)).WithMessage("El tipo de venta es inválido");
        RuleFor(x => x.CondicionPago)
            .Must(c => Enum.IsDefined(typeof(CondicionPagoVentaNav), c)).WithMessage("La condición de pago es inválida");
        RuleFor(x => x.Descuento)
            .GreaterThanOrEqualTo(0).WithMessage("El descuento no puede ser negativo");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("Debe registrar al menos un producto");
        RuleFor(x => x.Detalles)
            .Must(d => d == null || d.Select(i => i.ProductoId).Distinct().Count() == d.Count)
            .WithMessage("Hay productos repetidos en la venta");
        RuleForEach(x => x.Detalles)
            .SetValidator(new VentaLineaDtoValidator());
        RuleFor(x => x.Cobro!.Comprobante)
            .MaximumLength(100).WithMessage("El comprobante no puede superar 100 caracteres")
            .When(x => x.Cobro != null);
    }
}

public class EditarVentaNavDtoValidator : AbstractValidator<EditarVentaNavDto>
{
    public EditarVentaNavDtoValidator()
    {
        RuleFor(x => x.Descuento)
            .GreaterThanOrEqualTo(0).WithMessage("El descuento no puede ser negativo");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("Debe registrar al menos un producto");
        RuleFor(x => x.Detalles)
            .Must(d => d == null || d.Select(i => i.ProductoId).Distinct().Count() == d.Count)
            .WithMessage("Hay productos repetidos en la venta");
        RuleForEach(x => x.Detalles)
            .SetValidator(new VentaLineaDtoValidator());
    }
}

public class RegistrarEntregaVentaDtoValidator : AbstractValidator<RegistrarEntregaVentaDto>
{
    public RegistrarEntregaVentaDtoValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0).WithMessage("El almacén es obligatorio");
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("Debe indicar al menos una línea a entregar");
        RuleForEach(x => x.Detalles).ChildRules(l =>
        {
            l.RuleFor(i => i.VentaDetalleId).GreaterThan(0).WithMessage("La línea es obligatoria");
            l.RuleFor(i => i.Unidades).GreaterThan(0).WithMessage("Las unidades a entregar deben ser mayores a 0");
        });
    }
}

public class RegistrarCobroVentaDtoValidator : AbstractValidator<RegistrarCobroVentaDto>
{
    public RegistrarCobroVentaDtoValidator()
    {
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("El monto del cobro debe ser mayor a 0");
        RuleFor(x => x.Medio)
            .IsInEnum().WithMessage("El medio de pago es inválido");
        RuleFor(x => x.Comprobante)
            .MaximumLength(100).WithMessage("El comprobante no puede superar 100 caracteres");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
    }
}

public class CrearConsumoNavDtoValidator : AbstractValidator<CrearConsumoNavDto>
{
    public CrearConsumoNavDtoValidator()
    {
        RuleFor(x => x.AlmacenId)
            .GreaterThan(0).WithMessage("El almacén es obligatorio");
        RuleFor(x => x.Motivo)
            .Must(m => Enum.IsDefined(typeof(MotivoConsumoNav), m)).WithMessage("El motivo del consumo es inválido");
        RuleFor(x => x.Observacion)
            .MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
        RuleFor(x => x.Detalles)
            .NotEmpty().WithMessage("Debe registrar al menos un producto");
        RuleFor(x => x.Detalles)
            .Must(d => d == null || d.Select(i => i.ProductoId).Distinct().Count() == d.Count)
            .WithMessage("Hay productos repetidos en el consumo");
        RuleForEach(x => x.Detalles).ChildRules(l =>
        {
            l.RuleFor(i => i.ProductoId).GreaterThan(0).WithMessage("El producto es obligatorio");
            l.RuleFor(i => i.Unidades).GreaterThan(0).WithMessage("Las unidades deben ser mayores a 0");
        });
    }
}
