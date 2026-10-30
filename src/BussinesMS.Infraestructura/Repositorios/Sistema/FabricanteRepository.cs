using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class FabricanteRepository : IFabricanteRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public FabricanteRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<Fabricante> AsQueryable()
    {
        return _context.Fabricantes.AsQueryable();
    }

    public async Task<List<Fabricante>> ObtenerTodosAsync()
    {
        return await _context.Fabricantes
            .Where(f => f.IsActive)
            .OrderBy(f => f.Nombre)
            .ToListAsync();
    }

    public async Task<Fabricante?> ObtenerPorIdAsync(int id)
    {
        return await _context.Fabricantes.FindAsync(id);
    }

    public async Task<Fabricante> CrearAsync(Fabricante entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;

        _context.Fabricantes.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<Fabricante> ActualizarAsync(Fabricante entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId();
        if (usuarioId.HasValue)
        {
            entidad.UpdatedByUsuarioId = usuarioId;
            entidad.UpdatedAt = DateTime.UtcNow;
        }

        _context.Fabricantes.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.Fabricantes.FindAsync(id);
        if (entidad != null)
        {
            var usuarioId = _currentUser.GetUsuarioId();
            if (usuarioId.HasValue)
            {
                entidad.DeletedByUsuarioId = usuarioId;
                entidad.DeletedAt = DateTime.UtcNow;
                entidad.IsActive = false;
                _context.Fabricantes.Update(entidad);
            }
            else
            {
                _context.Fabricantes.Remove(entidad);
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Fabricante?> ObtenerPorNombreAsync(string nombre)
    {
        return await _context.Fabricantes
            .FirstOrDefaultAsync(f => f.Nombre.ToLower() == nombre.ToLower());
    }

    public async Task<Fabricante> ReactivarAsync(int id)
    {
        var entidad = await _context.Fabricantes.FindAsync(id);
        if (entidad == null)
            throw new Exception("Fabricante no encontrado");

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        entidad.DeletedAt = null;
        entidad.DeletedByUsuarioId = null;
        entidad.IsActive = true;

        _context.Fabricantes.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }
}