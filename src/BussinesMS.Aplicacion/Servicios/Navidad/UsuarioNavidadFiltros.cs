using System.Linq.Expressions;
using BussinesMS.Dominio.Entidades.Auth;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Regla única de "usuario válido" para el sistema Navideño (AuthDB, solo lectura).
// La usan VendedorNavService y CatalogoNavService.
public static class UsuarioNavidadFiltros
{
    public const int SistemaNavidadId = 2;

    // Activo y con acceso al sistema Navideño (o sin sistemas asignados y con Navidad por defecto)
    public static readonly Expression<Func<Usuario, bool>> EsUsuarioValido = u =>
        u.IsActive &&
        (u.UsuarioSistemas.Any(s => s.SistemaId == SistemaNavidadId) ||
         (!u.UsuarioSistemas.Any() && u.SistemaIdDefault == SistemaNavidadId));

    public static IQueryable<Usuario> ValidosNavidad(this IQueryable<Usuario> query)
        => query.Where(EsUsuarioValido);
}
