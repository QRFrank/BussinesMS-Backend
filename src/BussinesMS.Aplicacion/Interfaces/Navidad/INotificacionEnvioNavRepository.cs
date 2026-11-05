using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface INotificacionEnvioNavRepository : IRepositorio<NotificacionEnvioNav>
{
    /// <summary>Con tracking e Include(Notificacion). Pendientes que ya les toca, por prioridad y antigüedad.</summary>
    Task<List<NotificacionEnvioNav>> ObtenerPendientesParaEnviarAsync(DateTime ahoraUtc, int max);
    /// <summary>Con tracking e Include(Notificacion). Pendientes que llevan esperando desde antes de <paramref name="pendienteAntesDeUtc"/>.</summary>
    Task<List<NotificacionEnvioNav>> ObtenerPendientesAntiguosAsync(DateTime pendienteAntesDeUtc, int max);
    Task<int> ContarPendientesAsync();
    Task<int> ContarEnviadosDesdeAsync(DateTime desdeUtc);
    /// <summary>Con tracking e Include(Notificacion).</summary>
    Task<NotificacionEnvioNav?> ObtenerConNotificacionAsync(int id);
}
