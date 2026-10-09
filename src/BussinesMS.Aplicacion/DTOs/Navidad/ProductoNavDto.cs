using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class ProductoNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? ProveedorId { get; set; }
    public int? CategoriaProductoId { get; set; }
}

public class ProductoNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int CategoriaProductoId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    // Nombre ?? Descripcion (calculado en el mapeo, no se guarda)
    public string NombreMostrar { get; set; } = string.Empty;
    public decimal PrecioCompraUnidad { get; set; }
    public decimal PrecioCatalogo { get; set; }
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CrearProductoNavDto
{
    public int ProveedorId { get; set; }
    public int CategoriaProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    public decimal PrecioCatalogo { get; set; }
    // Opcionales, van juntos
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
}

public class ActualizarProductoNavDto
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public int CategoriaProductoId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    public decimal PrecioCatalogo { get; set; }
    // Opcionales, van juntos
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
}

// Ítem de la actualización masiva de precios (PUT api/Navidad/Productos/precios)
public class ActualizarPrecioProductoNavDto
{
    public int Id { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    public decimal PrecioCatalogo { get; set; }
}

public class ActualizarPreciosResultadoNavDto
{
    public int Actualizados { get; set; }
}
