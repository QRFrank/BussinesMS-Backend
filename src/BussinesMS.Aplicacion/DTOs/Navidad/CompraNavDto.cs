using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Dominio.Enums.Navidad;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class CompraNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? ProveedorId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // null → todas; true → solo anuladas; false → solo vigentes
    public bool? Anuladas { get; set; }
    public bool? PagadaAlContado { get; set; }
}

public class CompraNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string? NroNota { get; set; }
    public DateTime Fecha { get; set; }
    public bool PagadaAlContado { get; set; }
    public int? PagoProveedorId { get; set; }
    // Del PagoProveedor automático (null si no hay)
    public MedioPagoNav? MedioPago { get; set; }
    public string? MedioPagoNombre { get; set; }
    public string? Observacion { get; set; }
    public bool Anulada { get; set; }
    public int CantidadProductos { get; set; }
    public int TotalUnidades { get; set; }
    public decimal MontoTotal { get; set; }
    // Vacío en la lista
    public List<CompraDetalleNavDto> Detalles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CompraDetalleNavDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    // Proveedor principal del producto (puede diferir del proveedor de la compra)
    public int ProveedorPrincipalId { get; set; }
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    public decimal Subtotal { get; set; }
    public List<CompraDistribucionNavDto> Distribucion { get; set; } = new();
}

public class CompraDistribucionNavDto
{
    public int Id { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int CantidadUnidades { get; set; }
}

public class CrearCompraNavDto
{
    public int ProveedorId { get; set; }
    public string? NroNota { get; set; }
    public DateTime Fecha { get; set; }
    public bool PagadaAlContado { get; set; }
    // Obligatorio si PagadaAlContado
    public MedioPagoNav? MedioPago { get; set; }
    public string? Observacion { get; set; }
    public List<CrearCompraDetalleNavDto> Detalles { get; set; } = new();
}

public class CrearCompraDetalleNavDto
{
    public int ProductoId { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    // true → actualiza el precio de compra del producto (nunca el de catálogo)
    public bool ActualizarPrecioProducto { get; set; }
    public List<CrearCompraDistribucionNavDto> Distribucion { get; set; } = new();
}

public class CrearCompraDistribucionNavDto
{
    public int AlmacenId { get; set; }
    public int CantidadUnidades { get; set; }
}
