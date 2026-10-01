using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Infraestructura.Persistencia;
using BussinesMS.Infraestructura.Repositorios.Compartido;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Auth;

public class UsuarioRepository : RepositorioBase<Usuario>, IUsuarioRepository
{
    public UsuarioRepository(AuthDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<Usuario?> ObtenerPorUsernameAsync(string username)
    {
        return await _dbSet.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<Usuario?> ValidarCredencialesAsync(string username, string password)
    {
        return await ObtenerPorUsernameAsync(username);
    }

    public async Task<Usuario?> ObtenerConRolAsync(int id)
    {
        return await _dbSet.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<Usuario?> ObtenerConRolAsyncPorUsername(string username)
    {
        return await _dbSet.Include(u => u.Rol).FirstOrDefaultAsync(u => u.Username == username);
    }

    public async Task<List<UsuarioMenu>> ObtenerMenusAsync(int usuarioId)
    {
        return await ((AuthDbContext)_contexto).UsuarioMenus
            .Where(um => um.UsuarioId == usuarioId)
            .Include(um => um.Menu)
            .ToListAsync();
    }

    public async Task AgregarMenusAsync(int usuarioId, List<UsuarioMenu> menus)
    {
        var contexto = (AuthDbContext)_contexto;
        foreach (var menu in menus)
        {
            menu.UsuarioId = usuarioId;
        }
        await contexto.UsuarioMenus.AddRangeAsync(menus);
        await contexto.SaveChangesAsync();
    }

    public async Task EliminarMenusAsync(int usuarioId)
    {
        var contexto = (AuthDbContext)_contexto;
        var menus = await contexto.UsuarioMenus.Where(um => um.UsuarioId == usuarioId).ToListAsync();
        contexto.UsuarioMenus.RemoveRange(menus);
        await contexto.SaveChangesAsync();
    }

    public async Task<Usuario> ReactivarAsync(int id)
    {
        var entidad = await _dbSet.FindAsync(id);
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        entidad!.IsActive = true;
        entidad.UpdatedAt = DateTime.UtcNow;
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.DeletedAt = null;
        entidad.DeletedByUsuarioId = null;
        _dbSet.Update(entidad);
        await _contexto.SaveChangesAsync();
        return entidad;
    }
}