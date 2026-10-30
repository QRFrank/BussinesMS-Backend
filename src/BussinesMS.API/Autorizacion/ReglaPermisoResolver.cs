using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;

namespace BussinesMS.API.Autorizacion;

/// <summary>
/// Permisos fase 2: resuelve la regla de un endpoint a partir de sus atributos.
/// Orden: [AllowAnonymous] (acción o controller) → atributo de la acción → atributo del controller.
/// Un endpoint sin atributo queda SinMapear (falla cerrada).
/// </summary>
public static class ReglaPermisoResolver
{
    public static ReglaPermiso Resolver(ControllerActionDescriptor accion, string metodoHttp)
    {
        if (TieneAtributo<IAllowAnonymous>(accion.MethodInfo) || TieneAtributo<IAllowAnonymous>(accion.ControllerTypeInfo))
            return new ReglaPermiso(TipoReglaPermiso.Anonimo, Array.Empty<int>(), FlagMenu.PorVerbo);

        return ResolverEn(accion.MethodInfo, metodoHttp)
               ?? ResolverEn(accion.ControllerTypeInfo, metodoHttp)
               ?? ReglaPermiso.SinMapear;
    }

    /// <summary>Verbos HTTP declarados por la acción (para el arranque y la documentación).</summary>
    public static IReadOnlyList<string> Verbos(ControllerActionDescriptor accion)
        => accion.EndpointMetadata.OfType<HttpMethodMetadata>().SelectMany(m => m.HttpMethods).Distinct().ToList();

    public static FlagMenu FlagPorVerbo(string metodoHttp) => metodoHttp.ToUpperInvariant() switch
    {
        "GET" or "HEAD" or "OPTIONS" => FlagMenu.Leer,
        "POST" => FlagMenu.Crear,
        "PUT" or "PATCH" => FlagMenu.Editar,
        "DELETE" => FlagMenu.Eliminar,
        _ => FlagMenu.Editar
    };

    private static ReglaPermiso? ResolverEn(MemberInfo miembro, string metodoHttp)
    {
        // Si hubiera más de uno, gana el más restrictivo: SoloAdmin > Menu > SoloSesion
        if (miembro.GetCustomAttribute<SoloAdminAttribute>(true) != null)
            return new ReglaPermiso(TipoReglaPermiso.SoloAdmin, Array.Empty<int>(), FlagMenu.PorVerbo);

        var recepciones = miembro.GetCustomAttribute<RequierePuedeHacerRecepcionesAttribute>(true) != null;

        var menu = miembro.GetCustomAttribute<MenuAttribute>(true);
        if (menu != null)
        {
            var flag = menu.Flag == FlagMenu.PorVerbo ? FlagPorVerbo(metodoHttp) : menu.Flag;
            return new ReglaPermiso(TipoReglaPermiso.Menu, menu.MenuIds, flag, recepciones);
        }

        if (recepciones)
            return new ReglaPermiso(TipoReglaPermiso.PuedeHacerRecepciones, Array.Empty<int>(), FlagMenu.PorVerbo);

        if (miembro.GetCustomAttribute<SoloSesionAttribute>(true) != null)
            return new ReglaPermiso(TipoReglaPermiso.SoloSesion, Array.Empty<int>(), FlagMenu.PorVerbo);

        return null;
    }

    private static bool TieneAtributo<T>(MemberInfo miembro) where T : class
        => miembro.GetCustomAttributes(true).OfType<T>().Any();
}
