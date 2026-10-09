using BussinesMS.Aplicacion.DTOs.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearProductoNavDtoValidator : AbstractValidator<CrearProductoNavDto>
{
    public CrearProductoNavDtoValidator()
    {
        RuleFor(x => x.ProveedorId)
            .GreaterThan(0).WithMessage("El proveedor es obligatorio");
        RuleFor(x => x.CategoriaProductoId)
            .GreaterThan(0).WithMessage("La categoría es obligatoria");
        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria")
            .MaximumLength(150).WithMessage("La descripción no puede superar 150 caracteres");
        RuleFor(x => x.Nombre)
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres")
            .When(x => x.Nombre != null);
        RuleFor(x => x.PrecioCompraUnidad)
            .GreaterThanOrEqualTo(0).WithMessage("El precio de compra por unidad no puede ser negativo");
        RuleFor(x => x.PrecioCatalogo)
            .GreaterThanOrEqualTo(0).WithMessage("El precio de catálogo no puede ser negativo");
        // Empaque: NombreEmpaque vacío o solo espacios cuenta como null
        RuleFor(x => x.UnidadesPorEmpaque)
            .Must((dto, u) => u.HasValue == !string.IsNullOrWhiteSpace(dto.NombreEmpaque))
            .WithMessage("Las unidades por empaque y el nombre del empaque van juntos");
        RuleFor(x => x.UnidadesPorEmpaque)
            .GreaterThan(1).WithMessage("Las unidades por empaque deben ser mayores a 1")
            .When(x => x.UnidadesPorEmpaque.HasValue);
        RuleFor(x => x.NombreEmpaque)
            .Must(n => n == null || n.Trim().Length <= 20)
            .WithMessage("El nombre del empaque no puede superar 20 caracteres");
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
        RuleFor(x => x.Descripcion)
            .NotEmpty().WithMessage("La descripción es obligatoria")
            .MaximumLength(150).WithMessage("La descripción no puede superar 150 caracteres");
        RuleFor(x => x.Nombre)
            .MaximumLength(150).WithMessage("El nombre no puede superar 150 caracteres")
            .When(x => x.Nombre != null);
        RuleFor(x => x.PrecioCompraUnidad)
            .GreaterThanOrEqualTo(0).WithMessage("El precio de compra por unidad no puede ser negativo");
        RuleFor(x => x.PrecioCatalogo)
            .GreaterThanOrEqualTo(0).WithMessage("El precio de catálogo no puede ser negativo");
        // Empaque: NombreEmpaque vacío o solo espacios cuenta como null
        RuleFor(x => x.UnidadesPorEmpaque)
            .Must((dto, u) => u.HasValue == !string.IsNullOrWhiteSpace(dto.NombreEmpaque))
            .WithMessage("Las unidades por empaque y el nombre del empaque van juntos");
        RuleFor(x => x.UnidadesPorEmpaque)
            .GreaterThan(1).WithMessage("Las unidades por empaque deben ser mayores a 1")
            .When(x => x.UnidadesPorEmpaque.HasValue);
        RuleFor(x => x.NombreEmpaque)
            .Must(n => n == null || n.Trim().Length <= 20)
            .WithMessage("El nombre del empaque no puede superar 20 caracteres");
    }
}
