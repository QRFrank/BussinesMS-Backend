using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class MovimientoNavRepository : NavidadRepositorioBase<MovimientoNav>, IMovimientoNavRepository
{
    public MovimientoNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }
}
