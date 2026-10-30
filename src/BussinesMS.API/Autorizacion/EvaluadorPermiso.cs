using BussinesMS.Aplicacion.DTOs.Auth;

namespace BussinesMS.API.Autorizacion;

/// <summary>Decisión pura de permisos (sin BD): la usa PermisoMenuFilter y se puede probar simulando flags.</summary>
public static class EvaluadorPermiso
{
    public static bool Permitido(ReglaPermiso regla, bool autenticado, bool esAdmin, IReadOnlyDictionary<int, FlagsMenuDto> flags,
        bool puedeHacerRecepciones = false)
    {
        if (regla.Tipo == TipoReglaPermiso.Anonimo) return true;
        if (!autenticado) return false;
        if (regla.Tipo == TipoReglaPermiso.SoloSesion) return true;
        if (esAdmin) return true;
        if (regla.Tipo == TipoReglaPermiso.PuedeHacerRecepciones) return puedeHacerRecepciones;
        if (regla.Tipo != TipoReglaPermiso.Menu) return false; // SoloAdmin o SinMapear
        if (regla.OPuedeHacerRecepciones && puedeHacerRecepciones) return true;
        return regla.Menus.Any(id => flags.TryGetValue(id, out var f) && TieneFlag(f, regla.Flag));
    }

    public static bool TieneFlag(FlagsMenuDto f, FlagMenu flag) => flag switch
    {
        FlagMenu.Leer => f.Leer,
        FlagMenu.Crear => f.Crear,
        FlagMenu.Editar => f.Editar,
        FlagMenu.Eliminar => f.Eliminar,
        _ => false
    };
}
