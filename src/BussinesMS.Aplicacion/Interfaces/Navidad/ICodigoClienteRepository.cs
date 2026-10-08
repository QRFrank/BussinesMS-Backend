using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICodigoClienteRepository : IRepositorio<CodigoCliente>
{
    Task<CodigoCliente?> ObtenerConDetallesAsync(int id);
    Task<bool> ExisteCodigoAsync(int proveedorId, string codigo, int? excluirId = null);
}
