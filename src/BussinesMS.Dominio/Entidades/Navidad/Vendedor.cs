using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class Vendedor : EntidadBase
{
    public int TemporadaId { get; set; }
    // Usuario de AuthDB (sin FK)
    public int UsuarioId { get; set; }
    public TipoVendedor Tipo { get; set; }
    public decimal? SueldoMensual { get; set; }

    public Temporada? Temporada { get; set; }
}
