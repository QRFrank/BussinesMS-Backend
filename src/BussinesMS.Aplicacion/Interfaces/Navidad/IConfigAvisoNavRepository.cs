using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IConfigAvisoNavRepository : IRepositorio<ConfigAvisoNav>
{
    Task<ConfigAvisoNav?> ObtenerPorEventoAsync(EventoNotificacionNav evento);
}
