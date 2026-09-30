namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class VarianteVentaDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? NombreProducto { get; set; }
    public string? VarianteNombre { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal PrecioVentaUnitario { get; set; }
    public decimal PrecioVentaMayoreo { get; set; }
    public int StockTotal { get; set; }
    public List<StockAlmacenVentaDto> Almacenes { get; set; } = [];
    public List<PresentacionVarianteDto> Presentaciones { get; set; } = [];
}

public class StockAlmacenVentaDto
{
    public int AlmacenId { get; set; }
    public string? AlmacenNombre { get; set; }
    public bool EsTienda { get; set; }
    public int StockDisponible { get; set; }
}
