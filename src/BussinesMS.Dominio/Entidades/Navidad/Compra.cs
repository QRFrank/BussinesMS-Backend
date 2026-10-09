using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Compra a cualquier proveedor (Ajuste 2). No se edita: se anula. Con PagadaAlContado tiene un PagoProveedor automático
public class Compra : EntidadBase
{
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string? NroNota { get; set; }
    public DateTime Fecha { get; set; }
    public bool PagadaAlContado { get; set; }
    public int? PagoProveedorId { get; set; }
    public string? Observacion { get; set; }
    public bool Anulada { get; set; }

    public Temporada? Temporada { get; set; }
    public Proveedor? Proveedor { get; set; }
    public PagoProveedor? PagoProveedor { get; set; }
    public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
}
