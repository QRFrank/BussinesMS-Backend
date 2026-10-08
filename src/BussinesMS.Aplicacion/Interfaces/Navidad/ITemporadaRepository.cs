using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ITemporadaRepository : IRepositorio<Temporada>
{
    Task<Temporada?> ObtenerAbiertaAsync();
    Task<bool> ExisteAbiertaAsync(int? excludeId = null);
    Task<List<TemporadaAlmacenConteo>> ObtenerAlmacenesConteoAsync(IEnumerable<int> temporadaIds);
    Task ReemplazarAlmacenesConteoAsync(int temporadaId, IEnumerable<int> almacenIds);
    Task<bool> ExisteAlmacenConteoAsync(int temporadaId, int almacenId);
}
