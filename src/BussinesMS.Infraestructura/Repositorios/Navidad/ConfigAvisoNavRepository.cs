using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class ConfigAvisoNavRepository : NavidadRepositorioBase<ConfigAvisoNav>, IConfigAvisoNavRepository
{
    public ConfigAvisoNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<ConfigAvisoNav?> ObtenerPorEventoAsync(EventoNotificacionNav evento)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.IsActive && c.Evento == evento);
    }
}
