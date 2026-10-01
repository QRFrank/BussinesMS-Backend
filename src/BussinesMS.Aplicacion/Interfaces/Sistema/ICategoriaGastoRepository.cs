using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface ICategoriaGastoRepository : IRepositorio<CategoriaGasto>
{
    Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null);
}
