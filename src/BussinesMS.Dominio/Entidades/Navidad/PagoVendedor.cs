using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Pago a un vendedor: sueldo mensual (tienda) o comisión de fin de temporada (ruta). Anulación lógica
public class PagoVendedor : EntidadBase
{
    public int TemporadaId { get; set; }
    public int VendedorId { get; set; }
    public DateTime Fecha { get; set; }
    public TipoPagoVendedor Tipo { get; set; }
    // "yyyy-MM"; solo para sueldo
    public string? Periodo { get; set; }
    // Solo comisión
    public ModoComisionVendedor? ModoComision { get; set; }
    public decimal? Porcentaje { get; set; }
    // Neto vendido usado para calcular la comisión
    public decimal? MontoBase { get; set; }
    public decimal Monto { get; set; }
    public MedioPagoNav Medio { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    public string? Observacion { get; set; }
    public bool Anulado { get; set; }

    public Temporada? Temporada { get; set; }
    public Vendedor? Vendedor { get; set; }
    public ICollection<PagoVendedorDetalle> Detalles { get; set; } = new List<PagoVendedorDetalle>();
}
