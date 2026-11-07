using BussinesMS.Aplicacion.Interfaces.Compartido;
using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ICierreTemporadaNavRepository : IRepositorio<CierreTemporadaNav>
{
    // Sin tracking, solo activo
    Task<CierreTemporadaNav?> ObtenerPorTemporadaAsync(int temporadaId);
}
