using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IVendedorNavRepository : IRepositorio<Vendedor>
{
    Task<bool> ExisteUsuarioAsync(int temporadaId, int usuarioId, int? excluirId = null);
    Task<List<int>> ObtenerUsuarioIdsActivosAsync(int temporadaId);
    // Sin tracking: solo lectura
    Task<List<Vendedor>> ObtenerActivosPorTemporadaAsync(int temporadaId);
}
