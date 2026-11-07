using BussinesMS.Dominio.Entidades.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface ITemporadaActualService
{
    Task<Temporada> ObtenerAbiertaAsync();
    Task<int> ResolverTemporadaIdAsync(int? temporadaId);
    // Como ResolverTemporadaIdAsync pero devuelve la temporada (404 si el id no existe; sin id, la abierta o 409 SIN_TEMPORADA_ABIERTA)
    Task<Temporada> ObtenerParaLecturaAsync(int? temporadaId);
    Task<Temporada> VerificarEditableAsync(int temporadaId);
    void VerificarEditable(Temporada temporada);
    Task<bool> RequiereConteoAsync(int temporadaId, int almacenId);
}
