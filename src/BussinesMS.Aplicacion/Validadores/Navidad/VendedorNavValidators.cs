using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using FluentValidation;

namespace BussinesMS.Aplicacion.Validadores.Navidad;

public class CrearVendedorNavDtoValidator : AbstractValidator<CrearVendedorNavDto>
{
    public CrearVendedorNavDtoValidator()
    {
        RuleFor(x => x.UsuarioId)
            .GreaterThan(0).WithMessage("El usuario es obligatorio");
        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de vendedor no es válido");
        RuleFor(x => x.SueldoMensual)
            .NotNull().WithMessage("El sueldo mensual es obligatorio para vendedores de tienda")
            .GreaterThan(0).WithMessage("El sueldo mensual debe ser mayor a 0")
            .When(x => x.Tipo == TipoVendedor.Tienda);
    }
}

public class ActualizarVendedorNavDtoValidator : AbstractValidator<ActualizarVendedorNavDto>
{
    public ActualizarVendedorNavDtoValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("El Id es obligatorio");
        RuleFor(x => x.UsuarioId)
            .GreaterThan(0).WithMessage("El usuario es obligatorio");
        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de vendedor no es válido");
        RuleFor(x => x.SueldoMensual)
            .NotNull().WithMessage("El sueldo mensual es obligatorio para vendedores de tienda")
            .GreaterThan(0).WithMessage("El sueldo mensual debe ser mayor a 0")
            .When(x => x.Tipo == TipoVendedor.Tienda);
    }
}
