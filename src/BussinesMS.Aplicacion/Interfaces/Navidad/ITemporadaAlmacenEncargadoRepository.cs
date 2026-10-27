using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ITemporadaAlmacenEncargadoRepository : IRepositorio<TemporadaAlmacenEncargado>
{
    // Sin tracking: solo lectura
    Task<List<TemporadaAlmacenEncargado>> ObtenerPorTemporadaAsync(int temporadaId);
    Task<List<int>> ObtenerAlmacenIdsDeUsuarioAsync(int temporadaId, int usuarioId);
    // Reemplaza los encargados de un almacén en la temporada (borra y vuelve a crear)
    Task ReemplazarAsync(int temporadaId, int almacenId, IEnumerable<int> usuarioIds);
}
