using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class PagoInversor : EntidadBase
{
    // Copiado del aporte (todo dato de temporada lleva TemporadaId)
    public int TemporadaId { get; set; }
    public int AporteCapitalId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public TipoPagoInversor Tipo { get; set; }
    public string? Observacion { get; set; }

    public Temporada? Temporada { get; set; }
    public AporteCapital? AporteCapital { get; set; }
}
