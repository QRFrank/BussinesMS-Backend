using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class Temporada : EntidadBase
{
    public int Anio { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaCierre { get; set; }
    public EstadoTemporada Estado { get; set; } = EstadoTemporada.Abierta;

    public ICollection<TemporadaAlmacenConteo> AlmacenesConteo { get; set; } = new List<TemporadaAlmacenConteo>();
}
