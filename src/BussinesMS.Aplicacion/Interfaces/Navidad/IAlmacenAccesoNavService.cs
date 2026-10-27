namespace BussinesMS.Aplicacion.Interfaces.Navidad;

// Restricción por almacén del rol EncargadoAlmacen en el sistema Navideño.
// El admin y los demás roles no tienen restricción.
public interface IAlmacenAccesoNavService
{
    /// <summary>True si el usuario actual tiene el rol EncargadoAlmacen.</summary>
    Task<bool> EsEncargadoAsync();

    /// <summary>True si el usuario actual tiene el rol Admin.</summary>
    Task<bool> EsAdminAsync();

    /// <summary>Almacenes asignados al usuario actual en la temporada (vacío si no es encargado o no tiene).</summary>
    Task<List<int>> ObtenerAlmacenesACargoAsync(int temporadaId);

    /// <summary>
    /// Para filtrar lecturas: null si el usuario no está restringido (no es encargado);
    /// si es encargado, los almacenes que tiene a cargo (puede ser vacío).
    /// </summary>
    Task<List<int>?> ObtenerRestriccionAsync(int temporadaId);

    /// <summary>Si es encargado y el almacén no está a su cargo: 403 ALMACEN_NO_ASIGNADO. Otros roles: no hace nada.</summary>
    Task VerificarAlmacenAsync(int temporadaId, int almacenId);

    /// <summary>Igual que VerificarAlmacenAsync para varios almacenes (por ejemplo una distribución).</summary>
    Task VerificarAlmacenesAsync(int temporadaId, IEnumerable<int> almacenIds);
}
