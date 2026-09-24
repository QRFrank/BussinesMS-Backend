using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class ClienteRepository : IClienteRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ClienteRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<Cliente> AsQueryable()
        => _context.Clientes.AsQueryable();

    public async Task<List<Cliente>> ObtenerTodosAsync()
        => await _context.Clientes
            .Where(x => x.IsActive)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

    public async Task<Cliente?> ObtenerPorIdAsync(int id)
        => await _context.Clientes.FindAsync(id);

    // Sin filtro IsActive — para detectar inactivos y reactivarlos
    public async Task<Cliente?> ObtenerPorNumeroCarnetAsync(string numeroCarnet)
        => await _context.Clientes
            .Where(x => x.NumeroCarnet == numeroCarnet)
            .FirstOrDefaultAsync();

    // Sin filtro IsActive — el índice único filtrado ([NumeroCarnet] IS NOT NULL) incluye inactivos
    public async Task<bool> ExisteNumeroCarnetAsync(string numeroCarnet, int? excludeId = null)
    {
        var query = _context.Clientes.Where(x => x.NumeroCarnet == numeroCarnet);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<Cliente> CrearAsync(Cliente entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.Clientes.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<Cliente> ActualizarAsync(Cliente entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.Clientes.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.Clientes.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.Clientes.Update(entidad);
        await _context.SaveChangesAsync();
    }

    public async Task<Cliente> ReactivarAsync(int id)
    {
        var entidad = await _context.Clientes.FindAsync(id);
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad!.IsActive = true;
        entidad.UpdatedAt = DateTime.UtcNow;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.DeletedAt = null;
        entidad.DeletedByUsuarioId = null;
        _context.Clientes.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }
}
