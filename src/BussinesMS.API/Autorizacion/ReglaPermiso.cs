namespace BussinesMS.API.Autorizacion;

public enum TipoReglaPermiso
{
    SinMapear = 0,
    Anonimo,
    SoloSesion,
    SoloAdmin,
    Menu,
    PuedeHacerRecepciones
}

/// <summary>
/// Regla de permisos resuelta para un endpoint y un verbo HTTP.
/// <paramref name="OPuedeHacerRecepciones"/>: en una regla Menu, también pasa quien tiene el permiso "Puede hacer recepciones".
/// </summary>
public record ReglaPermiso(TipoReglaPermiso Tipo, int[] Menus, FlagMenu Flag, bool OPuedeHacerRecepciones = false)
{
    public static readonly ReglaPermiso SinMapear = new(TipoReglaPermiso.SinMapear, Array.Empty<int>(), FlagMenu.PorVerbo);

    public string Descripcion => Tipo switch
    {
        TipoReglaPermiso.Anonimo => "Anónimo",
        TipoReglaPermiso.SoloSesion => "Solo sesión",
        TipoReglaPermiso.SoloAdmin => "Solo admin",
        TipoReglaPermiso.Menu => $"{Flag} en {string.Join(" o ", Menus)}" + (OPuedeHacerRecepciones ? " o Puede hacer recepciones" : ""),
        TipoReglaPermiso.PuedeHacerRecepciones => "Puede hacer recepciones",
        _ => "SIN MAPEAR (denegado en Bloqueo)"
    };
}
