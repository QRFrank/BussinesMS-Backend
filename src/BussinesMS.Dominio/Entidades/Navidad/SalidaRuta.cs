using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Salida a ruta de un vendedor: una sola abierta por vendedor y temporada. Agrupa cargas y devoluciones
public class SalidaRuta : EntidadBase
{
    public int TemporadaId { get; set; }
    public int VendedorId { get; set; }
    public DateTime FechaApertura { get; set; }
    public EstadoSalidaRuta Estado { get; set; }
    public DateTime? FechaCierre { get; set; }
    // Venta de ruta generada al cerrar
    public int? VentaId { get; set; }
    public string? Observacion { get; set; }

    public Temporada? Temporada { get; set; }
    public Vendedor? Vendedor { get; set; }
    public VentaNav? Venta { get; set; }
    public ICollection<SalidaRutaCarga> Cargas { get; set; } = new List<SalidaRutaCarga>();
    public ICollection<DevolucionRuta> Devoluciones { get; set; } = new List<DevolucionRuta>();
}
