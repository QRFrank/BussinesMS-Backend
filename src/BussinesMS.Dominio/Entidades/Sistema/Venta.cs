using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class Venta : EntidadBase
{
    public int UsuarioId { get; set; }
    public int AlmacenId { get; set; }
    public int SesionCajaId { get; set; }
    public DateTime FechaVenta { get; set; } = DateTime.UtcNow;
    public decimal TotalBruto { get; set; }
    public decimal DescuentoTotal { get; set; } = 0;
    public decimal TotalNeto { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public decimal MontoEfectivo { get; set; } = 0;
    public decimal MontoTransferencia { get; set; } = 0;
    public decimal? MontoRecibido { get; set; }
    public decimal? Cambio { get; set; }
    public string? MotivoDescuento { get; set; }
    public int ClienteId { get; set; } = Cliente.ClienteGenericoId;

    public SesionCaja? SesionCaja { get; set; }
    public Cliente? Cliente { get; set; }
    public List<VentaDetalle> Detalles { get; set; } = [];
}
