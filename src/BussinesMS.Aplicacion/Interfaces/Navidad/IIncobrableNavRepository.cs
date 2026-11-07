using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IIncobrableNavRepository : IRepositorio<IncobrableNav>
{
    // CON tracking: aplicaciones
    Task<IncobrableNav?> ObtenerParaEdicionAsync(int id);
}
