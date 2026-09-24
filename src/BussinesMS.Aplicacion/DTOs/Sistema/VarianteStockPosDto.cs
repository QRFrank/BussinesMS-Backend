namespace BussinesMS.Aplicacion.DTOs.Sistema;

public class VarianteStockPosDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string? NombreProducto { get; set; }
    public string? VarianteNombre { get; set; }
    public string? CodigoBarras { get; set; }
    public decimal PrecioVentaUnitario { get; set; }
    public decimal PrecioVentaMayoreo { get; set; }
    public int? CategoriaId { get; set; }
    public string? CategoriaNombre { get; set; }
    public int Cantidad { get; set; }
    public List<PresentacionVarianteDto> Presentaciones { get; set; } = [];
}
