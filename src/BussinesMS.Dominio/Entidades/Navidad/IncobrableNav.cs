using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Deuda dada por incobrable (cierre de temporada): de un cliente (una venta) o del saldo de un vendedor de ruta. Pérdida de la temporada; anulable mientras la temporada esté abierta
public class IncobrableNav : EntidadBase
{
    public int TemporadaId { get; set; }
    public TipoIncobrableNav Tipo { get; set; }
    // Solo Tipo Cliente
    public int? VentaId { get; set; }
    // Solo Tipo VendedorRuta
    public int? VendedorId { get; set; }
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = string.Empty;
    // Día (hora Bolivia)
    public DateTime Fecha { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    public bool Anulado { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    // Sin FK: Usuario vive en AuthDB
    public int? UsuarioAnulacionId { get; set; }
    public string? MotivoAnulacion { get; set; }

    public Temporada? Temporada { get; set; }
    public VentaNav? Venta { get; set; }
    public Vendedor? Vendedor { get; set; }
    public ICollection<IncobrableNavAplicacion> Aplicaciones { get; set; } = new List<IncobrableNavAplicacion>();
}
