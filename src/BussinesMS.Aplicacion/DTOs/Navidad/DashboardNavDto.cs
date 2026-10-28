namespace BussinesMS.Aplicacion.DTOs.Navidad;

// Dashboard navideño por rol (solo lectura, temporada abierta). Montos en Bs, cantidades en unidades.
// Los DTOs de vendedor y encargado no incluyen costos.

// ---------- Comunes ----------

public class DashboardTemporadaNavDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Anio { get; set; }
    public DateTime FechaInicio { get; set; }
}

public class DashboardAvisoNavDto
{
    // SIN_APERTURA, CIERRE_PENDIENTE, CAJA_PENDIENTE, RESERVAS_PENDIENTES, SALIDAS_ABIERTAS, DEUDA_RUTA, ...
    public string Codigo { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}

public class DashboardConteoTiendaNavDto
{
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    // sinApertura | abierta (falta el cierre de hoy) | cerrada | cierrePendiente (apertura de un día anterior sin cierre)
    public string Estado { get; set; } = string.Empty;
    public DateTime? CierrePendienteFecha { get; set; }
    // Último conteo = Cierre sin entrega de efectivo registrada
    public bool CajaPendiente { get; set; }
}

public class DashboardProductoUnidadesNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
}

// ---------- Perfil ----------

public class DashboardPerfilNavDto
{
    // admin | encargado | vendedorTienda | vendedorRuta | ninguno
    public string Perfil { get; set; } = "ninguno";
    // null si no hay temporada abierta
    public DashboardTemporadaNavDto? Temporada { get; set; }
    public int? VendedorId { get; set; }
    public string? VendedorNombre { get; set; }
    // Solo encargado: almacenes a cargo en la temporada abierta
    public List<int>? AlmacenesACargo { get; set; }
}

// ---------- Admin ----------

public class DashboardAdminNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    public DashboardVentasNavDto Ventas { get; set; } = new();
    public DashboardDeudasNavDto Deudas { get; set; } = new();
    public DashboardFaltaLlegarNavDto FaltaLlegar { get; set; } = new();
    public DashboardPagosProveedoresNavDto PagosProveedores { get; set; } = new();
    public DashboardTablasNavDto Tablas { get; set; } = new();
    public DashboardAlertasNavDto Alertas { get; set; } = new();
    // Cambios 2026-10-28
    public DashboardReservasPendientesNavDto ReservasPendientes { get; set; } = new();
    public DashboardResultadoNavDto Resultado { get; set; } = new();
    public DashboardCapitalNavDto Capital { get; set; } = new();
    public DashboardFlujoEfectivoNavDto FlujoEfectivo { get; set; } = new();
    public DashboardNosDebenNavDto NosDeben { get; set; } = new();
    public DashboardDeboNavDto Debo { get; set; } = new();
    public DashboardGastosTarjetaNavDto Gastos { get; set; } = new();
    public DashboardPerdidasNavDto Perdidas { get; set; } = new();
    public DashboardConciliacionNavDto Conciliacion { get; set; } = new();
}

