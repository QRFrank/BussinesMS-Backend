namespace BussinesMS.Aplicacion.DTOs.Navidad;

// Cambios 2026-10-28: reservas pendientes, resultado, capital, flujo de efectivo, posición y conciliación.
// Los calcula ResumenNavCalculos y los comparten el dashboard del admin y Reportes/resultado.

// Reservas (o la parte de una reserva) que todavía no se entregó: NO son venta. Al momento actual
public class DashboardReservasPendientesNavDto
{
    // Reservas con algo por entregar (pendientes o parciales)
    public int Cantidad { get; set; }
    // Neto de lo que falta entregar
    public decimal TotalReservado { get; set; }
    // Dinero ya cobrado de esa parte (no es venta ni "cobrado")
    public decimal Adelantos { get; set; }
    // TotalReservado − Adelantos: lo que se cobra al entregar
    public decimal SaldoPorCobrar { get; set; }
}

public class DashboardResultadoLineaNavDto
{
    // ventasNetas | costoVendido | perdidaConsumo | ajustesNegativosConteo | gastos | sueldosPagados | comisionesRutaPagadas | comisionesInversoresPagadas
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    // +1 suma, −1 resta. El monto siempre es positivo
    public int Signo { get; set; }
    public decimal Monto { get; set; }
    public string Origen { get; set; } = string.Empty;
}

public class DashboardResultadoNavDto
{
    public string CriterioCosto { get; set; } = string.Empty;
    public List<DashboardResultadoLineaNavDto> Lineas { get; set; } = new();
    // Σ signo × monto
    public decimal Ganancia { get; set; }
    public List<string> Advertencias { get; set; } = new();
}

public class DashboardCapitalNavDto
{
    // Todo lo aportado (propio + inversores)
    public decimal Aportes { get; set; }
    public decimal CapitalPropio { get; set; }
    public decimal CapitalInversores { get; set; }
    // Capital devuelto a inversores
    public decimal CapitalDevuelto { get; set; }
    // Comisión ya pagada a inversores
    public decimal ComisionPagada { get; set; }
    // CapitalDevuelto + ComisionPagada
    public decimal PagadoAInversores { get; set; }
    public decimal CapitalPorDevolver { get; set; }
    public decimal ComisionPorPagar { get; set; }
    // CapitalPorDevolver + ComisionPorPagar
    public decimal DeudaInversores { get; set; }
    public decimal DeudaProveedores { get; set; }
    public decimal PorCobrarClientes { get; set; }
    public decimal PorCobrarVendedoresRuta { get; set; }
    // Sueldos que se deben (sueldo × meses − pagado)
    public decimal SueldosPendientes { get; set; }
    // Stock a costo promedio
    public decimal StockValorizado { get; set; }
}

// ----- Tarjeta 1: Efectivo neto ("Efectivo que debés tener") -----

public class DashboardOtrosCobrosNavDto
{
    // Cobros posteriores al primero de una venta de tienda a crédito (abonos)
    public decimal AbonosCredito { get; set; }
    // Todo lo cobrado sobre reservas (adelantos y lo cobrado al entregar)
    public decimal Reservas { get; set; }
    public decimal Total { get; set; }
}

public class DashboardFlujoEntroNavDto
{
    // Lo cobrado al vender en la tienda (primer cobro de cada venta directa de tienda)
    public decimal VentasTienda { get; set; }
    // Lo que rindieron o entregaron los vendedores de ruta (todos los cobros de ventas de ruta)
    public decimal RendicionesRuta { get; set; }
    public DashboardOtrosCobrosNavDto OtrosCobros { get; set; } = new();
    // Aportes de los inversores (línea aparte; es deuda, se devuelve con comisión)
    public decimal AportesInversores { get; set; }
    // Aportes del capital propio
    public decimal AportesCapitalPropio { get; set; }
    public decimal Total { get; set; }
}

public class DashboardFlujoPagosNavDto
{
    public decimal Proveedores { get; set; }
    public decimal Sueldos { get; set; }
    public decimal ComisionesRuta { get; set; }
    public decimal ComisionesInversores { get; set; }
    public decimal DevolucionesCapital { get; set; }
    public decimal Total { get; set; }
}

public class DashboardFlujoSalioNavDto
{
    public decimal Gastos { get; set; }
    public DashboardFlujoPagosNavDto Pagos { get; set; } = new();
    public decimal Total { get; set; }
}

public class DashboardCajaSinEntregarNavDto
{
    public int AlmacenId { get; set; }
    public string AlmacenNombre { get; set; } = string.Empty;
    // Efectivo esperado del turno cerrado y aún sin entrega registrada
    public decimal EfectivoEsperado { get; set; }
}

// Informativo: no cambia el total del flujo
public class DashboardCajasSinEntregarNavDto
{
    public decimal Monto { get; set; }
    public List<DashboardCajaSinEntregarNavDto> Tiendas { get; set; } = new();
}

