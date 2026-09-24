using BussinesMS.Dominio.Entidades.Compartido;
using BussinesMS.Dominio.Enums;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class InventarioLote : EntidadBase
{
    public int VarianteId { get; set; }
    public int? CompraDetalleId { get; set; }
    public string CodigoLote { get; set; } = string.Empty;
    public decimal CostoCompraUnitario { get; set; }
    public int StockInicial { get; set; }
    public int CantidadVendida { get; set; } = 0;
    public int CantidadVencida { get; set; } = 0;
    public EstadoLote EstadoLote { get; set; } = EstadoLote.Activo;
    public DateTime? FechaVencimiento { get; set; }

    public ProductoVariante? Variante { get; set; }
    public CompraDetalle? CompraDetalle { get; set; }
}
