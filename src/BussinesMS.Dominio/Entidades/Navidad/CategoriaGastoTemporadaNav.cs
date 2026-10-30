using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Vínculo muchos a muchos: categorías de gasto habilitadas en cada temporada.
// Al crear una temporada se copian los vínculos de la anterior.
public class CategoriaGastoTemporadaNav : EntidadBase
{
    public int CategoriaGastoId { get; set; }
    public int TemporadaId { get; set; }

    public CategoriaGastoNav? CategoriaGasto { get; set; }
    public Temporada? Temporada { get; set; }
}
