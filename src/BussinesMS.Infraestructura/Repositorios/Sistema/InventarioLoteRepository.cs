using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class InventarioLoteRepository : IInventarioLoteRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public InventarioLoteRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<InventarioLote> AsQueryable()
        => _context.InventarioLotes.AsQueryable();

    public async Task<List<InventarioLote>> ObtenerTodosAsync()
        => await _context.InventarioLotes
            .Where(x => x.IsActive)
            .ToListAsync();

    public async Task<InventarioLote?> ObtenerPorIdAsync(int id)
        => await _context.InventarioLotes
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<InventarioLote?> ObtenerConDetallesAsync(int id)
        => await _context.InventarioLotes
            .Include(x => x.Variante)
                .ThenInclude(v => v!.Producto)
                    .ThenInclude(p => p!.Categoria)
            .Include(x => x.Variante!.Producto!.Fabricante)
            .Include(x => x.Variante!.Sabor)
            .Include(x => x.Variante!.Tamanio)
            .Include(x => x.Variante!.Presentaciones)
                .ThenInclude(p => p.TipoPresentacion)
            .Include(x => x.CompraDetalle)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<InventarioLote> CrearAsync(InventarioLote entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.InventarioLotes.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<InventarioLote> CrearSinGuardarAsync(InventarioLote entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.InventarioLotes.Add(entidad);
        return entidad;
    }

    public async Task<InventarioLote> ActualizarAsync(InventarioLote entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.InventarioLotes.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.InventarioLotes.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.InventarioLotes.Update(entidad);
        await _context.SaveChangesAsync();
    }
}
