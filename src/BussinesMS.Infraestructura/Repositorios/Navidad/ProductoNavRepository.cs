using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class ProductoNavRepository : NavidadRepositorioBase<Producto>, IProductoNavRepository
{
    public ProductoNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<Producto?> ObtenerConDetalleAsync(int id)
        => await _dbSet
            .Include(p => p.Proveedor)
            .Include(p => p.Categoria)
            .Include(p => p.Presentaciones.Where(x => x.IsActive).OrderBy(x => x.Unidades))
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<bool> ExisteNombreAsync(int proveedorId, string nombre, int? excluirId = null)
    {
        var n = nombre.Trim().ToLower();
        return await _dbSet.AnyAsync(p => p.ProveedorId == proveedorId
            && p.IsActive
            && p.Nombre.ToLower() == n
            && (!excluirId.HasValue || p.Id != excluirId.Value));
    }

    public async Task<List<Producto>> ObtenerActivosConPresentacionesPorTemporadaAsync(int temporadaId)
        => await _dbSet
            .AsNoTracking()
            .Include(p => p.Presentaciones.Where(x => x.IsActive).OrderBy(x => x.Unidades))
            .Where(p => p.TemporadaId == temporadaId
                && p.IsActive
                && p.Proveedor != null && p.Proveedor.IsActive)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

    public async Task<bool> TieneProductosActivosPorCategoriaAsync(int categoriaId)
        => await _dbSet.AnyAsync(p => p.CategoriaProductoId == categoriaId && p.IsActive);
}
