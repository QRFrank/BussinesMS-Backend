namespace BussinesMS.Aplicacion.DTOs.Navidad;

// Filtro común de faltantes y deudas (no paginado)
public class AbastecimientoNavFiltroDto
{
    public int? TemporadaId { get; set; }
    public int? ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
}

public class FaltanteNavDto
{
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoTitular { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public int Pedido { get; set; }
    public int Recibido { get; set; }
    public int Faltante { get; set; }
    public bool LlegoDeMas { get; set; }
    public bool TrabajaConPedido { get; set; }
}

// Ajuste 2: deuda = pedidos + compras − pagos, por proveedor + código (o sin código)
public class DeudaProveedorNavDto
{
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoTitular { get; set; }
    public bool TrabajaConPedido { get; set; }
    // Σ MontoTotalProveedor de pedidos activos (0 si no hay)
    public decimal TotalPedidos { get; set; }
    // Σ compras no anuladas (las compras no llevan código: van en la fila sin código del proveedor)
    public decimal TotalCompras { get; set; }
    // TotalPedidos + TotalCompras
    public decimal TotalDeuda { get; set; }
    // Σ pagos activos (incluye los pagos automáticos de compras al contado)
    public decimal TotalPagado { get; set; }
    // TotalDeuda − TotalPagado. Negativo = saldo a favor
    public decimal Saldo { get; set; }
}
