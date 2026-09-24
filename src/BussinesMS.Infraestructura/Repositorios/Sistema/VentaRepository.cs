using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class VentaRepository : IVentaRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public VentaRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<List<Venta>> ObtenerTodosAsync()
        => await _context.Ventas
            .Include(x => x.Cliente)
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.FechaVenta)
            .ToListAsync();

    public async Task<Venta?> ObtenerPorIdAsync(int id)
        => await _context.Ventas
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Venta?> ObtenerConDetallesAsync(int id)
        => await _context.Ventas
            .Include(x => x.SesionCaja)
            .Include(x => x.Cliente)
            .Include(x => x.Detalles)
                .ThenInclude(d => d.Variante)
                    .ThenInclude(v => v!.Producto)
            .Include(x => x.Detalles)
                .ThenInclude(d => d.Lote)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<Venta> CrearAsync(Venta entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.UsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.Ventas.Add(entidad);
        await _context.SaveChangesAsync();
        await _context.Entry(entidad).Collection(e => e.Detalles).LoadAsync();
        return entidad;
    }

    public async Task<Venta> CrearSinGuardarAsync(Venta entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.UsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.Ventas.Add(entidad);
        return entidad;
    }

    public async Task<Venta> ActualizarAsync(Venta entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.Ventas.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.Ventas.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.Ventas.Update(entidad);
        await _context.SaveChangesAsync();
    }
}
