using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface INotificacionNavRepository : IRepositorio<NotificacionNav>
{
    Task<bool> ExisteClaveUnicaAsync(string claveUnica);
    /// <summary>Con tracking. Notificaciones activas sin envíos generados y ya vencidas de programar, las más antiguas primero.</summary>
    Task<List<NotificacionNav>> ObtenerSinResolverAsync(DateTime ahoraUtc, int max);
}
