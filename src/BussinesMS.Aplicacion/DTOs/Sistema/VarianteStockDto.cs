namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class VarianteStockDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? NombreProducto { get; set; }
    public string? VarianteNombre { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal PrecioVentaUnitario { get; set; }
    public decimal PrecioVentaMayoreo { get; set; }
    public decimal PrecioCompra { get; set; }
    public string? CodigoAlmacen { get; set; }
    public bool IsActive { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public int StockDisponible { get; set; }
    public int CantidadVendida { get; set; }
    public int CantidadVencida { get; set; }
    public List<PresentacionVarianteDto> Presentaciones { get; set; } = [];
}

public class VarianteStockDetalleDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? NombreProducto { get; set; }
    public string? VarianteNombre { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal PrecioVentaUnitario { get; set; }
    public decimal PrecioVentaMayoreo { get; set; }
    public decimal PrecioCompra { get; set; }
    public string? CodigoAlmacen { get; set; }
    public bool IsActive { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public int StockDisponible { get; set; }
    public int CantidadVendida { get; set; }
    public int CantidadVencida { get; set; }
    public List<PresentacionVarianteDto> Presentaciones { get; set; } = [];
    public List<LoteAlmacenStockDto> Lotes { get; set; } = [];
}

public class PresentacionVarianteDto
{
    public int Id { get; set; }
    public string? NombrePersonalizado { get; set; }
    public int Cantidad { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool EsDefaultReporte { get; set; }
}

public class LoteAlmacenStockDto
{
    public int Id { get; set; }
    public string CodigoLote { get; set; } = string.Empty;
    public int AlmacenId { get; set; }
    public int StockInicial { get; set; }
    public int StockDisponible { get; set; }
    public int CantidadVendida { get; set; }
    public int CantidadVencida { get; set; }
    public int EstadoLote { get; set; }
    public int? CompraDetalleId { get; set; }
    public decimal CostoCompraUnitario { get; set; }
    public DateTime? FechaVencimiento { get; set; }
    public int? DiasParaVencer { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
