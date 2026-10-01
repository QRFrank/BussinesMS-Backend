using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class CategoriaGastoRepository : ICategoriaGastoRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public CategoriaGastoRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<CategoriaGasto> AsQueryable()
        => _context.CategoriasGasto.AsQueryable();

    public async Task<List<CategoriaGasto>> ObtenerTodosAsync()
        => await _context.CategoriasGasto
            .Where(x => x.IsActive)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

    public async Task<CategoriaGasto?> ObtenerPorIdAsync(int id)
        => await _context.CategoriasGasto
            .FirstOrDefaultAsync(x => x.Id == id);

    // Sin filtro IsActive: el índice único de Nombre en BD cubre también a los inactivos
    public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null)
    {
        var query = _context.CategoriasGasto
            .Where(x => x.Nombre.ToLower() == nombre.ToLower());
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<CategoriaGasto> CrearAsync(CategoriaGasto entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.CategoriasGasto.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<CategoriaGasto> ActualizarAsync(CategoriaGasto entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.CategoriasGasto.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.CategoriasGasto.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.CategoriasGasto.Update(entidad);
        await _context.SaveChangesAsync();
    }
}
