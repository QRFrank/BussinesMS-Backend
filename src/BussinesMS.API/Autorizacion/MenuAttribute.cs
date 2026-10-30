namespace BussinesMS.API.Autorizacion;

/// <summary>
/// Permisos fase 2: el usuario necesita el flag (por verbo, o <see cref="Flag"/>) en CUALQUIERA de estos menús.
/// En la acción manda sobre lo que diga el controller. El Admin pasa siempre.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class MenuAttribute : Attribute
{
    public int[] MenuIds { get; }

    /// <summary>Override del flag para acciones especiales (ej. toda anulación exige Eliminar).</summary>
    public FlagMenu Flag { get; set; } = FlagMenu.PorVerbo;

    public MenuAttribute(params int[] menuIds)
    {
        if (menuIds == null || menuIds.Length == 0)
            throw new ArgumentException("[Menu] necesita al menos un id de menú", nameof(menuIds));
        MenuIds = menuIds;
    }
}
