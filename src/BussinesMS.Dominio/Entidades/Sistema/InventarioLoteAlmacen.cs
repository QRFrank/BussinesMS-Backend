using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Sistema;

public class InventarioLoteAlmacen : EntidadBase
{
    public int LoteId { get; set; }
    public int VarianteId { get; set; }
    public int AlmacenId { get; set; }
    public int StockDisponible { get; set; }

    public InventarioLote? Lote { get; set; }
}
