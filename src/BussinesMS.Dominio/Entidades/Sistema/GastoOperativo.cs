using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class GastoOperativo : EntidadBase
{
    public int? SesionCajaId { get; set; }
    public int CategoriaGastoId { get; set; }
    public decimal Monto { get; set; }
    // Desglose del pago mixto: Monto = MontoCaja + MontoExterno. La caja solo descuenta MontoCaja.
    public decimal MontoCaja { get; set; } = 0;
    public decimal MontoExterno { get; set; } = 0;
    public string? Descripcion { get; set; }
    public DateTime FechaGasto { get; set; } = DateTime.UtcNow;
    // Sin FK: Almacen vive en la BD Auth (igual que SesionCaja.AlmacenId)
    public int? AlmacenId { get; set; }

    public SesionCaja? SesionCaja { get; set; }
    public CategoriaGasto? CategoriaGasto { get; set; }
}
