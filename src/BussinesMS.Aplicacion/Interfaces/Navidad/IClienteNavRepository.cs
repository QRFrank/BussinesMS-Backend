using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IClienteNavRepository : IRepositorio<ClienteNav>
{
    Task<bool> ExisteDocumentoAsync(string documento, int? excluirId = null);
}
