using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICategoriaProductoNavRepository : IRepositorio<CategoriaProductoNav>
{
    Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null);
}