public class DashboardFlujoEfectivoNavDto
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public DashboardFlujoEntroNavDto Entro { get; set; } = new();
    public DashboardFlujoSalioNavDto Salio { get; set; } = new();
    // Entró − salió: cuánto dinero debería haber
    public decimal Neto { get; set; }
    // Al momento actual (no usa el rango)
    public DashboardCajasSinEntregarNavDto CajasSinEntregar { get; set; } = new();
}

// ----- Tarjeta 2: Nos deben -----

public class DashboardNosDebenNavDto
{
    // Ventas a crédito (y la parte entregada de reservas con saldo): igual a deudas.clientes
    public DashboardDeudaClientesNavDto Clientes { get; set; } = new();
    // Deuda de los vendedores de ruta: igual a deudas.vendedoresRuta
    public DashboardDeudaVendedoresRutaNavDto VendedoresRuta { get; set; } = new();
    // Clientes.Total + VendedoresRuta.Total
    public decimal Total { get; set; }
}

// ----- Tarjeta 3: Debo (solo proveedores) -----

public class DashboardDeboProveedorNavDto
{
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public decimal TotalDeuda { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }
}

public class DashboardDeboNavDto
{
    // Deuda neta con proveedores (pedidos + compras a crédito − pagos): igual a deudas.proveedores.total
    public decimal Total { get; set; }
    public List<DashboardDeboProveedorNavDto> PorProveedor { get; set; } = new();
    // Por proveedor y código de cliente (igual a deudas.proveedores.porCodigo)
    public List<DashboardDeudaProveedorCodigoNavDto> PorCodigo { get; set; } = new();
}

// ----- Tarjeta 4: Gastos -----

public class DashboardGastoCategoriaNavDto
{
    public int CategoriaId { get; set; }
    public string CategoriaNombre { get; set; } = string.Empty;
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
}

public class DashboardGastosOperativosNavDto
{
    public decimal Total { get; set; }
    public int Cantidad { get; set; }
    public List<DashboardGastoCategoriaNavDto> PorCategoria { get; set; } = new();
}

public class DashboardPagosPersonalNavDto
{
    public decimal Sueldos { get; set; }
    public decimal ComisionesRuta { get; set; }
    public decimal Total { get; set; }
}

public class DashboardGastosTarjetaNavDto
{
    public DashboardGastosOperativosNavDto Operativos { get; set; } = new();
    public DashboardPagosPersonalNavDto PagosPersonal { get; set; } = new();
    // Operativos + sueldos + comisiones de ruta
    public decimal Total { get; set; }
}

// ----- Tarjeta 5: Pérdidas -----

public class DashboardPerdidaMotivoNavDto
{
    // Consumos: Propio | Invitacion | Otro. Conteo: MermaRobo | VentaNoRegistrada | Consumo | ErrorConteo | SinMotivo
    public string Motivo { get; set; } = string.Empty;
    public int Unidades { get; set; }
    // A costo promedio
    public decimal Monto { get; set; }
}

public class DashboardPerdidaGrupoNavDto
{
    public int Unidades { get; set; }
    public decimal Monto { get; set; }
    public List<DashboardPerdidaMotivoNavDto> PorMotivo { get; set; } = new();
}

public class DashboardPerdidasNavDto
{
    public DashboardPerdidaGrupoNavDto Consumos { get; set; } = new();
    // Solo diferencias negativas. Monto = línea ajustesNegativosConteo del resultado
    public DashboardPerdidaGrupoNavDto DiferenciasConteo { get; set; } = new();
    public int TotalUnidades { get; set; }
    public decimal TotalMonto { get; set; }
}

public class DashboardConciliacionLineaNavDto
{
    public string Clave { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    // +1 suma, −1 resta (en las causas posibles, solo informativo)
    public int Signo { get; set; }
    public decimal Monto { get; set; }
    public string Origen { get; set; } = string.Empty;
}

public class DashboardPatrimonioEsperadoNavDto
{
    public decimal CapitalPropio { get; set; }
    public decimal Ganancia { get; set; }
    // No hay tabla de retiros del dueño: 0
    public decimal Retiros { get; set; }
    public decimal Total { get; set; }
}

public class DashboardConciliacionNavDto
{
    public DashboardPatrimonioEsperadoNavDto Esperado { get; set; } = new();
    public List<DashboardConciliacionLineaNavDto> RealLineas { get; set; } = new();
    public decimal Real { get; set; }
    // Real − esperado. Ideal 0
    public decimal Diferencia { get; set; }
    // Montos medibles que pueden explicar la diferencia (informativos, no se suman a nada)
    public List<DashboardConciliacionLineaNavDto> PosiblesCausas { get; set; } = new();
    public List<string> Notas { get; set; } = new();
}
