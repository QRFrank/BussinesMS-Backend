using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class ProductoRepository : IProductoRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProductoRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<Producto> AsQueryable()
        => _context.Productos.AsQueryable();

    public async Task<List<Producto>> ObtenerTodosAsync()
        => await _context.Productos
            .Where(x => x.IsActive)
            .Include(x => x.Categoria)
            .Include(x => x.Fabricante)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

    public async Task<Producto?> ObtenerPorIdAsync(int id)
        => await _context.Productos
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Producto?> ObtenerConDetallesAsync(int id)
        => await _context.Productos
            .Include(x => x.Categoria)
            .Include(x => x.Fabricante)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<List<Producto>> ObtenerActivosAsync()
        => await _context.Productos
            .Where(x => x.IsActive)
            .Include(x => x.Categoria)
            .Include(x => x.Fabricante)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

    public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null)
    {
        var query = _context.Productos
            .Where(x => x.Nombre.ToLower() == nombre.ToLower() && x.IsActive);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<Producto> CrearAsync(Producto entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.Productos.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<Producto> ActualizarAsync(Producto entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.Productos.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.Productos.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.Productos.Update(entidad);
        await _context.SaveChangesAsync();
    }
}