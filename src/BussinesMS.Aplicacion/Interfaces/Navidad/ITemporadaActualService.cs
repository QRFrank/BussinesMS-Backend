using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ITemporadaActualService
{
    Task<Temporada> ObtenerAbiertaAsync();
    Task<int> ResolverTemporadaIdAsync(int? temporadaId);
    Task<Temporada> VerificarEditableAsync(int temporadaId);
    void VerificarEditable(Temporada temporada);
    Task<bool> RequiereConteoAsync(int temporadaId, int almacenId);
}
