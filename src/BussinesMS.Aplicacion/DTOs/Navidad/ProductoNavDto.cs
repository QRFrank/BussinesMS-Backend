using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class ProductoNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? ProveedorId { get; set; }
    public int? CategoriaProductoId { get; set; }
}

public class PresentacionNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
    // Unidades * PrecioUnitario (calculado en el mapeo, no se guarda)
    public decimal PrecioTotal { get; set; }
    public bool EsPrincipal { get; set; }
}

public class ProductoNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int CategoriaProductoId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioCompraUnidad { get; set; }
    public decimal PrecioCatalogo { get; set; }
    // Solo presentaciones activas, ordenadas por Unidades asc
    public List<PresentacionNavDto> Presentaciones { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GuardarPresentacionNavDto
{
    // Null = presentación nueva (en POST se ignora)
    public int? Id { get; set; }
    // Para la presentación de 1 unidad, vacío = "Unidad"
    public string? Nombre { get; set; }
    public int Unidades { get; set; }
    public decimal PrecioUnitario { get; set; }
    public bool EsPrincipal { get; set; }
}

public class CrearProductoNavDto
{
    public int ProveedorId { get; set; }
    public int CategoriaProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioCompraUnidad { get; set; }
    public decimal PrecioCatalogo { get; set; }
    public List<GuardarPresentacionNavDto> Presentaciones { get; set; } = new();
}

public class ActualizarProductoNavDto
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public int CategoriaProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal PrecioCompraUnidad { get; set; }
    public decimal PrecioCatalogo { get; set; }
    public List<GuardarPresentacionNavDto> Presentaciones { get; set; } = new();
}
