using BussinesMS.Aplicacion.DTOs.Auth;

namespace BussinesMS.Aplicacion.Interfaces.Auth;

// Permisos fase 2: flags del usuario autenticado, consultados por el filtro de autorización global.
public interface IPermisoUsuarioService
{
    /// <summary>True si el usuario actual tiene el rol Admin.</summary>
    Task<bool> EsAdminAsync();

    /// <summary>menuId -> flags del usuario autenticado (vacío si no hay sesión).</summary>
    Task<IReadOnlyDictionary<int, FlagsMenuDto>> ObtenerFlagsAsync();

    /// <summary>True si el usuario actual tiene "Puede hacer recepciones" en el sistema Navideño (el admin no se evalúa acá).</summary>
    Task<bool> PuedeHacerRecepcionesAsync();

    /// <summary>menuId -> nombre del menú; ids desconocidos devuelven "menú {id}".</summary>
    Task<IReadOnlyDictionary<int, string>> ObtenerNombresMenusAsync(IEnumerable<int> menuIds);
}
