namespace BussinesMS.Aplicacion.DTOs.Navidad;

// Reportes del sistema navideño (módulo 9, solo admin, solo lectura, temporada abierta). Montos en Bs, cantidades en unidades.
// Reutilizan los DTOs del dashboard donde el dato es el mismo, porque salen del mismo cálculo (ResumenNavCalculos).

// ---------- Resultado de la temporada ----------

public class ReporteResultadoNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    // null = desde el inicio de la temporada
    public DateTime? Desde { get; set; }
    // null = hasta hoy
    public DateTime? Hasta { get; set; }
    // Mismo cálculo que resultado del dashboard (con rango: las ventas, consumos, ajustes, gastos y pagos del rango)
    public DashboardResultadoNavDto Resultado { get; set; } = new();
    // Al momento actual
    public DashboardCapitalNavDto Capital { get; set; } = new();
    // Entró/salió del rango (sin fechas: toda la temporada)
    public DashboardFlujoEfectivoNavDto FlujoEfectivo { get; set; } = new();
    // Al momento actual
    public DashboardNosDebenNavDto NosDeben { get; set; } = new();
    public DashboardDeboNavDto Debo { get; set; } = new();
    // Del rango de fechas (sin fechas: toda la temporada)
    public DashboardGastosTarjetaNavDto Gastos { get; set; } = new();
    public DashboardPerdidasNavDto Perdidas { get; set; } = new();
    public DashboardReservasPendientesNavDto ReservasPendientes { get; set; } = new();
    // Siempre de toda la temporada al momento actual (no usa el rango)
    public DashboardConciliacionNavDto Conciliacion { get; set; } = new();
}

// ---------- Ventas ----------

public class ReporteVentasNavFiltrosDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? AlmacenId { get; set; }
    public int? VendedorId { get; set; }
    // 1 Tienda | 2 Ruta
    public int? Canal { get; set; }
    // 1 Directa | 2 Reserva
    public int? Tipo { get; set; }
}

public class ReporteVentasNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    public ReporteVentasNavFiltrosDto Filtros { get; set; } = new();
    // Mismo cálculo que ventas del dashboard. gastos y efectivo: gastos del rango de fechas (no se filtran por almacén, vendedor ni canal)
    public DashboardVentasNavDto Totales { get; set; } = new();
    // Al momento actual; respeta almacén y vendedor, no el rango ni canal/tipo
    public DashboardReservasPendientesNavDto ReservasPendientes { get; set; } = new();
    public List<ReporteVentaDiaNavDto> PorDia { get; set; } = new();
    // Mismo cálculo que tablas.ventasPorVendedor del dashboard (reporte del módulo 8)
    public List<DashboardVentaVendedorNavDto> PorVendedor { get; set; } = new();
    public List<ReporteVentaAlmacenNavDto> PorAlmacen { get; set; } = new();
    public List<ReporteVentaProductoNavDto> PorProducto { get; set; } = new();
    public ReporteVentasProductosTotalesNavDto TotalesProductos { get; set; } = new();
}

public class ReporteVentaDiaNavDto
{
    public DateTime Fecha { get; set; }
    public int CantidadVentas { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Neto { get; set; }
    // Lo cobrado hasta hoy de las ventas de ese día (no es el efectivo que entró ese día)
    public decimal Cobrado { get; set; }
    public decimal PorCobrar { get; set; }
}

public class ReporteVentaAlmacenNavDto
{
    // null = ventas sin almacén (ruta)
    public int? AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public int CantidadVentas { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Neto { get; set; }
    public decimal Cobrado { get; set; }
    public decimal PorCobrar { get; set; }
}

public class ReporteVentaProductoNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
    // Neto con la parte proporcional del descuento de cada venta
    public decimal Neto { get; set; }
    // Costo promedio ponderado del producto en la temporada (0 si no tuvo entradas)
    public decimal CostoPromedio { get; set; }
    // Unidades × costo promedio, redondeado a 2
    public decimal Costo { get; set; }
    // Neto − costo
    public decimal Ganancia { get; set; }
}

public class ReporteVentasProductosTotalesNavDto
{
    public int Unidades { get; set; }
    public decimal Neto { get; set; }
    public decimal Costo { get; set; }
    public decimal Ganancia { get; set; }
}

// ---------- Stock ----------

public class ReporteStockNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    // Columnas de la matriz
    public List<ReporteStockAlmacenNavDto> Almacenes { get; set; } = new();
    // Filas de la matriz
    public List<ReporteStockProductoNavDto> Productos { get; set; } = new();
    public int TotalUnidades { get; set; }
    public decimal TotalValorizado { get; set; }
}

