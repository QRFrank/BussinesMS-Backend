using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class CompraRepository : ICompraRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CompraRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<Compra> AsQueryable()
        => _context.Compras.AsQueryable();

    public async Task<List<Compra>> ObtenerTodosAsync()
        => await _context.Compras
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.FechaCompra)
            .ToListAsync();

    public async Task<Compra?> ObtenerPorIdAsync(int id)
        => await _context.Compras
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Compra?> ObtenerConDetallesAsync(int id)
        => await _context.Compras
            .Include(x => x.Proveedor)
            .Include(x => x.Detalles).ThenInclude(d => d.Variante)
                .ThenInclude(v => v!.Producto).ThenInclude(p => p!.Fabricante)
            .Include(x => x.Detalles).ThenInclude(d => d.Variante).ThenInclude(v => v!.Sabor)
            .Include(x => x.Detalles).ThenInclude(d => d.Variante).ThenInclude(v => v!.Tamanio)
            .Include(x => x.Pagos)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<List<Compra>> ObtenerActivasAsync()
        => await _context.Compras
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.FechaCompra)
            .ToListAsync();

    public async Task<Compra> CrearAsync(Compra entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.UsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.Compras.Add(entidad);
        await _context.SaveChangesAsync();
        await _context.Entry(entidad).Collection(e => e.Detalles).LoadAsync();
        return entidad;
    }

    public async Task<Compra> CrearSinGuardarAsync(Compra entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.UsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.Compras.Add(entidad);
        return entidad;
    }

    public async Task<Compra> ActualizarAsync(Compra entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.Compras.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.Compras.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.Compras.Update(entidad);
        await _context.SaveChangesAsync();
    }
}
