using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICategoriaGastoNavRepository : IRepositorio<CategoriaGastoNav>
{
    Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null);
    Task<CategoriaGastoNav?> ObtenerPorNombreAsync(string nombre);
    // Categorías ligadas a la temporada (sin filtrar IsActive)
    IQueryable<CategoriaGastoNav> AsQueryablePorTemporada(int temporadaId);
    Task<bool> EstaVinculadaAsync(int categoriaId, int temporadaId);
    Task VincularAsync(int categoriaId, int temporadaId);
    // Copia los vínculos de categorías activas de la temporada origen a la destino (omite los ya existentes)
    Task CopiarVinculosAsync(int temporadaOrigenId, int temporadaDestinoId);
}