public class ReporteStockAlmacenNavDto
{
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public bool EsTienda { get; set; }
    public int Unidades { get; set; }
    public decimal Valorizado { get; set; }
}

public class ReporteStockProductoNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    // Una celda por almacén, en el mismo orden que "almacenes"
    public List<int> Unidades { get; set; } = new();
    public int Total { get; set; }
    public decimal CostoPromedio { get; set; }
    public decimal Valorizado { get; set; }
}

// ---------- Faltantes ----------

public class ReporteFaltantesNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    // Mismo cálculo que faltaLlegar del dashboard (solo faltante > 0)
    public int TotalUnidades { get; set; }
    public List<ReporteFaltanteCodigoNavDto> PorCodigo { get; set; } = new();
    public List<DashboardFaltanteNavDto> Items { get; set; } = new();
}

public class ReporteFaltanteCodigoNavDto
{
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoTitular { get; set; }
    public int Productos { get; set; }
    public int Pedido { get; set; }
    public int Recibido { get; set; }
    public int Faltante { get; set; }
}

// ---------- Deudas ----------

public class ReporteDeudasNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    public DashboardDeudaProveedoresNavDto Proveedores { get; set; } = new();
    public DashboardDeudaClientesNavDto Clientes { get; set; } = new();
    // Detalle por cliente y venta o reserva; Σ saldoTotal = clientes.total
    public List<ReporteDeudaClienteNavDto> ClientesDetalle { get; set; } = new();
    public DashboardDeudaVendedoresRutaNavDto VendedoresRuta { get; set; } = new();
    public DashboardDeudaInversoresNavDto Inversores { get; set; } = new();
    public List<ResumenAporteCapitalDto> InversoresAportes { get; set; } = new();
    public DashboardSueldosNavDto Sueldos { get; set; } = new();
    public DashboardComisionesNavDto Comisiones { get; set; } = new();
}

public class ReporteDeudaClienteNavDto
{
    // null = ventas con saldo sin cliente asignado
    public int? ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal SaldoTotal { get; set; }
    public List<ReporteDeudaVentaNavDto> Ventas { get; set; } = new();
}

public class ReporteDeudaVentaNavDto
{
    public int VentaId { get; set; }
    public int NroVenta { get; set; }
    public DateTime Fecha { get; set; }
    // Credito (venta directa con saldo) | Reserva
    public string Tipo { get; set; } = string.Empty;
    public decimal Neto { get; set; }
    public decimal Cobrado { get; set; }
    public decimal Saldo { get; set; }
}

// ---------- Gastos ----------

public class ReporteGastosNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? CategoriaId { get; set; }
    public decimal Total { get; set; }
    public int Cantidad { get; set; }
    public List<ReporteGastoCategoriaNavDto> PorCategoria { get; set; } = new();
    // Todos los del filtro, el más reciente primero
    public List<DashboardGastoNavDto> Items { get; set; } = new();
}

public class ReporteGastoCategoriaNavDto
{
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
}

// ---------- Pagos a proveedores ----------

public class ReportePagosProveedoresNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? ProveedorId { get; set; }
    public int? CodigoClienteId { get; set; }
    // Mismo cálculo que pagosProveedores del dashboard (toda la temporada, al momento actual; solo se filtra por proveedor y código)
    public decimal TotalComprado { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal Saldo { get; set; }
    // Una fila por proveedor y código (incluye saldo 0)
    public List<ReportePagoProveedorCodigoNavDto> PorProveedor { get; set; } = new();
    // Σ de los pagos listados (respeta el rango de fechas)
    public decimal TotalPagosListados { get; set; }
    public List<DashboardPagoProveedorNavDto> Pagos { get; set; } = new();
}

public class ReportePagoProveedorCodigoNavDto
{
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoTitular { get; set; }
    public decimal TotalPedidos { get; set; }
    public decimal TotalCompras { get; set; }
    public decimal TotalComprado { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
}
