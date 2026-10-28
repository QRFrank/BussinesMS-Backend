using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class VentaClienteRutaLineaDtoValidator : AbstractValidator<VentaClienteRutaLineaDto>
{
    public VentaClienteRutaLineaDtoValidator()
    {
        RuleFor(x => x.ProductoId).GreaterThan(0).WithMessage("El producto es obligatorio");
        RuleFor(x => x.Unidades).GreaterThan(0).WithMessage("Las unidades deben ser mayores a 0");
        RuleFor(x => x.PrecioUnitario).GreaterThanOrEqualTo(0).WithMessage("El precio no puede ser negativo");
    }
}

public class RegistrarVentaClienteRutaDtoValidator : AbstractValidator<RegistrarVentaClienteRutaDto>
{
    public RegistrarVentaClienteRutaDtoValidator()
    {
        RuleFor(x => x.ClienteId).GreaterThan(0).WithMessage("El cliente es obligatorio");
        RuleFor(x => x.Observacion).MaximumLength(500).WithMessage("La observación no puede superar 500 caracteres");
        RuleFor(x => x.PagadoAhora).GreaterThanOrEqualTo(0).WithMessage("Lo pagado no puede ser negativo");
        RuleFor(x => x.Medio).IsInEnum().When(x => x.Medio.HasValue).WithMessage("El medio de pago es inválido");
        RuleFor(x => x.Medio).NotNull().When(x => x.PagadoAhora > 0).WithMessage("El medio de pago es obligatorio si hay un pago");
        RuleFor(x => x.Detalles).NotEmpty().WithMessage("Debe registrar al menos un producto");
        RuleForEach(x => x.Detalles).SetValidator(new VentaClienteRutaLineaDtoValidator());
    }
}

public class RegistrarCobroClienteRutaDtoValidator : AbstractValidator<RegistrarCobroClienteRutaDto>
{
    public RegistrarCobroClienteRutaDtoValidator()
    {
        RuleFor(x => x.Monto).GreaterThan(0).WithMessage("El monto debe ser mayor a 0");
        RuleFor(x => x.Medio).IsInEnum().WithMessage("El medio de pago es inválido");
    }
}
