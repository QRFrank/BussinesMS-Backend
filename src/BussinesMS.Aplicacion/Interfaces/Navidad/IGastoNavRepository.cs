using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IGastoNavRepository : IRepositorio<GastoNav>
{
    Task<GastoNav?> ObtenerConDetallesAsync(int id);
    Task<bool> TieneGastosActivosPorCategoriaAsync(int categoriaId);
}
