using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class ProveedorNavRepository : NavidadRepositorioBase<Proveedor>, IProveedorNavRepository
{
    public ProveedorNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<Proveedor?> ObtenerConCodigosAsync(int id)
        => await _dbSet
            .Include(p => p.Codigos.Where(c => c.IsActive).OrderBy(c => c.Codigo))
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<bool> ExisteNombreAsync(int temporadaId, string nombre, int? excluirId = null)
    {
        var n = nombre.Trim().ToLower();
        return await _dbSet.AnyAsync(p => p.TemporadaId == temporadaId
            && p.IsActive
            && p.Nombre.ToLower() == n
            && (!excluirId.HasValue || p.Id != excluirId.Value));
    }

    public async Task<bool> TieneCodigosActivosAsync(int proveedorId)
        => await _contexto.Set<CodigoCliente>().AnyAsync(c => c.ProveedorId == proveedorId && c.IsActive);

    public async Task<bool> TieneProductosActivosAsync(int proveedorId)
        => await _contexto.Set<Producto>().AnyAsync(p => p.ProveedorId == proveedorId && p.IsActive);

    public async Task<Dictionary<int, int>> ContarCodigosActivosAsync(IEnumerable<int> proveedorIds)
    {
        var ids = proveedorIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, int>();
        return await _contexto.Set<CodigoCliente>()
            .Where(c => c.IsActive && ids.Contains(c.ProveedorId))
            .GroupBy(c => c.ProveedorId)
            .Select(g => new { g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Cantidad);
    }

    public async Task<Dictionary<int, int>> ContarProductosActivosAsync(IEnumerable<int> proveedorIds)
    {
        var ids = proveedorIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, int>();
        return await _contexto.Set<Producto>()
            .Where(p => p.IsActive && ids.Contains(p.ProveedorId))
            .GroupBy(p => p.ProveedorId)
            .Select(g => new { g.Key, Cantidad = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Cantidad);
    }

    public async Task<bool> TemporadaTieneProveedoresActivosAsync(int temporadaId)
        => await _dbSet.AnyAsync(p => p.TemporadaId == temporadaId && p.IsActive);

    public async Task<List<Proveedor>> ObtenerActivosConCodigosPorTemporadaAsync(int temporadaId)
        => await _dbSet
            .AsNoTracking()
            .Include(p => p.Codigos.Where(c => c.IsActive).OrderBy(c => c.Codigo))
            .Where(p => p.TemporadaId == temporadaId && p.IsActive)
            .OrderBy(p => p.Nombre)
            .ToListAsync();
}
