using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class ProveedorRepository : IProveedorRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProveedorRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<Proveedor> AsQueryable()
        => _context.Proveedores.AsQueryable();

    public async Task<List<Proveedor>> ObtenerTodosAsync()
        => await _context.Proveedores
            .Where(x => x.IsActive)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

    public async Task<Proveedor?> ObtenerPorIdAsync(int id)
        => await _context.Proveedores.FindAsync(id);

    public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null)
    {
        var query = _context.Proveedores
            .Where(x => x.Nombre.ToLower() == nombre.ToLower() && x.IsActive);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<Proveedor> CrearAsync(Proveedor entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.Proveedores.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<Proveedor> ActualizarAsync(Proveedor entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.Proveedores.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.Proveedores.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.Proveedores.Update(entidad);
        await _context.SaveChangesAsync();
    }
}
