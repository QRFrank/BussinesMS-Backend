using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class NotificacionEnvioNavRepository : NavidadRepositorioBase<NotificacionEnvioNav>, INotificacionEnvioNavRepository
{
    public NotificacionEnvioNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<List<NotificacionEnvioNav>> ObtenerPendientesParaEnviarAsync(DateTime ahoraUtc, int max)
    {
        return await _dbSet
            .Include(e => e.Notificacion)
            .Where(e => e.IsActive
                && e.Estado == EstadoEnvioNotificacionNav.Pendiente
                && (e.ProximoIntentoEn == null || e.ProximoIntentoEn <= ahoraUtc)
                && (e.Notificacion!.ProgramadaPara == null || e.Notificacion.ProgramadaPara <= ahoraUtc))
            .OrderBy(e => e.Notificacion!.Prioridad).ThenBy(e => e.NotificacionId).ThenBy(e => e.Id)
            .Take(max)
            .ToListAsync();
    }

    public async Task<List<NotificacionEnvioNav>> ObtenerPendientesAntiguosAsync(DateTime pendienteAntesDeUtc, int max)
    {
        return await _dbSet
            .Include(e => e.Notificacion)
            .Where(e => e.IsActive && e.Estado == EstadoEnvioNotificacionNav.Pendiente && e.PendienteDesde < pendienteAntesDeUtc)
            .OrderBy(e => e.Id)
            .Take(max)
            .ToListAsync();
    }

    public async Task<int> ContarPendientesAsync()
    {
        return await _dbSet.CountAsync(e => e.IsActive && e.Estado == EstadoEnvioNotificacionNav.Pendiente);
    }

    public async Task<int> ContarEnviadosDesdeAsync(DateTime desdeUtc)
    {
        return await _dbSet.CountAsync(e => e.IsActive && e.Estado == EstadoEnvioNotificacionNav.Enviado && e.EnviadoEn >= desdeUtc);
    }

    public async Task<NotificacionEnvioNav?> ObtenerConNotificacionAsync(int id)
    {
        return await _dbSet.Include(e => e.Notificacion).FirstOrDefaultAsync(e => e.Id == id);
    }
}
