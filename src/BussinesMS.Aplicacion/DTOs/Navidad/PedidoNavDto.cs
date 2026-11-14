using BussinesMS.Aplicacion.Seguridad;

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
    [OcultoSinPrecioCompra] public decimal? MontoTotalCalculado { get; set; }
    [OcultoSinPrecioCompra] public decimal? MontoTotalProveedor { get; set; }
    [OcultoSinPrecioCompra] public bool? DifiereMonto { get; set; }
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
    public string? Color { get; set; }
    public int CantidadUnidades { get; set; }
    [OcultoSinPrecioCompra] public decimal? PrecioCompraUnidad { get; set; }
    [OcultoSinPrecioCompra] public decimal? Subtotal { get; set; }
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

// Recepciones de un pedido (app móvil, solo lectura)
public class PedidoRecepcionesNavDto
{
    public int PedidoId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoTitular { get; set; }
    public int TotalPedido { get; set; }
    public int TotalRecibido { get; set; }
    // Pedido − Recibido; negativo si llegó de más
    public int TotalFaltante { get; set; }
    public List<PedidoRecepcionAlmacenNavDto> PorAlmacen { get; set; } = new();
    public List<PedidoRecepcionProductoNavDto> PorProducto { get; set; } = new();
    public List<PedidoRecepcionItemNavDto> Recepciones { get; set; } = new();
}

public class PedidoRecepcionAlmacenNavDto
{
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int CantidadUnidades { get; set; }
}

public class PedidoRecepcionProductoNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Pedido { get; set; }
    public int Recibido { get; set; }
    public int Faltante { get; set; }
    public bool LlegoDeMas { get; set; }
    public List<PedidoRecepcionAlmacenNavDto> PorAlmacen { get; set; } = new();
}

public class PedidoRecepcionItemNavDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string? NroFactura { get; set; }
    public string? Observacion { get; set; }
    public int CantidadProductos { get; set; }
    public int TotalUnidades { get; set; }
    [OcultoSinPrecioCompra] public decimal? MontoTotal { get; set; }
    public List<RecepcionDetalleNavDto> Detalles { get; set; } = new();
}
