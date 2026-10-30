using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class PagoCompraRepository : IPagoCompraRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PagoCompraRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<PagoCompra> AsQueryable()
        => _context.PagosCompra.AsQueryable();

    public async Task<List<PagoCompra>> ObtenerTodosAsync()
        => await _context.PagosCompra
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.FechaPago)
            .ToListAsync();

    public async Task<PagoCompra?> ObtenerPorIdAsync(int id)
        => await _context.PagosCompra
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<PagoCompra> CrearAsync(PagoCompra entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.PagosCompra.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<PagoCompra> CrearSinGuardarAsync(PagoCompra entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.PagosCompra.Add(entidad);
        return entidad;
    }

    public async Task<PagoCompra> ActualizarAsync(PagoCompra entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.PagosCompra.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.PagosCompra.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.PagosCompra.Update(entidad);
        await _context.SaveChangesAsync();
    }
}
