using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class PagoCompra : EntidadBase
{
    public int CompraId { get; set; }
    public decimal Monto { get; set; }
    // Desglose del pago mixto: Monto = MontoCaja + MontoExterno. La caja solo descuenta MontoCaja.
    public decimal MontoCaja { get; set; } = 0;
    public decimal MontoExterno { get; set; } = 0;
    public DateTime FechaPago { get; set; } = DateTime.UtcNow;
    public int? SesionCajaId { get; set; }
    public int PagadoPorUsuarioId { get; set; }
    public string? Observacion { get; set; }

    public Compra? Compra { get; set; }
}
