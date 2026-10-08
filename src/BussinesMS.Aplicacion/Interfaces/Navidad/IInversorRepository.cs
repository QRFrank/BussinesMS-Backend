using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IInversorRepository : IRepositorio<Inversor>
{
    Task<bool> TieneAportesEnTemporadaAbiertaAsync(int inversorId);
}
