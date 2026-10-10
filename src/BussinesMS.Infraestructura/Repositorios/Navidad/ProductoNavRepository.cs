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
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<bool> ExisteDescripcionAsync(int proveedorId, string descripcion, int? excluirId = null)
    {
        var d = descripcion.Trim().ToLower();
        return await _dbSet.AnyAsync(p => p.ProveedorId == proveedorId
            && p.IsActive
            && p.Descripcion.ToLower() == d
            && (!excluirId.HasValue || p.Id != excluirId.Value));
    }

    public async Task<bool> ExisteNombreEnTemporadaAsync(int temporadaId, string nombre, int? excluirId = null)
    {
        var n = nombre.Trim().ToLower();
        return await _dbSet.AnyAsync(p => p.TemporadaId == temporadaId
            && p.IsActive
            && p.Nombre != null
            && p.Nombre.ToLower() == n
            && (!excluirId.HasValue || p.Id != excluirId.Value));
    }

    public async Task<List<Producto>> ObtenerActivosPorTemporadaAsync(int temporadaId)
        => await _dbSet
            .AsNoTracking()
            .Where(p => p.TemporadaId == temporadaId
                && p.IsActive
                && p.Proveedor != null && p.Proveedor.IsActive)
            .OrderBy(p => p.Descripcion)
            .ToListAsync();

    public async Task CambiarEstadoAsync(Producto entidad, bool isActive)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        var ahora = DateTime.UtcNow;
        entidad.IsActive = isActive;
        entidad.UpdatedAt = ahora;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.DeletedAt = isActive ? null : ahora;
        entidad.DeletedByUsuarioId = isActive ? null : usuarioId;
        _dbSet.Update(entidad);
        await _contexto.SaveChangesAsync();
    }

    public async Task<bool> TieneProductosActivosPorCategoriaAsync(int categoriaId)
        => await _dbSet.AnyAsync(p => p.CategoriaProductoId == categoriaId && p.IsActive);
}
