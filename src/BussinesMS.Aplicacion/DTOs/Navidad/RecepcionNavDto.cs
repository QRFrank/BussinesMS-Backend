using BussinesMS.Aplicacion.DTOs.Plantillas;

namespace BussinesMS.Aplicacion.DTOs.Navidad;

public class RecepcionNavFiltroDto : GenericPaginationQueryDto
{
    public int? TemporadaId { get; set; }
    public int? ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }
    // null → todas; true → solo anuladas; false → solo vigentes
    public bool? Anuladas { get; set; }
}

public class RecepcionNavDto
{
    public int Id { get; set; }
    public int TemporadaId { get; set; }
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    // Siempre true desde el Ajuste 2 (solo proveedores con pedido)
    public bool TrabajaConPedido { get; set; }
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoTitular { get; set; }
    public string? NroFactura { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observacion { get; set; }
    public bool Anulada { get; set; }
    public int CantidadProductos { get; set; }
    public int TotalUnidades { get; set; }
    public decimal MontoTotal { get; set; }
    // Vacío en la lista
    public List<RecepcionDetalleNavDto> Detalles { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RecepcionDetalleNavDto
{
    // Línea de la recepción (Ajuste 2: ya no es un lote)
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public int CantidadUnidades { get; set; }
    public decimal PrecioCompraUnidad { get; set; }
    public decimal Subtotal { get; set; }
    public List<RecepcionDistribucionNavDto> Distribucion { get; set; } = new();
}

public class RecepcionDistribucionNavDto
{
    public int Id { get; set; }
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int CantidadUnidades { get; set; }
}

public class CrearRecepcionNavDto
{
    public int ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    public string? NroFactura { get; set; }
    public DateTime Fecha { get; set; }
    public string? Observacion { get; set; }
    public List<CrearRecepcionDetalleNavDto> Detalles { get; set; } = new();
}

public class CrearRecepcionDetalleNavDto
{
    public int ProductoId { get; set; }
    public List<CrearRecepcionDistribucionNavDto> Distribucion { get; set; } = new();
}

public class CrearRecepcionDistribucionNavDto
{
    public int AlmacenId { get; set; }
    public int CantidadUnidades { get; set; }
}
