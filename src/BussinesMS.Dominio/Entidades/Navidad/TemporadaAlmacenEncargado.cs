using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Encargados de almacén por temporada (muchos a muchos). AlmacenId y UsuarioId viven en AuthDB: sin FK.
// Al reemplazar las asignaciones se borran las filas (no hay borrado lógico).
public class TemporadaAlmacenEncargado : EntidadBase
{
    public int TemporadaId { get; set; }
    public int AlmacenId { get; set; }
    public int UsuarioId { get; set; }

    public Temporada? Temporada { get; set; }
}
