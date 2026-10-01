using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class GastoOperativoRepository : IGastoOperativoRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public GastoOperativoRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<GastoOperativo> AsQueryable()
        => _context.GastosOperativos.AsQueryable();

    public async Task<List<GastoOperativo>> ObtenerTodosAsync()
        => await _context.GastosOperativos
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.FechaGasto)
            .ToListAsync();

    public async Task<GastoOperativo?> ObtenerPorIdAsync(int id)
        => await _context.GastosOperativos
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<GastoOperativo?> ObtenerConDetallesAsync(int id)
        => await _context.GastosOperativos
            .Include(x => x.CategoriaGasto)
            .Include(x => x.SesionCaja)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<GastoOperativo> CrearAsync(GastoOperativo entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.GastosOperativos.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<GastoOperativo> ActualizarAsync(GastoOperativo entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.GastosOperativos.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.GastosOperativos.FindAsync(id);
        if (entidad == null) return;

        // Siempre soft-delete (anulación)
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.GastosOperativos.Update(entidad);
        await _context.SaveChangesAsync();
    }
}
