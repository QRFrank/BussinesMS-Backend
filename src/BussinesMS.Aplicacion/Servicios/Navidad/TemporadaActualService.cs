using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

public class TemporadaActualService : ITemporadaActualService
{
    private readonly ITemporadaRepository _repo;

    public TemporadaActualService(ITemporadaRepository repo)
    {
        _repo = repo;
    }

    public async Task<Temporada> ObtenerAbiertaAsync()
    {
        var abierta = await _repo.ObtenerAbiertaAsync();
        if (abierta == null)
            throw new ExcepcionDominio("No hay temporada abierta", 409, "SIN_TEMPORADA_ABIERTA");
        return abierta;
    }

    // Si no viene temporadaId se usa la abierta; con Id permite consultar temporadas cerradas (solo lectura)
    public async Task<int> ResolverTemporadaIdAsync(int? temporadaId)
    {
        if (temporadaId.HasValue)
        {
            var temporada = await _repo.ObtenerPorIdAsync(temporadaId.Value);
            if (temporada == null)
                throw new EntidadNoEncontradaException("Temporada", temporadaId.Value);
            return temporada.Id;
        }

        var abierta = await ObtenerAbiertaAsync();
        return abierta.Id;
    }

    public async Task<Temporada> VerificarEditableAsync(int temporadaId)
    {
        var temporada = await _repo.ObtenerPorIdAsync(temporadaId);
        if (temporada == null)
            throw new EntidadNoEncontradaException("Temporada", temporadaId);

        VerificarEditable(temporada);
        return temporada;
    }

    public void VerificarEditable(Temporada temporada)
    {
        if (temporada.Estado == EstadoTemporada.Cerrada)
            throw new ExcepcionDominio("La temporada está cerrada y es de solo lectura", 409, "TEMPORADA_CERRADA");
    }

    // True si el almacén requiere apertura/cierre diario (conteo) en esa temporada
    public async Task<bool> RequiereConteoAsync(int temporadaId, int almacenId)
        => await _repo.ExisteAlmacenConteoAsync(temporadaId, almacenId);
}
