using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Infraestructura.Persistencia;
using BussinesMS.Infraestructura.Repositorios.Compartido;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class NavidadRepositorioBase<T> : RepositorioBase<T> where T : EntidadBase
{
    public NavidadRepositorioBase(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }
}
