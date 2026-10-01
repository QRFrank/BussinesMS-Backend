using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IGastoOperativoRepository : IRepositorio<GastoOperativo>
{
    // Incluye CategoriaGasto y SesionCaja
    Task<GastoOperativo?> ObtenerConDetallesAsync(int id);
}
