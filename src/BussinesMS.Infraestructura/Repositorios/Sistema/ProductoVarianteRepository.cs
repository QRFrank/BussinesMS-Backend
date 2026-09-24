using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class ProductoVarianteRepository : IProductoVarianteRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProductoVarianteRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<ProductoVariante> AsQueryable()
        => _context.ProductoVariantes.AsQueryable();

    public async Task<List<ProductoVariante>> ObtenerTodosAsync()
        => await _context.ProductoVariantes
            .Where(x => x.IsActive)
            .Include(x => x.Producto)
                .ThenInclude(p => p.Fabricante)
            .Include(x => x.Sabor)
            .Include(x => x.Tamanio)
            .OrderBy(x => x.Producto!.Nombre)
            .ThenBy(x => x.Sabor!.Nombre)
            .ThenBy(x => x.Tamanio!.Nombre)
            .ToListAsync();

    public async Task<ProductoVariante?> ObtenerPorIdAsync(int id)
        => await _context.ProductoVariantes
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<ProductoVariante?> ObtenerConDetallesAsync(int id)
        => await _context.ProductoVariantes
            .Include(x => x.Producto)
                .ThenInclude(p => p.Categoria)
            .Include(x => x.Producto)
                .ThenInclude(p => p.Fabricante)
            .Include(x => x.Sabor)
            .Include(x => x.Tamanio)
            .Include(x => x.Presentaciones.Where(p => p.IsActive))
                .ThenInclude(p => p.TipoPresentacion)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<List<ProductoVariante>> ObtenerActivosAsync()
        => await _context.ProductoVariantes
            .Where(x => x.IsActive)
            .Include(x => x.Producto)
                .ThenInclude(p => p.Fabricante)
            .Include(x => x.Sabor)
            .Include(x => x.Tamanio)
            .OrderBy(x => x.Producto!.Nombre)
            .ThenBy(x => x.Sabor!.Nombre)
            .ThenBy(x => x.Tamanio!.Nombre)
            .ToListAsync();

    public async Task<bool> ExisteCombinacionAsync(int productoId, int saborId, int tamanioId, int? excludeId = null)
    {
        var query = _context.ProductoVariantes
            .Where(x => x.ProductoId == productoId
                && x.SaborId == saborId
                && x.TamanioId == tamanioId
                && x.IsActive);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<bool> ExisteCodigoBarrasAsync(string? codigoBarras, int? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(codigoBarras))
            return false;

        var query = _context.ProductoVariantes
            .Where(x => x.CodigoBarras == codigoBarras && x.IsActive);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<ProductoVariante?> ObtenerPorCodigoBarrasAsync(string codigoBarras)
        => await _context.ProductoVariantes
            .Include(x => x.Producto)
                .ThenInclude(p => p.Fabricante)
            .Include(x => x.Sabor)
            .Include(x => x.Tamanio)
            .FirstOrDefaultAsync(x => x.CodigoBarras == codigoBarras && x.IsActive);

    public async Task<ProductoVariante> CrearAsync(ProductoVariante entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.ProductoVariantes.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<ProductoVariante> ActualizarAsync(ProductoVariante entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.ProductoVariantes.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.ProductoVariantes.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.ProductoVariantes.Update(entidad);
        await _context.SaveChangesAsync();
    }
}