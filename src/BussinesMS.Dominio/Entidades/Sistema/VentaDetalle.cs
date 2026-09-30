using BussinesMS.Dominio.Enums;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class VentaDetalle
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int VarianteId { get; set; }
    public int LoteId { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioUnitarioCobrado { get; set; }
    public decimal CostoUnitarioLote { get; set; }
    public decimal Subtotal { get; set; }
    /// <summary>Qué campo de precio (Unitario o Mayoreo) usó el cajero para esta línea, sin importar si el monto fue editado a mano. Nullable: ventas históricas quedan sin registrar (null), a propósito, sin backfill.</summary>
    public TipoPrecioVenta? TipoPrecio { get; set; }

    public Venta? Venta { get; set; }
    public ProductoVariante? Variante { get; set; }
    public InventarioLote? Lote { get; set; }
}
