using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class PedidoNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
}

public class PedidoNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoTitular { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observacion { get; set; }
    public int CantidadProductos { get; set; }
    public int TotalUnidades { get; set; }
    // Siempre true desde el Ajuste 2 (solo proveedores con pedido)
    public bool TrabajaConPedido { get; set; }
    // Σ cantidad × precio de compra de las líneas
    public decimal MontoTotalCalculado { get; set; }
    public decimal MontoTotalProveedor { get; set; }
    public bool DifiereMonto { get; set; }
    // Vacío en la lista
    public List<PedidoDetalleNavDto> Detalles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PedidoDetalleNavDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    public decimal Subtotal { get; set; }
    // Σ recibido de ese proveedor + código + producto (mínimo de la fila al editar)
    public int Recibido { get; set; }
}

public class CrearPedidoNavDto
{
    public int ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observacion { get; set; }
    // null → se guarda el calculado
    public decimal? MontoTotalProveedor { get; set; }
    public List<CrearPedidoDetalleNavDto> Detalles { get; set; } = new();
}

public class ActualizarPedidoNavDto : CrearPedidoNavDto
{
    public int Id { get; set; }
}

public class CrearPedidoDetalleNavDto
{
    public int ProductoId { get; set; }
    // 0 = el proveedor no lo tuvo
    public int CantidadUnidades { get; set; }
    // Obligatorio (Ajuste 2): el precio de la nota de compra del código. No modifica el producto
    public decimal? PrecioCompraUnidad { get; set; }
}
