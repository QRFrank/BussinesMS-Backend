using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Anulado = IsActive false (borrado lógico)
public class PagoProveedor : EntidadBase
{
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    public DateTime Fecha { get; set; }
    public decimal Monto { get; set; }
    public MedioPagoNav Medio { get; set; }
    public string? Comprobante { get; set; }
    public string? Observacion { get; set; }

    public Temporada? Temporada { get; set; }
    public Proveedor? Proveedor { get; set; }
    public CodigoCliente? CodigoCliente { get; set; }
}
