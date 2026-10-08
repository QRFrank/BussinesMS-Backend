using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IProveedorNavRepository : IRepositorio<Proveedor>
{
    // Incluye solo los códigos activos ordenados por Codigo
    Task<Proveedor?> ObtenerConCodigosAsync(int id);
    Task<bool> ExisteNombreAsync(int temporadaId, string nombre, int? excluirId = null);
    Task<bool> TieneCodigosActivosAsync(int proveedorId);
    Task<bool> TieneProductosActivosAsync(int proveedorId);
    Task<Dictionary<int, int>> ContarCodigosActivosAsync(IEnumerable<int> proveedorIds);
    Task<Dictionary<int, int>> ContarProductosActivosAsync(IEnumerable<int> proveedorIds);
    Task<bool> TemporadaTieneProveedoresActivosAsync(int temporadaId);
    // Sin tracking (copia de catálogo): proveedores activos con sus códigos activos
    Task<List<Proveedor>> ObtenerActivosConCodigosPorTemporadaAsync(int temporadaId);
}