public class DashboardVentasNavDto
{
    public int CantidadVentas { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Neto { get; set; }
    public decimal Cobrado { get; set; }
    public decimal PorCobrar { get; set; }
    public List<DashboardVentasCanalNavDto> PorCanal { get; set; } = new();
}

public class DashboardVentasCanalNavDto
{
    // Tienda | Ruta
    public string Canal { get; set; } = string.Empty;
    public int CantidadVentas { get; set; }
    public decimal ImporteTotal { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Neto { get; set; }
    public decimal Cobrado { get; set; }
    public decimal PorCobrar { get; set; }
}

public class DashboardDeudasNavDto
{
    public DashboardDeudaProveedoresNavDto Proveedores { get; set; } = new();
    public DashboardDeudaClientesNavDto Clientes { get; set; } = new();
    public DashboardDeudaVendedoresRutaNavDto VendedoresRuta { get; set; } = new();
    public DashboardDeudaInversoresNavDto Inversores { get; set; } = new();
    public DashboardSueldosNavDto Sueldos { get; set; } = new();
    public DashboardComisionesNavDto Comisiones { get; set; } = new();
}

public class DashboardDeudaProveedoresNavDto
{
    public decimal Total { get; set; }
    // Una fila por proveedor (y por código de cliente si usa códigos), con saldo distinto de 0
    public List<DashboardDeudaProveedorCodigoNavDto> PorCodigo { get; set; } = new();
}

public class DashboardDeudaProveedorCodigoNavDto
{
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public string? CodigoTitular { get; set; }
    public decimal TotalDeuda { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
}

public class DashboardDeudaClientesNavDto
{
    public decimal Total { get; set; }
    // Saldo de ventas directas a crédito
    public decimal SaldoCreditos { get; set; }
    // Saldo de reservas
    public decimal SaldoReservas { get; set; }
    public int ClientesConSaldo { get; set; }
}

public class DashboardDeudaVendedoresRutaNavDto
{
    public decimal Total { get; set; }
    public List<DashboardDeudaVendedorRutaNavDto> PorVendedor { get; set; } = new();
}

public class DashboardDeudaVendedorRutaNavDto
{
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public decimal MontoVendido { get; set; }
    public decimal Descuentos { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal Saldo { get; set; }
}

public class DashboardDeudaInversoresNavDto
{
    public decimal CapitalTotal { get; set; }
    public decimal ComisionPorPagar { get; set; }
    public decimal CapitalPorDevolver { get; set; }
    // ComisionPorPagar + CapitalPorDevolver
    public decimal Total { get; set; }
}

public class DashboardSueldosNavDto
{
    // Σ de lo que se debe de sueldos (sueldo × meses − pagado, solo si es positivo)
    public decimal TotalDebe { get; set; }
    public decimal TotalPagado { get; set; }
    public List<DashboardSueldoVendedorNavDto> PorVendedor { get; set; } = new();
}

public class DashboardSueldoVendedorNavDto
{
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public decimal? SueldoMensual { get; set; }
    public int Meses { get; set; }
    public decimal Pagado { get; set; }
    public decimal Debe { get; set; }
}

public class DashboardComisionesNavDto
{
    // Σ de pagos de comisión vigentes
    public decimal TotalPagado { get; set; }
    // Vendedores de ruta con ventas y sin pago de comisión vigente. El monto de la comisión se decide al pagar
    // (por producto o %), por eso se informa lo vendido (neto) y no un monto a pagar
    public List<DashboardComisionPendienteNavDto> Pendientes { get; set; } = new();
}

public class DashboardComisionPendienteNavDto
{
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public int Unidades { get; set; }
    public decimal MontoNetoVendido { get; set; }
}

public class DashboardFaltaLlegarNavDto
{
    public int TotalUnidades { get; set; }
    // Solo filas con faltante > 0, por código y producto
    public List<DashboardFaltanteNavDto> Items { get; set; } = new();
}

public class DashboardFaltanteNavDto
{
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public int? CodigoClienteId { get; set; }
    public string? Codigo { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public int Pedido { get; set; }
    public int Recibido { get; set; }
    public int Faltante { get; set; }
}

public class DashboardPagosProveedoresNavDto
{
    // Pedidos + compras
    public decimal TotalComprado { get; set; }
    public decimal TotalPagado { get; set; }
    public decimal Saldo { get; set; }
    public List<DashboardPagoProveedorNavDto> UltimosPagos { get; set; } = new();
}

public class DashboardPagoProveedorNavDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public decimal Monto { get; set; }
    // Transferencia | Efectivo
    public string Medio { get; set; } = string.Empty;
}

public class DashboardTablasNavDto
{
    public List<DashboardVentaVendedorNavDto> VentasPorVendedor { get; set; } = new();
    public List<DashboardProductoVendidoNavDto> ProductosMasVendidos { get; set; } = new();
    public List<DashboardRecepcionadoVendidoNavDto> RecepcionadoVsVendido { get; set; } = new();
    public DashboardGastosNavDto Gastos { get; set; } = new();
}

public class DashboardVentaVendedorNavDto
{
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    // Tienda | Ruta
    public string Tipo { get; set; } = string.Empty;
    public int CantidadVentas { get; set; }
    public int UnidadesTienda { get; set; }
    public int UnidadesRuta { get; set; }
    public int Unidades { get; set; }
    public decimal MontoBruto { get; set; }
    public decimal Descuentos { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal? MontoCobrado { get; set; }
    public decimal? Saldo { get; set; }
}

public class DashboardProductoVendidoNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Unidades { get; set; }
    // Neto, con la parte proporcional del descuento de cada venta
    public decimal Monto { get; set; }
}

public class DashboardRecepcionadoVendidoNavDto
{
    public int ProductoId { get; set; }
    public string ProductoNombreMostrar { get; set; } = string.Empty;
    public int? UnidadesPorEmpaque { get; set; }
    public string? NombreEmpaque { get; set; }
    public string? Color { get; set; }
    public int Recepcionado { get; set; }
    public int Comprado { get; set; }
    public int Vendido { get; set; }
    // Stock actual en todos los almacenes
    public int EnStock { get; set; }
}

public class DashboardGastosNavDto
{
    public decimal Total { get; set; }
    public int Cantidad { get; set; }
    // Los más recientes (hasta 30)
    public List<DashboardGastoNavDto> Items { get; set; } = new();
}

public class DashboardGastoNavDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public decimal Monto { get; set; }
}

public class DashboardAlertasNavDto
{
    // Tiendas con conteo diario que hoy no están cerradas (sin apertura, abierta sin cierre, o cierre pendiente de un día anterior)
    public List<DashboardConteoTiendaNavDto> Conteos { get; set; } = new();
    // Tiendas cuyo último cierre no tiene la entrega de efectivo registrada
    public List<DashboardConteoTiendaNavDto> CajasSinEntregar { get; set; } = new();
    public List<DashboardSalidaAbiertaNavDto> SalidasAbiertas { get; set; } = new();
}

public class DashboardSalidaAbiertaNavDto
{
    public int SalidaId { get; set; }
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public DateTime FechaApertura { get; set; }
    // Unidades en poder del vendedor (cargado − devuelto), sumando todas sus salidas abiertas
    public int UnidadesEnPoder { get; set; }
}

// ---------- Vendedor ----------

public class DashboardVendedorNavDto
{
    // vendedorTienda | vendedorRuta
    public string Perfil { get; set; } = string.Empty;
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    public int VendedorId { get; set; }
    public string VendedorNombre { get; set; } = string.Empty;
    public List<DashboardAvisoNavDto> Avisos { get; set; } = new();
    // Solo vendedor de tienda
    public DashboardVendedorHoyNavDto? Hoy { get; set; }
    public DashboardVendedorTiendaNavDto? Tienda { get; set; }
    // Solo vendedor de ruta
    public DashboardVendedorRutaNavDto? Ruta { get; set; }
}

public class DashboardVendedorHoyNavDto
{
    public DateTime Fecha { get; set; }
    public int CantidadVentas { get; set; }
    // Monto neto vendido hoy (Bs)
    public decimal Monto { get; set; }
}

public class DashboardVendedorTiendaNavDto
{
    // Tienda de sesión recibida en ?almacenId= (null si no se envió)
    public int? AlmacenId { get; set; }
    public string? AlmacenNombre { get; set; }
    // null si no se envió almacenId o la tienda no lleva conteo
    public DashboardConteoTiendaNavDto? Conteo { get; set; }
    public int ReservasPendientesEntrega { get; set; }
    public decimal ReservasSaldo { get; set; }
}

public class DashboardVendedorRutaNavDto
{
    public int SalidasAbiertas { get; set; }
    public int UnidadesEnPoder { get; set; }
    public DateTime? FechaAperturaMasAntigua { get; set; }
    public decimal MontoVendido { get; set; }
    public decimal MontoCobrado { get; set; }
    // Lo que debe rendir al negocio
    public decimal DebeRendir { get; set; }
    // Registro personal y privado de "me deben" (solo lo ve el propio vendedor)
    public decimal MeDebenTotal { get; set; }
    public int MeDebenClientes { get; set; }
}

// ---------- Encargado ----------

public class DashboardEncargadoNavDto
{
    public DashboardTemporadaNavDto Temporada { get; set; } = new();
    public List<DashboardAlmacenStockNavDto> Almacenes { get; set; } = new();
    public DashboardEncargadoStockNavDto Stock { get; set; } = new();
    public DashboardPorLlegarNavDto PorLlegar { get; set; } = new();
    public DashboardMovimientosHoyNavDto MovimientosHoy { get; set; } = new();
    // Solo almacenes a cargo que llevan conteo diario
    public List<DashboardConteoTiendaNavDto> Conteo { get; set; } = new();
    public List<DashboardAvisoNavDto> Avisos { get; set; } = new();
}

public class DashboardAlmacenStockNavDto
{
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    public bool EsTienda { get; set; }
    public int Unidades { get; set; }
}

public class DashboardEncargadoStockNavDto
{
    public int TotalUnidades { get; set; }
    public int UmbralStockBajo { get; set; }
    public int CantidadAgotados { get; set; }
    public int CantidadPocoStock { get; set; }
    // Productos con stock 0 sumando los almacenes a cargo (hasta 30)
    public List<DashboardProductoUnidadesNavDto> Agotados { get; set; } = new();
    // Productos con 1..umbral unidades sumando los almacenes a cargo, de menor a mayor (hasta 30)
    public List<DashboardProductoUnidadesNavDto> PocoStock { get; set; } = new();
}

public class DashboardPorLlegarNavDto
{
    // false: el pedido no distribuye por almacén, así que se informa lo que falta llegar de toda la temporada
    public bool DistribuidoPorAlmacen { get; set; }
    public string Nota { get; set; } = string.Empty;
    public int TotalUnidades { get; set; }
    // Por producto, de mayor a menor faltante (hasta 30)
    public List<DashboardProductoUnidadesNavDto> Items { get; set; } = new();
}

public class DashboardMovimientosHoyNavDto
{
    public DateTime Fecha { get; set; }
    public DashboardMovimientoResumenNavDto Recepciones { get; set; } = new();
    public DashboardMovimientoResumenNavDto TrasladosEntrantes { get; set; } = new();
    public DashboardMovimientoResumenNavDto TrasladosSalientes { get; set; } = new();
    public DashboardMovimientoResumenNavDto DevolucionesRuta { get; set; } = new();
}

public class DashboardMovimientoResumenNavDto
{
    // Cantidad de documentos de hoy que tocan sus almacenes
    public int Operaciones { get; set; }
    // Unidades que entran o salen de sus almacenes
    public int Unidades { get; set; }
}
