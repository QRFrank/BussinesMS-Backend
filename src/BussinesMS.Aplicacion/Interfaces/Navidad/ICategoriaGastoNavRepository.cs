using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICategoriaGastoNavRepository : IRepositorio<CategoriaGastoNav>
{
    Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null);
}
