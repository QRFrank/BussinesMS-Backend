using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IAporteCapitalRepository : IRepositorio<AporteCapital>
{
    Task<AporteCapital?> ObtenerConDetallesAsync(int id);
    // Aportes activos de la temporada con Inversor y Pagos (todos; los activos se filtran en el servicio)
    Task<List<AporteCapital>> ObtenerActivosConPagosPorTemporadaAsync(int temporadaId);
}
