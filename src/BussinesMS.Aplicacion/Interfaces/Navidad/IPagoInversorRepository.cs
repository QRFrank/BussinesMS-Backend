using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IPagoInversorRepository : IRepositorio<PagoInversor>
{
    Task<PagoInversor?> ObtenerConDetallesAsync(int id);
    Task<decimal> SumarPagosActivosAsync(int aporteCapitalId, TipoPagoInversor tipo);
    Task<bool> TienePagosActivosAsync(int aporteCapitalId);
}
