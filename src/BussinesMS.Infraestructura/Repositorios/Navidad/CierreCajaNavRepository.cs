using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class CierreCajaNavRepository : NavidadRepositorioBase<CierreCajaTienda>, ICierreCajaNavRepository
{
    public CierreCajaNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }
}
