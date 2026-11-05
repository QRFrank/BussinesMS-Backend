using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class NotificacionNavRepository : NavidadRepositorioBase<NotificacionNav>, INotificacionNavRepository
{
    public NotificacionNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<bool> ExisteClaveUnicaAsync(string claveUnica)
    {
        return await _dbSet.AnyAsync(n => n.IsActive && n.ClaveUnica == claveUnica);
    }

    public async Task<List<NotificacionNav>> ObtenerSinResolverAsync(DateTime ahoraUtc, int max)
    {
        return await _dbSet
            .Where(n => n.IsActive && n.ResueltaEn == null && (n.ProgramadaPara == null || n.ProgramadaPara <= ahoraUtc))
            .OrderBy(n => n.Prioridad).ThenBy(n => n.Id)
            .Take(max)
            .ToListAsync();
    }
}
