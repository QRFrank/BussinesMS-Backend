using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class TipoPresentacionRepository : ITipoPresentacionRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public TipoPresentacionRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<TipoPresentacion> AsQueryable()
        => _context.TiposPresentacion.AsQueryable();

    public async Task<List<TipoPresentacion>> ObtenerTodosAsync()
        => await _context.TiposPresentacion
            .Where(x => x.IsActive)
            .OrderBy(x => x.Orden)
            .ToListAsync();

    public async Task<TipoPresentacion?> ObtenerPorIdAsync(int id)
        => await _context.TiposPresentacion
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null)
    {
        var query = _context.TiposPresentacion
            .Where(x => x.Nombre.ToLower() == nombre.ToLower() && x.IsActive);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    // Unidad es Orden=1, no se puede eliminar
    public async Task<bool> EsUnidadAsync(int id)
        => await _context.TiposPresentacion
            .AnyAsync(x => x.Id == id && x.Orden == 1);

    public async Task<TipoPresentacion> CrearAsync(TipoPresentacion entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.TiposPresentacion.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<TipoPresentacion> ActualizarAsync(TipoPresentacion entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.TiposPresentacion.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.TiposPresentacion.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.TiposPresentacion.Update(entidad);
        await _context.SaveChangesAsync();
    }
}