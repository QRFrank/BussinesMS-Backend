using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Sistema;

namespace BussinesMS.Aplicacion.Interfaces.Sistema;

public interface IClienteRepository : IRepositorio<Cliente>
{
    // Busca sin filtrar por IsActive — el índice único de NumeroCarnet cubre también inactivos
    Task<Cliente?> ObtenerPorNumeroCarnetAsync(string numeroCarnet);
    Task<bool> ExisteNumeroCarnetAsync(string numeroCarnet, int? excludeId = null);
    Task<Cliente> ReactivarAsync(int id);
}
