using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

public class Recepcion : EntidadBase
{
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    public string? NroFactura { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observacion { get; set; }
    public bool Anulada { get; set; }

    public Temporada? Temporada { get; set; }
    public Proveedor? Proveedor { get; set; }
    public CodigoCliente? CodigoCliente { get; set; }
    public ICollection<RecepcionDetalle> Detalles { get; set; } = new List<RecepcionDetalle>();
}
