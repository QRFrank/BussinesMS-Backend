using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Compra a cualquier proveedor (Ajuste 2). No se edita: se anula. Puede tener varios PagoProveedor vinculados (PagoProveedor.CompraId)
public class Compra : EntidadBase
{
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string? NroNota { get; set; }
    public DateTime Fecha { get; set; }
    public bool PagadaAlContado { get; set; }
    public string? Observacion { get; set; }
    public bool Anulada { get; set; }

    public Temporada? Temporada { get; set; }
    public Proveedor? Proveedor { get; set; }
    // Pagos vinculados (el automático al crear + los que se registren después); incluye anulados (IsActive false)
    public ICollection<PagoProveedor> Pagos { get; set; } = new List<PagoProveedor>();
    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
}
