using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class DescripcionTamanioRepository : IDescripcionTamanioRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DescripcionTamanioRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<DescripcionTamanio> AsQueryable()
    {
        return _context.DescripcionTamanios.AsQueryable();
    }

    public async Task<List<DescripcionTamanio>> ObtenerTodosAsync()
    {
        return await _context.DescripcionTamanios
            .Where(t => t.IsActive)
            .OrderBy(t => t.Nombre)
            .ToListAsync();
    }

    public async Task<DescripcionTamanio?> ObtenerPorIdAsync(int id)
    {
        return await _context.DescripcionTamanios.FindAsync(id);
    }

    public async Task<DescripcionTamanio> CrearAsync(DescripcionTamanio entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;

        _context.DescripcionTamanios.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<DescripcionTamanio> ActualizarAsync(DescripcionTamanio entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId();
        if (usuarioId.HasValue)
        {
            entidad.UpdatedByUsuarioId = usuarioId;
            entidad.UpdatedAt = DateTime.UtcNow;
        }

        _context.DescripcionTamanios.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.DescripcionTamanios.FindAsync(id);
        if (entidad != null)
        {
            var usuarioId = _currentUser.GetUsuarioId();
            if (usuarioId.HasValue)
            {
                entidad.DeletedByUsuarioId = usuarioId;
                entidad.DeletedAt = DateTime.UtcNow;
                entidad.IsActive = false;
                _context.DescripcionTamanios.Update(entidad);
            }
            else
            {
                _context.DescripcionTamanios.Remove(entidad);
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<DescripcionTamanio?> ObtenerPorNombreAsync(string nombre)
    {
        return await _context.DescripcionTamanios
            .FirstOrDefaultAsync(t => t.Nombre.ToLower() == nombre.ToLower());
    }

    public async Task<DescripcionTamanio> ReactivarAsync(int id)
    {
        var entidad = await _context.DescripcionTamanios.FindAsync(id);
        if (entidad == null)
            throw new Exception("Tamaño no encontrado");

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        entidad.DeletedAt = null;
        entidad.DeletedByUsuarioId = null;
        entidad.IsActive = true;

        _context.DescripcionTamanios.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }
}