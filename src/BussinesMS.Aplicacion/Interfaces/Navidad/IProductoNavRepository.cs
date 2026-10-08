using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IProductoNavRepository : IRepositorio<Producto>
{
    // Incluye proveedor, categoría y solo las presentaciones activas ordenadas por Unidades
    Task<Producto?> ObtenerConDetalleAsync(int id);
    Task<bool> ExisteNombreAsync(int proveedorId, string nombre, int? excluirId = null);
    // Sin tracking (copia de catálogo): productos activos de proveedores activos, con presentaciones activas
    Task<List<Producto>> ObtenerActivosConPresentacionesPorTemporadaAsync(int temporadaId);
    Task<bool> TieneProductosActivosPorCategoriaAsync(int categoriaId);
}
