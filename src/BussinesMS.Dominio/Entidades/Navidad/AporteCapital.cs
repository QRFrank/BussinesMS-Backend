using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class AporteCapital : EntidadBase
{
    public int TemporadaId { get; set; }
    // null = capital propio
    public int? InversorId { get; set; }
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
    public decimal PorcentajeComision { get; set; }
    public string? Observacion { get; set; }

    public Temporada? Temporada { get; set; }
    public Inversor? Inversor { get; set; }
    public ICollection<PagoInversor> Pagos { get; set; } = new List<PagoInversor>();
}
