using BussinesMS.Dominio.Entidades.Compartido;

namespace BussinesMS.Dominio.Entidades.Navidad;

// Stock por producto × almacén (Ajuste 2, reemplaza a LoteAlmacen). Solo se modifica vía IStockNavService, siempre con su MovimientoNav
public class StockAlmacen : EntidadBase
{
    public int TemporadaId { get; set; }
    public int ProductoId { get; set; }
    // Sin FK: Almacen vive en AuthDB
    public int AlmacenId { get; set; }
    public int Cantidad { get; set; }

    public Temporada? Temporada { get; set; }
    public Producto? Producto { get; set; }
}
