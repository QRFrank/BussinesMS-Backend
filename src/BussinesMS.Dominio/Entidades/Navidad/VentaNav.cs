using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Venta navideña (fuente de verdad: los estados viven en la cabecera). Canal Tienda (módulo 6) o Ruta (módulo 7).
public class VentaNav : EntidadBase
{
    public int TemporadaId { get; set; }
    // Correlativo por temporada
    public int NroVenta { get; set; }
    // Día de la venta (hora de Bolivia)
    public DateTime Fecha { get; set; }
    // Tienda de la venta (origen por defecto de las entregas). Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    public CanalVentaNav Canal { get; set; }
    // Quien la registró. Sin FK: Usuario vive en AuthDB
    public int UsuarioId { get; set; }
    public int VendedorId { get; set; }
    public int? ClienteId { get; set; }
    public TipoVentaNav Tipo { get; set; }
    public CondicionPagoVentaNav CondicionPago { get; set; }
    // Solo reservas: aparta las unidades pendientes en AlmacenId
    public bool BloquearStock { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal MontoSaldo { get; set; }
    public EstadoPagoVentaNav EstadoPago { get; set; }
    public EstadoEntregaVentaNav EstadoEntrega { get; set; }
    public bool Anulada { get; set; }
    public string? Observacion { get; set; }

    public Temporada? Temporada { get; set; }
    public Vendedor? Vendedor { get; set; }
    public ClienteNav? Cliente { get; set; }
    public ICollection<VentaNavDetalle> Detalles { get; set; } = new List<VentaNavDetalle>();
    public ICollection<EntregaVenta> Entregas { get; set; } = new List<EntregaVenta>();
    public ICollection<CobroVenta> Cobros { get; set; } = new List<CobroVenta>();
}
