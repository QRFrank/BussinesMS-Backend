using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class DescripcionSaborRepository : IDescripcionSaborRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public DescripcionSaborRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<DescripcionSabor> AsQueryable()
    {
        return _context.DescripcionSabores.AsQueryable();
    }

    public async Task<List<DescripcionSabor>> ObtenerTodosAsync()
    {
        return await _context.DescripcionSabores
            .Where(s => s.IsActive)
            .OrderBy(s => s.Nombre)
            .ToListAsync();
    }

    public async Task<DescripcionSabor?> ObtenerPorIdAsync(int id)
    {
        return await _context.DescripcionSabores.FindAsync(id);
    }

    public async Task<DescripcionSabor> CrearAsync(DescripcionSabor entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;

        _context.DescripcionSabores.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<DescripcionSabor> ActualizarAsync(DescripcionSabor entidad)
    {
        var usuarioId = _currentUser.GetUsuarioId();
        if (usuarioId.HasValue)
        {
            entidad.UpdatedByUsuarioId = usuarioId;
            entidad.UpdatedAt = DateTime.UtcNow;
        }

        _context.DescripcionSabores.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.DescripcionSabores.FindAsync(id);
        if (entidad != null)
        {
            var usuarioId = _currentUser.GetUsuarioId();
            if (usuarioId.HasValue)
            {
                entidad.DeletedByUsuarioId = usuarioId;
                entidad.DeletedAt = DateTime.UtcNow;
                entidad.IsActive = false;
                _context.DescripcionSabores.Update(entidad);
            }
            else
            {
                _context.DescripcionSabores.Remove(entidad);
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<DescripcionSabor?> ObtenerPorNombreAsync(string nombre)
    {
        return await _context.DescripcionSabores
            .FirstOrDefaultAsync(s => s.Nombre.ToLower() == nombre.ToLower());
    }

    public async Task<DescripcionSabor> ReactivarAsync(int id)
    {
        var entidad = await _context.DescripcionSabores.FindAsync(id);
        if (entidad == null)
            throw new Exception("Sabor no encontrado");

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        entidad.DeletedAt = null;
        entidad.DeletedByUsuarioId = null;
        entidad.IsActive = true;

        _context.DescripcionSabores.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }
}