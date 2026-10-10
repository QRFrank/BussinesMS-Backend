using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IProductoNavRepository : IRepositorio<Producto>
{
    // Incluye proveedor y categoría
    Task<Producto?> ObtenerConDetalleAsync(int id);
    // Descripción única por proveedor entre activos (sin distinguir mayúsculas)
    Task<bool> ExisteDescripcionAsync(int proveedorId, string descripcion, int? excluirId = null);
    // Alias (Nombre) único por temporada entre activos (sin distinguir mayúsculas)
    Task<bool> ExisteNombreEnTemporadaAsync(int temporadaId, string nombre, int? excluirId = null);
    // Sin tracking (copia de catálogo): productos activos de proveedores activos
    Task<List<Producto>> ObtenerActivosPorTemporadaAsync(int temporadaId);
    Task<bool> TieneProductosActivosPorCategoriaAsync(int categoriaId);
    // Activa o desactiva con auditoría: al desactivar pone DeletedAt/DeletedBy; al activar los limpia; siempre UpdatedAt/UpdatedBy
    Task CambiarEstadoAsync(Producto entidad, bool isActive);
}
