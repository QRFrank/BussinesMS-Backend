using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Registro PERSONAL y PRIVADO de una venta del vendedor de ruta a uno de sus clientes (cuaderno digital).
// No es una venta del sistema: no mueve stock ni crea VentaNav/CobroVenta, ni entra en deudas, comisiones o reportes.
public class VentaClienteRuta : EntidadBase
{
    public int TemporadaId { get; set; }
    public int VendedorId { get; set; }
    // Cliente compartido (ClienteNav)
    public int ClienteId { get; set; }
    // Salida de la que salió la venta (opcional)
    public int? SalidaRutaId { get; set; }
    // Fecha Bolivia (solo día)
    public DateTime Fecha { get; set; }
    public decimal Total { get; set; }
    // Cobrado al registrar + cobros posteriores vigentes
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
    public bool Anulada { get; set; }
    public string? Observacion { get; set; }

    public Temporada? Temporada { get; set; }
    public Vendedor? Vendedor { get; set; }
    public ClienteNav? Cliente { get; set; }
    public SalidaRuta? SalidaRuta { get; set; }
    public ICollection<VentaClienteRutaDetalle> Detalles { get; set; } = new List<VentaClienteRutaDetalle>();
    public ICollection<CobroClienteRuta> Cobros { get; set; } = new List<CobroClienteRuta>();
}
