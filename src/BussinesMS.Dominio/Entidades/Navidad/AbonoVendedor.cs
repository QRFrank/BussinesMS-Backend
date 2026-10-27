using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Abono del vendedor a su deuda (se aplica a las ventas pendientes). Anulación lógica
public class AbonoVendedor : EntidadBase
{
    public int TemporadaId { get; set; }
    public int VendedorId { get; set; }
    public DateTime Fecha { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    public decimal Monto { get; set; }
    public decimal Descuento { get; set; }
    // Null si Monto = 0
    public MedioPagoNav? Medio { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }
    public bool Anulado { get; set; }

    public Temporada? Temporada { get; set; }
    public Vendedor? Vendedor { get; set; }
    public ICollection<AbonoVendedorAplicacion> Aplicaciones { get; set; } = new List<AbonoVendedorAplicacion>();
}
