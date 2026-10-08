using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class GuardarPresentacionNavDtoValidator : AbstractValidator<GuardarPresentacionNavDto>
{
    public GuardarPresentacionNavDtoValidator()
    {
        RuleFor(x => x.Unidades)
            .GreaterThanOrEqualTo(1).WithMessage("Las unidades de cada presentación deben ser al menos 1");
        RuleFor(x => x.PrecioUnitario)
            .GreaterThan(0).WithMessage("El precio unitario de cada presentación debe ser mayor a 0");
        RuleFor(x => x.Nombre)
            .MaximumLength(50).WithMessage("El nombre de la presentación no puede superar 50 caracteres");
        // Solo la presentación de 1 unidad puede venir sin nombre (se guarda "Unidad")
        RuleFor(x => x.Nombre)
            .NotEmpty().When(x => x.Unidades != 1)
            .WithMessage(x => $"La presentación de {x.Unidades} unidades debe tener nombre");
    }
}

public class CrearProductoNavDtoValidator : AbstractValidator<CrearProductoNavDto>
{
    public CrearProductoNavDtoValidator()
    {
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El proveedor es obligatorio");
        RuleFor(x => x.CategoriaProductoId)
            .GreaterThan(0).WithMessage("La categoría es obligatoria");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres");
        RuleFor(x => x.PrecioCompraUnidad)
            .GreaterThan(0).WithMessage("El precio de compra por unidad debe ser mayor a 0");
        RuleFor(x => x.PrecioCatalogo)
            .GreaterThan(0).WithMessage("El precio de catálogo debe ser mayor a 0");
        RuleFor(x => x.Presentaciones)
            .NotEmpty().WithMessage("Debe registrar al menos una presentación");
        RuleForEach(x => x.Presentaciones)
            .SetValidator(new GuardarPresentacionNavDtoValidator());
    }
}

public class ActualizarProductoNavDtoValidator : AbstractValidator<ActualizarProductoNavDto>
{
    public ActualizarProductoNavDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El proveedor es obligatorio");
        RuleFor(x => x.CategoriaProductoId)
            .GreaterThan(0).WithMessage("La categoría es obligatoria");
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio")
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres");
        RuleFor(x => x.PrecioCompraUnidad)
            .GreaterThan(0).WithMessage("El precio de compra por unidad debe ser mayor a 0");
        RuleFor(x => x.PrecioCatalogo)
            .GreaterThan(0).WithMessage("El precio de catálogo debe ser mayor a 0");
        RuleFor(x => x.Presentaciones)
            .NotEmpty().WithMessage("Debe registrar al menos una presentación");
        RuleForEach(x => x.Presentaciones)
            .SetValidator(new GuardarPresentacionNavDtoValidator());
    }
}
