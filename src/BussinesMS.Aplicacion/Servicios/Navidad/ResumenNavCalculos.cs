using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Filtro de ventas compartido por el dashboard (sin filtros) y los reportes. Desde/Hasta = día inclusive.
public class FiltroResumenVentas
{
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int? AlmacenId { get; set; }
    public int? VendedorId { get; set; }
    public CanalVentaNav? Canal { get; set; }
    public TipoVentaNav? Tipo { get; set; }
}

public sealed class LineaVentaResumen
{
    public int ProductoId { get; set; }
    public int Unidades { get; set; }
    public decimal Subtotal { get; set; }
    public int UnidadesEntregadas { get; set; }
}

// Una venta no anulada con su "peso": la fracción que cuenta como venta. Las ventas directas y de ruta pesan 1.
// Una reserva pesa lo entregado: 0 si nada se entregó, 1 si está entregada, y para entregas parciales
// Σ(subtotal × unidades entregadas / unidades) / total. La parte no entregada (1 − peso) es "reserva pendiente".
public sealed class VentaResumen
{
    public int Id { get; set; }
    public int NroVenta { get; set; }
    public DateTime Fecha { get; set; }
    public int? AlmacenId { get; set; }
    public int VendedorId { get; set; }
    public int? ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public CanalVentaNav Canal { get; set; }
    public TipoVentaNav Tipo { get; set; }
    public decimal MontoTotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal MontoNeto { get; set; }
    public decimal MontoCobrado { get; set; }
    public decimal MontoSaldo { get; set; }
    public decimal Peso { get; set; }
    public List<LineaVentaResumen> Lineas { get; set; } = new();

    // Parte que cuenta como venta
    public bool EsVenta => Peso > 0;
    public decimal Importe => ResumenNavCalculos.Parte(MontoTotal, Peso);
    public decimal DescuentoVenta => ResumenNavCalculos.Parte(Descuento, Peso);
    public decimal Neto => ResumenNavCalculos.Parte(MontoNeto, Peso);
    public decimal Cobrado => ResumenNavCalculos.Parte(MontoCobrado, Peso);
    public decimal Saldo => ResumenNavCalculos.Parte(MontoSaldo, Peso);
}

// Cálculos que muestran a la vez el dashboard del admin y los reportes (módulo 9): un solo método por total, para que den los mismos
// números. Sin filtros = temporada abierta completa. Los saldos (deudas, por cobrar) son siempre al momento actual.
public class ResumenNavCalculos
{
    private const string CriterioCostoTexto =
        "Costo promedio ponderado por producto en la temporada: Σ(cantidad × precio de compra) / Σ cantidad de las recepciones y compras no anuladas. " +
        "Es el mismo costo de Stock y Consumos; no depende del rango de fechas";

    private readonly IVentaNavRepository _ventaRepo;
    private readonly IGastoNavRepository _gastoRepo;
    private readonly IPagoProveedorNavRepository _pagoProveedorRepo;
    private readonly IPagoVendedorNavRepository _pagoVendedorRepo;
    private readonly ICobroVentaNavRepository _cobroRepo;
    private readonly IAporteCapitalRepository _aporteRepo;
    private readonly IPagoInversorRepository _pagoInversorRepo;
    private readonly IConsumoNavRepository _consumoRepo;
    private readonly IConteoInventarioNavRepository _conteoRepo;
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly IPedidoNavRepository _pedidoRepo;
    private readonly ITemporadaRepository _temporadaRepo;
    private readonly IConteoNavService _conteo;
    private readonly ICierreCajaNavService _cierreCaja;
    private readonly IStockNavService _stock;
    private readonly IAbastecimientoNavService _abastecimiento;
    private readonly IRutaNavService _ruta;
    private readonly IPagoVendedorNavService _pagosVendedores;
    private readonly IAporteCapitalService _capital;

    private readonly Dictionary<int, List<VentaResumen>> _ventasCache = new();
    private Snapshot? _snapshot;

    public ResumenNavCalculos(
        IVentaNavRepository ventaRepo,
        IGastoNavRepository gastoRepo,
        IPagoProveedorNavRepository pagoProveedorRepo,
        IPagoVendedorNavRepository pagoVendedorRepo,
        ICobroVentaNavRepository cobroRepo,
        IAporteCapitalRepository aporteRepo,
        IPagoInversorRepository pagoInversorRepo,
        IConsumoNavRepository consumoRepo,
        IConteoInventarioNavRepository conteoRepo,
        IStockAlmacenNavRepository stockRepo,
        IPedidoNavRepository pedidoRepo,
        ITemporadaRepository temporadaRepo,
        IConteoNavService conteo,
        ICierreCajaNavService cierreCaja,
        IStockNavService stock,
        IAbastecimientoNavService abastecimiento,
        IRutaNavService ruta,
        IPagoVendedorNavService pagosVendedores,
        IAporteCapitalService capital)
    {
        _ventaRepo = ventaRepo;
        _gastoRepo = gastoRepo;
        _pagoProveedorRepo = pagoProveedorRepo;
        _pagoVendedorRepo = pagoVendedorRepo;
        _cobroRepo = cobroRepo;
        _aporteRepo = aporteRepo;
        _pagoInversorRepo = pagoInversorRepo;
        _consumoRepo = consumoRepo;
        _conteoRepo = conteoRepo;
        _stockRepo = stockRepo;
        _pedidoRepo = pedidoRepo;
        _temporadaRepo = temporadaRepo;
        _conteo = conteo;
        _cierreCaja = cierreCaja;
        _stock = stock;
        _abastecimiento = abastecimiento;
        _ruta = ruta;
        _pagosVendedores = pagosVendedores;
        _capital = capital;
    }

    public static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    // monto × peso; con peso 1 devuelve el monto exacto (sin redondear)
    public static decimal Parte(decimal monto, decimal peso)
        => peso >= 1m ? monto : peso <= 0m ? 0m : Redondear(monto * peso);

    // Fracción de la venta que cuenta como venta (ver VentaResumen). Entregado = 1, nada entregado = 0
    public static decimal CalcularPeso(TipoVentaNav tipo, EstadoEntregaVentaNav estado, decimal montoTotal,
        IEnumerable<(decimal Subtotal, int Unidades, int Entregadas)> lineas)
    {
        if (tipo != TipoVentaNav.Reserva || estado == EstadoEntregaVentaNav.Entregado) return 1m;
        if (estado == EstadoEntregaVentaNav.Pendiente) return 0m;
        if (montoTotal <= 0) return 0m;
        var entregado = lineas.Where(l => l.Unidades > 0).Sum(l => l.Subtotal * Math.Min(l.Entregadas, l.Unidades) / l.Unidades);
        return Math.Max(0m, Math.Min(1m, entregado / montoTotal));
    }

    // ================= Ventas =================

    // Todas las ventas no anuladas de la temporada (incluye reservas con peso 0), con los filtros opcionales. Una carga por petición
    public async Task<List<VentaResumen>> CargarVentasAsync(int temporadaId, FiltroResumenVentas? f = null)
    {
        if (!_ventasCache.TryGetValue(temporadaId, out var todas))
        {
            var filas = await _ventaRepo.AsQueryable().AsNoTracking()
                .Where(v => v.IsActive && !v.Anulada && v.TemporadaId == temporadaId)
                .Select(v => new
                {
                    v.Id,
                    v.NroVenta,
                    v.Fecha,
                    v.AlmacenId,
                    v.VendedorId,
                    v.ClienteId,
                    ClienteNombre = v.Cliente != null ? v.Cliente.Nombre : null,
                    v.Canal,
                    v.Tipo,
                    v.EstadoEntrega,
                    v.MontoTotal,
                    v.Descuento,
                    v.MontoNeto,
                    v.MontoCobrado,
                    v.MontoSaldo,
                    Lineas = v.Detalles.Where(d => d.Unidades > 0)
                        .Select(d => new LineaVentaResumen { ProductoId = d.ProductoId, Unidades = d.Unidades, Subtotal = d.Subtotal, UnidadesEntregadas = d.UnidadesEntregadas })
                        .ToList()
                })
                .ToListAsync();

            todas = filas.Select(v => new VentaResumen
            {
                Id = v.Id,
                NroVenta = v.NroVenta,
                Fecha = v.Fecha,
                AlmacenId = v.AlmacenId,
                VendedorId = v.VendedorId,
                ClienteId = v.ClienteId,
                ClienteNombre = v.ClienteNombre,
                Canal = v.Canal,
                Tipo = v.Tipo,
                MontoTotal = v.MontoTotal,
                Descuento = v.Descuento,
                MontoNeto = v.MontoNeto,
                MontoCobrado = v.MontoCobrado,
                MontoSaldo = v.MontoSaldo,
                Lineas = v.Lineas,
                Peso = CalcularPeso(v.Tipo, v.EstadoEntrega, v.MontoTotal, v.Lineas.Select(l => (l.Subtotal, l.Unidades, l.UnidadesEntregadas)))
            }).ToList();
            _ventasCache[temporadaId] = todas;
        }

        return f == null ? todas : todas.Where(v => Coincide(v, f)).ToList();
    }

    private static bool Coincide(VentaResumen v, FiltroResumenVentas f)
    {
        if (f.Desde.HasValue && v.Fecha < f.Desde.Value.Date) return false;
        if (f.Hasta.HasValue && v.Fecha >= f.Hasta.Value.Date.AddDays(1)) return false;
        if (f.AlmacenId.HasValue && v.AlmacenId != f.AlmacenId.Value) return false;
        if (f.VendedorId.HasValue && v.VendedorId != f.VendedorId.Value) return false;
        if (f.Canal.HasValue && v.Canal != f.Canal.Value) return false;
        if (f.Tipo.HasValue && v.Tipo != f.Tipo.Value) return false;
        return true;
    }

    public async Task<DashboardVentasNavDto> ObtenerVentasAsync(int temporadaId, FiltroResumenVentas? filtro = null)
    {
        var ventas = (await CargarVentasAsync(temporadaId, filtro)).Where(v => v.EsVenta).ToList();

        var porCanal = ventas
            .GroupBy(v => v.Canal)
            .OrderBy(g => g.Key)
            .Select(g => new DashboardVentasCanalNavDto
            {
                Canal = g.Key == CanalVentaNav.Ruta ? "Ruta" : "Tienda",
                CantidadVentas = g.Count(),
                ImporteTotal = g.Sum(x => x.Importe),
                Descuentos = g.Sum(x => x.DescuentoVenta),
                Neto = g.Sum(x => x.Neto),
                Cobrado = g.Sum(x => x.Cobrado),
                PorCobrar = g.Sum(x => x.Saldo)
            })
            .ToList();

        return new DashboardVentasNavDto
        {
            CantidadVentas = porCanal.Sum(c => c.CantidadVentas),
            ImporteTotal = porCanal.Sum(c => c.ImporteTotal),
            Descuentos = porCanal.Sum(c => c.Descuentos),
            Neto = porCanal.Sum(c => c.Neto),
            Cobrado = porCanal.Sum(c => c.Cobrado),
            PorCobrar = porCanal.Sum(c => c.PorCobrar),
            PorCanal = porCanal
        };
    }

    // Unidades entregadas y neto (con la parte proporcional del descuento de cada venta) por producto
    public async Task<List<(int ProductoId, int Unidades, decimal Neto)>> ObtenerVendidoPorProductoAsync(int temporadaId, FiltroResumenVentas? filtro = null)
    {
        var ventas = (await CargarVentasAsync(temporadaId, filtro)).Where(v => v.EsVenta);
        var filas = new Dictionary<int, (int Unidades, decimal Neto)>();
        foreach (var v in ventas)
            foreach (var l in v.Lineas)
            {
                var unidades = v.Tipo == TipoVentaNav.Reserva ? Math.Min(l.UnidadesEntregadas, l.Unidades) : l.Unidades;
                if (unidades <= 0) continue;
                var neto = (v.MontoTotal > 0 ? l.Subtotal * v.MontoNeto / v.MontoTotal : l.Subtotal) * unidades / l.Unidades;
                var f = filas.GetValueOrDefault(l.ProductoId);
                filas[l.ProductoId] = (f.Unidades + unidades, f.Neto + neto);
            }
        return filas.Select(kv => (kv.Key, kv.Value.Unidades, kv.Value.Neto)).ToList();
    }

    // Reservas con algo sin entregar: no son venta. Respeta almacén y vendedor del filtro; no usa fechas, canal ni tipo (es un saldo al momento actual)
    public async Task<DashboardReservasPendientesNavDto> ObtenerReservasPendientesAsync(int temporadaId, FiltroResumenVentas? filtro = null)
    {
        var f = filtro == null ? null : new FiltroResumenVentas { AlmacenId = filtro.AlmacenId, VendedorId = filtro.VendedorId };
        var pendientes = (await CargarVentasAsync(temporadaId, f)).Where(v => v.Tipo == TipoVentaNav.Reserva && v.Peso < 1m).ToList();
        var reservado = pendientes.Sum(v => ParteNoEntregada(v.MontoNeto, v.Peso));
        var adelantos = pendientes.Sum(v => ParteNoEntregada(v.MontoCobrado, v.Peso));
        return new DashboardReservasPendientesNavDto
        {
            Cantidad = pendientes.Count,
            TotalReservado = reservado,
            Adelantos = adelantos,
            SaldoPorCobrar = reservado - adelantos
        };
    }

    private static decimal ParteNoEntregada(decimal monto, decimal peso) => monto - Parte(monto, peso);

    // ================= Gastos =================

    // maxItems null = todos. Sin fechas = toda la temporada
    public async Task<DashboardGastosNavDto> ObtenerGastosAsync(int temporadaId, int? maxItems = 30, DateTime? desde = null, DateTime? hasta = null, int? categoriaId = null)
    {
        var query = _gastoRepo.AsQueryable().AsNoTracking().Where(g => g.IsActive && g.TemporadaId == temporadaId);
        if (desde.HasValue)
        {
            var d = desde.Value.Date;
            query = query.Where(g => g.Fecha >= d);
        }
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1);
            query = query.Where(g => g.Fecha < h);
        }
        if (categoriaId.HasValue)
            query = query.Where(g => g.CategoriaId == categoriaId.Value);

        var total = await query.SumAsync(g => (decimal?)g.Monto) ?? 0m;
        var cantidad = await query.CountAsync();
        var ordenada = query.OrderByDescending(g => g.Fecha).ThenByDescending(g => g.Id);
        var items = await (maxItems.HasValue ? ordenada.Take(maxItems.Value) : ordenada)
            .Select(g => new DashboardGastoNavDto
            {
                Id = g.Id,
                Fecha = g.Fecha,
                CategoriaNombre = g.Categoria != null ? g.Categoria.Nombre : string.Empty,
                Descripcion = g.Descripcion,
                Monto = g.Monto
            })
            .ToListAsync();

        return new DashboardGastosNavDto { Total = total, Cantidad = cantidad, Items = items };
    }

    // ================= Deudas =================

    public DashboardDeudaProveedoresNavDto ConstruirDeudaProveedores(List<DeudaProveedorNavDto> deudas)
        => new()
        {
            Total = deudas.Sum(d => d.Saldo),
            PorCodigo = deudas
                .Where(d => d.Saldo != 0)
                .OrderByDescending(d => d.Saldo)
                .Select(d => new DashboardDeudaProveedorCodigoNavDto
                {
                    ProveedorId = d.ProveedorId,
                    ProveedorNombre = d.ProveedorNombre,
                    CodigoClienteId = d.CodigoClienteId,
                    Codigo = d.Codigo,
                    CodigoTitular = d.CodigoTitular,
                    TotalDeuda = d.TotalDeuda,
                    Pagado = d.TotalPagado,
                    Saldo = d.Saldo
                })
                .ToList()
        };

    // Ventas de tienda que cuentan como venta (las reservas, solo la parte entregada) con saldo por cobrar. La deuda de la ruta es del vendedor
    public async Task<List<VentaResumen>> ObtenerVentasTiendaConSaldoAsync(int temporadaId)
        => (await CargarVentasAsync(temporadaId, new FiltroResumenVentas { Canal = CanalVentaNav.Tienda }))
            .Where(v => v.EsVenta && v.Saldo > 0)
            .ToList();

    // Clientes: crédito = venta directa con saldo; reservas = saldo de la parte ya entregada de reservas (el saldo de lo no entregado va en reservas pendientes)
    public async Task<DashboardDeudaClientesNavDto> ObtenerDeudaClientesAsync(int temporadaId)
    {
        var ventas = await ObtenerVentasTiendaConSaldoAsync(temporadaId);
        var creditos = ventas.Where(v => v.Tipo == TipoVentaNav.Directa).Sum(v => v.Saldo);
        var reservas = ventas.Where(v => v.Tipo == TipoVentaNav.Reserva).Sum(v => v.Saldo);
        return new DashboardDeudaClientesNavDto
        {
            Total = creditos + reservas,
            SaldoCreditos = creditos,
            SaldoReservas = reservas,
            ClientesConSaldo = ventas.Where(v => v.ClienteId != null).Select(v => v.ClienteId).Distinct().Count()
        };
    }

    public DashboardDeudaVendedoresRutaNavDto ConstruirDeudaRuta(List<DeudaVendedorRutaDto> deudas)
        => new()
        {
            Total = deudas.Sum(d => d.Saldo),
            PorVendedor = deudas
                .Where(d => d.Saldo != 0)
                .OrderByDescending(d => d.Saldo)
                .Select(d => new DashboardDeudaVendedorRutaNavDto
                {
                    VendedorId = d.VendedorId,
                    VendedorNombre = d.VendedorNombre,
                    MontoVendido = d.MontoVendido,
                    Descuentos = d.Descuentos,
                    MontoCobrado = d.MontoCobrado,
                    Saldo = d.Saldo
                })
                .ToList()
        };

    public DashboardDeudaInversoresNavDto ConstruirDeudaInversores(ResumenCapitalDto capital)
        => new()
        {
            CapitalTotal = capital.CapitalTotal,
            ComisionPorPagar = capital.ComisionPorPagar,
            CapitalPorDevolver = capital.CapitalPorDevolver,
            Total = capital.ComisionPorPagar + capital.CapitalPorDevolver
        };

    public DashboardSueldosNavDto ConstruirSueldos(List<SueldoVendedorResumenDto> sueldos)
        => new()
        {
            TotalPagado = sueldos.Sum(s => s.TotalPagado),
            TotalDebe = sueldos.Where(s => s.Debe.HasValue && s.Debe.Value > 0).Sum(s => s.Debe!.Value),
            PorVendedor = sueldos
                .Where(s => s.Debe.HasValue && s.Debe.Value > 0)
                .OrderByDescending(s => s.Debe)
                .Select(s => new DashboardSueldoVendedorNavDto
                {
                    VendedorId = s.VendedorId,
                    VendedorNombre = s.VendedorNombre,
                    SueldoMensual = s.SueldoMensual,
                    Meses = s.Meses,
                    Pagado = s.TotalPagado,
                    Debe = s.Debe!.Value
                })
                .ToList()
        };

    // Σ de pagos de comisión de ruta vigentes (con fechas, solo los del rango)
    public async Task<decimal> ObtenerComisionesPagadasAsync(int temporadaId, DateTime? desde = null, DateTime? hasta = null)
    {
        var query = _pagoVendedorRepo.AsQueryable().AsNoTracking()
            .Where(p => p.IsActive && !p.Anulado && p.TemporadaId == temporadaId && p.Tipo == TipoPagoVendedor.Comision);
        query = FiltrarFechaPago(query, desde, hasta);
        return await query.SumAsync(p => (decimal?)p.Monto) ?? 0m;
    }

    // Σ de pagos de sueldo vigentes de los vendedores de tienda activos (el mismo conjunto del resumen de sueldos)
    public async Task<decimal> ObtenerSueldosPagadosAsync(int temporadaId, DateTime? desde = null, DateTime? hasta = null)
    {
        var query = _pagoVendedorRepo.AsQueryable().AsNoTracking()
            .Where(p => p.IsActive && !p.Anulado && p.TemporadaId == temporadaId && p.Tipo == TipoPagoVendedor.Sueldo
                        && p.Vendedor != null && p.Vendedor.IsActive && p.Vendedor.Tipo == TipoVendedor.Tienda);
        query = FiltrarFechaPago(query, desde, hasta);
        return await query.SumAsync(p => (decimal?)p.Monto) ?? 0m;
    }

    private static IQueryable<PagoVendedor> FiltrarFechaPago(IQueryable<PagoVendedor> query, DateTime? desde, DateTime? hasta)
    {
        if (desde.HasValue)
        {
            var d = desde.Value.Date;
            query = query.Where(p => p.Fecha >= d);
        }
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1);
            query = query.Where(p => p.Fecha < h);
        }
        return query;
    }

    public async Task<DashboardComisionesNavDto> ConstruirComisionesAsync(int temporadaId, List<ReporteVentasVendedorDto> reporte)
    {
        var pagado = await ObtenerComisionesPagadasAsync(temporadaId);

        return new DashboardComisionesNavDto
        {
            TotalPagado = pagado,
            Pendientes = reporte
                .Where(r => r.Tipo == (int)TipoVendedor.Ruta && r.PagoComisionVigenteId == null && r.Totales.Unidades > 0)
                .OrderByDescending(r => r.Totales.MontoNeto)
                .Select(r => new DashboardComisionPendienteNavDto
                {
                    VendedorId = r.VendedorId,
                    VendedorNombre = r.VendedorNombre,
                    Unidades = r.Totales.Unidades,
                    MontoNetoVendido = r.Totales.MontoNeto
                })
                .ToList()
        };
    }

    // ================= Abastecimiento =================

    public DashboardFaltaLlegarNavDto ConstruirFaltaLlegar(List<FaltanteNavDto> faltantes)
    {
        var pendientes = faltantes.Where(f => f.Faltante > 0).ToList();
        return new DashboardFaltaLlegarNavDto
        {
            TotalUnidades = pendientes.Sum(f => f.Faltante),
            Items = pendientes
                .Select(f => new DashboardFaltanteNavDto
                {
                    ProveedorId = f.ProveedorId,
                    ProveedorNombre = f.ProveedorNombre,
                    CodigoClienteId = f.CodigoClienteId,
                    Codigo = f.Codigo,
                    ProductoId = f.ProductoId,
                    ProductoNombreMostrar = f.ProductoNombreMostrar,
                    UnidadesPorEmpaque = f.UnidadesPorEmpaque,
                    NombreEmpaque = f.NombreEmpaque,
                    Pedido = f.Pedido,
                    Recibido = f.Recibido,
                    Faltante = f.Faltante
                })
                .ToList()
        };
    }

    // Totales de comprado/pagado/saldo (de las deudas) y los últimos pagos (los 5 más recientes en el dashboard; con filtros, los del filtro)
    public async Task<DashboardPagosProveedoresNavDto> ConstruirPagosProveedoresAsync(
        int temporadaId, List<DeudaProveedorNavDto> deudas, int? maxPagos = 5,
        DateTime? desde = null, DateTime? hasta = null, int? proveedorId = null, int? codigoClienteId = null)
    {
        var query = _pagoProveedorRepo.AsQueryable().AsNoTracking()
            .Where(p => p.IsActive && p.TemporadaId == temporadaId);
        if (desde.HasValue)
        {
            var d = desde.Value.Date;
            query = query.Where(p => p.Fecha >= d);
        }
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1);
            query = query.Where(p => p.Fecha < h);
        }
        if (proveedorId.HasValue) query = query.Where(p => p.ProveedorId == proveedorId.Value);
        if (codigoClienteId.HasValue) query = query.Where(p => p.CodigoClienteId == codigoClienteId.Value);

        var ordenada = query.OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id);
        var ultimos = await (maxPagos.HasValue ? ordenada.Take(maxPagos.Value) : ordenada)
            .Select(p => new
            {
                p.Id,
                p.Fecha,
                Proveedor = p.Proveedor != null ? p.Proveedor.Nombre : string.Empty,
                Codigo = p.CodigoCliente != null ? p.CodigoCliente.Codigo : null,
                p.Monto,
                p.Medio
            })
            .ToListAsync();

        return new DashboardPagosProveedoresNavDto
        {
            TotalComprado = deudas.Sum(d => d.TotalDeuda),
            TotalPagado = deudas.Sum(d => d.TotalPagado),
            Saldo = deudas.Sum(d => d.Saldo),
            UltimosPagos = ultimos
                .Select(p => new DashboardPagoProveedorNavDto
                {
                    Id = p.Id,
                    Fecha = p.Fecha,
                    ProveedorNombre = p.Proveedor,
                    Codigo = p.Codigo,
                    Monto = p.Monto,
                    Medio = p.Medio == MedioPagoNav.Transferencia ? "Transferencia" : "Efectivo"
                })
                .ToList()
        };
    }

    // ================= Ventas por vendedor (reporte del módulo 8) =================

    public List<DashboardVentaVendedorNavDto> ConstruirVentasPorVendedor(List<ReporteVentasVendedorDto> reporte)
        => reporte
            .OrderByDescending(r => r.Totales.MontoNeto)
            .Select(r => new DashboardVentaVendedorNavDto
            {
                VendedorId = r.VendedorId,
                VendedorNombre = r.VendedorNombre,
                Tipo = r.TipoNombre,
                CantidadVentas = r.Totales.CantidadVentas,
                UnidadesTienda = r.Totales.UnidadesTienda,
                UnidadesRuta = r.Totales.UnidadesRuta,
                Unidades = r.Totales.Unidades,
                MontoBruto = r.Totales.MontoBruto,
                Descuentos = r.Totales.Descuentos,
                MontoNeto = r.Totales.MontoNeto,
                MontoCobrado = r.MontoCobrado,
                Saldo = r.Saldo
            })
            .ToList();

    // ================= Stock =================

    // Σ stock × costo promedio ponderado (sin redondear cada fila), redondeado a 2
    public async Task<decimal> ObtenerStockValorizadoAsync(int temporadaId, int? almacenId = null, int? proveedorId = null)
    {
        var query = _stockRepo.AsQueryable().AsNoTracking().Where(s => s.IsActive && s.TemporadaId == temporadaId);
        if (almacenId.HasValue) query = query.Where(s => s.AlmacenId == almacenId.Value);
        if (proveedorId.HasValue) query = query.Where(s => s.Producto!.ProveedorId == proveedorId.Value);
        var filas = await query.GroupBy(s => s.ProductoId).Select(g => new { ProductoId = g.Key, Unidades = g.Sum(s => s.Cantidad) }).ToListAsync();
        var costos = await _stock.ObtenerCostosPromedioAsync(temporadaId, filas.Select(f => f.ProductoId));
        return Redondear(filas.Sum(f => f.Unidades * costos.GetValueOrDefault(f.ProductoId)));
    }

    // ================= Resultado (ganancia) =================

    private IQueryable<T> FiltrarFecha<T>(IQueryable<T> query, DateTime? desde, DateTime? hasta,
        System.Linq.Expressions.Expression<Func<T, DateTime>> fecha)
    {
        if (desde.HasValue)
        {
            var d = desde.Value.Date;
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.GreaterThanOrEqual(fecha.Body, System.Linq.Expressions.Expression.Constant(d)), fecha.Parameters));
        }
        if (hasta.HasValue)
        {
            var h = hasta.Value.Date.AddDays(1);
            query = query.Where(System.Linq.Expressions.Expression.Lambda<Func<T, bool>>(
                System.Linq.Expressions.Expression.LessThan(fecha.Body, System.Linq.Expressions.Expression.Constant(h)), fecha.Parameters));
        }
        return query;
    }

    // Ajustes de conteo (apertura y cierre) valorizados: (faltantes, sobrantes)
    private async Task<(decimal Negativos, decimal Positivos)> ObtenerAjustesConteoAsync(
        int temporadaId, DateTime? desde, DateTime? hasta, Dictionary<int, decimal> costos)
    {
        var conteoQuery = FiltrarFecha(
            _conteoRepo.AsQueryable().AsNoTracking().Where(c => c.IsActive && c.TemporadaId == temporadaId),
            desde, hasta, c => c.Fecha);
        var difs = await conteoQuery
            .SelectMany(c => c.Detalles)
            .Where(d => d.Diferencia != 0)
            .GroupBy(d => new { d.ProductoId, Negativa = d.Diferencia < 0 })
            .Select(g => new { g.Key.ProductoId, g.Key.Negativa, Unidades = g.Sum(d => d.Diferencia) })
            .ToListAsync();
        var neg = difs.Where(x => x.Negativa).Sum(x => Redondear(-x.Unidades * costos.GetValueOrDefault(x.ProductoId)));
        var pos = difs.Where(x => !x.Negativa).Sum(x => Redondear(x.Unidades * costos.GetValueOrDefault(x.ProductoId)));
        return (neg, pos);
    }

    private static DashboardResultadoLineaNavDto Linea(string clave, string nombre, int signo, decimal monto, string origen) => new()
    {
        Clave = clave,
        Nombre = nombre,
        Signo = signo,
        Monto = Redondear(monto),
        Origen = origen
    };

    // Ganancia: ventas netas (sin reservas no entregadas) − costo − consumo − ajustes − gastos − sueldos, comisiones e inversores pagados
    public async Task<DashboardResultadoNavDto> ObtenerResultadoAsync(int temporadaId, DateTime? desde, DateTime? hasta)
    {
        var costos = await _stock.ObtenerCostosPromedioAsync(temporadaId);
        var advertencias = new List<string>();

        var filtro = new FiltroResumenVentas { Desde = desde, Hasta = hasta };
        var ventas = await ObtenerVentasAsync(temporadaId, filtro);
        var vendido = await ObtenerVendidoPorProductoAsync(temporadaId, filtro);
        var costoVendido = vendido.Sum(x => Redondear(x.Unidades * costos.GetValueOrDefault(x.ProductoId)));
        var sinCosto = vendido.Count(x => !costos.ContainsKey(x.ProductoId));
        if (sinCosto > 0)
            advertencias.Add($"{sinCosto} producto(s) vendidos no tienen costo promedio (sin recepciones ni compras): su costo cuenta como 0");

        var perdidas = await ObtenerPerdidasAsync(temporadaId, desde, hasta);
        var perdidaConsumo = perdidas.Consumos.Monto;
        var ajustesNegativos = perdidas.DiferenciasConteo.Monto;

        var gastos = (await ObtenerGastosAsync(temporadaId, 0, desde, hasta)).Total;
        var sueldos = await ObtenerSueldosPagadosAsync(temporadaId, desde, hasta);
        var comisionesRuta = await ObtenerComisionesPagadasAsync(temporadaId, desde, hasta);
        var comisionesInversores = await FiltrarFecha(
                _pagoInversorRepo.AsQueryable().AsNoTracking()
                    .Where(p => p.IsActive && p.TemporadaId == temporadaId && p.Tipo == TipoPagoInversor.Comision),
                desde, hasta, p => p.Fecha)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

        var lineas = new List<DashboardResultadoLineaNavDto>
        {
            Linea("ventasNetas", "Ventas netas", 1, ventas.Neto, "Σ monto neto de las ventas no anuladas; las reservas cuentan solo por lo entregado; igual a ventas.neto del dashboard"),
            Linea("costoVendido", "Costo de lo vendido", -1, costoVendido, "Σ unidades vendidas (reservas: las entregadas) × costo promedio, por producto"),
            Linea("perdidaConsumo", "Pérdida por consumo", -1, perdidaConsumo, "Σ pérdidas por consumo de la tarjeta Pérdidas (unidades × costo promedio)"),
            Linea("ajustesNegativosConteo", "Ajustes negativos de conteo", -1, ajustesNegativos, "Σ diferencias negativas de conteo de la tarjeta Pérdidas (unidades × costo promedio)"),
            Linea("gastos", "Gastos", -1, gastos, "Σ gastos activos; igual a tablas.gastos.total del dashboard"),
            Linea("sueldosPagados", "Sueldos pagados", -1, sueldos, "Σ pagos de sueldo vigentes; igual a deudas.sueldos.totalPagado del dashboard"),
            Linea("comisionesRutaPagadas", "Comisiones de ruta pagadas", -1, comisionesRuta, "Σ pagos de comisión de vendedores vigentes; igual a deudas.comisiones.totalPagado del dashboard"),
            Linea("comisionesInversoresPagadas", "Comisiones de inversores pagadas", -1, comisionesInversores, "Σ pagos de tipo comisión a inversores. Lo que aún no se pagó está en capital.comisionPorPagar")
        };

        return new DashboardResultadoNavDto
        {
            CriterioCosto = CriterioCostoTexto,
            Lineas = lineas,
            Ganancia = lineas.Sum(l => l.Signo * l.Monto),
            Advertencias = advertencias
        };
    }

    // ================= Pérdidas =================

    // Consumos (por motivo) y diferencias negativas de conteo (por motivo), en unidades y en Bs a costo promedio.
    // Cada monto = Σ por producto de redondear(unidades × costo promedio). Los totales son los que restan en la ganancia
    public async Task<DashboardPerdidasNavDto> ObtenerPerdidasAsync(int temporadaId, DateTime? desde, DateTime? hasta)
    {
        var costos = await _stock.ObtenerCostosPromedioAsync(temporadaId);

        var consumos = await FiltrarFecha(
                _consumoRepo.AsQueryable().AsNoTracking().Where(c => c.IsActive && !c.Anulado && c.TemporadaId == temporadaId),
                desde, hasta, c => c.Fecha)
            .SelectMany(c => c.Detalles.Select(d => new { c.Motivo, d.ProductoId, d.Unidades }))
            .GroupBy(x => new { x.Motivo, x.ProductoId })
            .Select(g => new { g.Key.Motivo, g.Key.ProductoId, Unidades = g.Sum(x => x.Unidades) })
            .ToListAsync();

        var conteos = await FiltrarFecha(
                _conteoRepo.AsQueryable().AsNoTracking().Where(c => c.IsActive && c.TemporadaId == temporadaId),
                desde, hasta, c => c.Fecha)
            .SelectMany(c => c.Detalles)
            .Where(d => d.Diferencia < 0)
            .GroupBy(d => new { d.MotivoDiferencia, d.ProductoId })
            .Select(g => new { g.Key.MotivoDiferencia, g.Key.ProductoId, Unidades = -g.Sum(d => d.Diferencia) })
            .ToListAsync();

        DashboardPerdidaMotivoNavDto Motivo(string nombre, IEnumerable<(int ProductoId, int Unidades)> filas)
        {
            var lista = filas.ToList();
            return new DashboardPerdidaMotivoNavDto
            {
                Motivo = nombre,
                Unidades = lista.Sum(x => x.Unidades),
                Monto = lista.Sum(x => Redondear(x.Unidades * costos.GetValueOrDefault(x.ProductoId)))
            };
        }

        DashboardPerdidaGrupoNavDto Grupo(List<DashboardPerdidaMotivoNavDto> motivos) => new()
        {
            Unidades = motivos.Sum(m => m.Unidades),
            Monto = motivos.Sum(m => m.Monto),
            PorMotivo = motivos
        };

        var porConsumo = Grupo(new[] { MotivoConsumoNav.Propio, MotivoConsumoNav.Invitacion, MotivoConsumoNav.Otro }
            .Select(m => Motivo(m.ToString(), consumos.Where(x => x.Motivo == m).Select(x => (x.ProductoId, x.Unidades))))
            .ToList());

        var motivosConteo = new List<DashboardPerdidaMotivoNavDto>();
        foreach (var m in new[] { MotivoDiferenciaConteo.MermaRobo, MotivoDiferenciaConteo.VentaNoRegistrada, MotivoDiferenciaConteo.Consumo, MotivoDiferenciaConteo.ErrorConteo })
            motivosConteo.Add(Motivo(m.ToString(), conteos.Where(x => x.MotivoDiferencia == m).Select(x => (x.ProductoId, x.Unidades))));
        motivosConteo.Add(Motivo("SinMotivo", conteos.Where(x => x.MotivoDiferencia == null).Select(x => (x.ProductoId, x.Unidades))));
        var porConteo = Grupo(motivosConteo);

        return new DashboardPerdidasNavDto
        {
            Consumos = porConsumo,
            DiferenciasConteo = porConteo,
            TotalUnidades = porConsumo.Unidades + porConteo.Unidades,
            TotalMonto = porConsumo.Monto + porConteo.Monto
        };
    }

    // ================= Gastos (tarjeta) =================

    // Gastos operativos por categoría + pagos de sueldos y comisiones de ruta
    public async Task<DashboardGastosTarjetaNavDto> ObtenerGastosTarjetaAsync(int temporadaId, DateTime? desde, DateTime? hasta, int? categoriaId = null)
    {
        var query = _gastoRepo.AsQueryable().AsNoTracking().Where(g => g.IsActive && g.TemporadaId == temporadaId);
        query = FiltrarFecha(query, desde, hasta, g => g.Fecha);
        if (categoriaId.HasValue) query = query.Where(g => g.CategoriaId == categoriaId.Value);

        var porCategoria = (await query
                .GroupBy(g => new { g.CategoriaId, Nombre = g.Categoria != null ? g.Categoria.Nombre : string.Empty })
                .Select(g => new DashboardGastoCategoriaNavDto { CategoriaId = g.Key.CategoriaId, CategoriaNombre = g.Key.Nombre, Cantidad = g.Count(), Total = g.Sum(x => x.Monto) })
                .ToListAsync())
            .OrderByDescending(c => c.Total).ToList();

        var operativos = new DashboardGastosOperativosNavDto
        {
            Total = porCategoria.Sum(c => c.Total),
            Cantidad = porCategoria.Sum(c => c.Cantidad),
            PorCategoria = porCategoria
        };
        var personal = new DashboardPagosPersonalNavDto
        {
            Sueldos = await ObtenerSueldosPagadosAsync(temporadaId, desde, hasta),
            ComisionesRuta = await ObtenerComisionesPagadasAsync(temporadaId, desde, hasta)
        };
        personal.Total = personal.Sueldos + personal.ComisionesRuta;
        return new DashboardGastosTarjetaNavDto { Operativos = operativos, PagosPersonal = personal, Total = operativos.Total + personal.Total };
    }

    // ================= Flujo de efectivo =================

    // Entró − salió. Cada línea sale de una sola tabla, sin sumar dos veces el mismo dinero:
    // - CobroVenta (no anulado, de ventas no anuladas) es TODO el dinero que entra por ventas y reservas: el cobro al vender, los abonos de créditos,
    //   los adelantos de reservas y, en la ruta, la liquidación al cerrar la salida y los abonos/rendiciones de los vendedores (AbonoVendedor genera un
    //   CobroVenta por cada venta que cubre, por eso AbonoVendedor / AbonoVendedorAplicacion NO se suman aparte). Se reparte así:
    //   venta de ruta = rendicionesRuta; reserva = otrosCobros.reservas; venta directa de tienda: el primer cobro (por id) = ventasTienda y los siguientes = abonosCredito.
    // - AporteCapital (inversor o propio), PagoProveedor (incluye el pago automático de una compra al contado), GastoNav,
    //   PagoVendedor (sueldo y comisión) y PagoInversor (comisión y devolución de capital).
    // - VentaClienteRuta / CobroClienteRuta (cuaderno privado del vendedor) no son dinero del negocio y no entran.
    public async Task<DashboardFlujoEfectivoNavDto> ObtenerFlujoEfectivoAsync(int temporadaId, DateTime? desde, DateTime? hasta)
    {
        var ventas = (await CargarVentasAsync(temporadaId)).ToDictionary(v => v.Id);

        var todosCobros = await _cobroRepo.AsQueryable().AsNoTracking()
            .Where(c => c.IsActive && !c.Anulado && c.Venta != null && c.Venta.IsActive && !c.Venta.Anulada && c.Venta.TemporadaId == temporadaId)
            .Select(c => new { c.Id, c.VentaId, c.Monto, c.Fecha })
            .ToListAsync();
        var primerCobro = todosCobros.GroupBy(c => c.VentaId).ToDictionary(g => g.Key, g => g.Min(c => c.Id));
        var dDesde = desde?.Date;
        var dHasta = hasta?.Date.AddDays(1);

        decimal ventasTienda = 0, abonos = 0, reservas = 0, rutaRend = 0;
        foreach (var c in todosCobros)
        {
            if (dDesde.HasValue && c.Fecha < dDesde.Value) continue;
            if (dHasta.HasValue && c.Fecha >= dHasta.Value) continue;
            if (!ventas.TryGetValue(c.VentaId, out var v)) continue;
            if (v.Canal == CanalVentaNav.Ruta) rutaRend += c.Monto;
            else if (v.Tipo == TipoVentaNav.Reserva) reservas += c.Monto;
            else if (primerCobro[c.VentaId] == c.Id) ventasTienda += c.Monto;
            else abonos += c.Monto;
        }

        var aportes = await FiltrarFecha(
                _aporteRepo.AsQueryable().AsNoTracking().Where(a => a.IsActive && a.TemporadaId == temporadaId),
                desde, hasta, a => a.Fecha)
            .GroupBy(a => a.InversorId == null)
            .Select(g => new { Propio = g.Key, Monto = g.Sum(a => a.Monto) })
            .ToListAsync();

        var pagosProveedores = await FiltrarFecha(
                _pagoProveedorRepo.AsQueryable().AsNoTracking().Where(p => p.IsActive && p.TemporadaId == temporadaId),
                desde, hasta, p => p.Fecha)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

        var pagosInversores = await FiltrarFecha(
                _pagoInversorRepo.AsQueryable().AsNoTracking().Where(p => p.IsActive && p.TemporadaId == temporadaId),
                desde, hasta, p => p.Fecha)
            .GroupBy(p => p.Tipo)
            .Select(g => new { Tipo = g.Key, Monto = g.Sum(p => p.Monto) })
            .ToListAsync();

        var entro = new DashboardFlujoEntroNavDto
        {
            VentasTienda = ventasTienda,
            RendicionesRuta = rutaRend,
            OtrosCobros = new DashboardOtrosCobrosNavDto { AbonosCredito = abonos, Reservas = reservas, Total = abonos + reservas },
            AportesInversores = aportes.Where(a => !a.Propio).Sum(a => a.Monto),
            AportesCapitalPropio = aportes.Where(a => a.Propio).Sum(a => a.Monto)
        };
        entro.Total = entro.VentasTienda + entro.RendicionesRuta + entro.OtrosCobros.Total + entro.AportesInversores + entro.AportesCapitalPropio;

        var pagos = new DashboardFlujoPagosNavDto
        {
            Proveedores = pagosProveedores,
            Sueldos = await ObtenerSueldosPagadosAsync(temporadaId, desde, hasta),
            ComisionesRuta = await ObtenerComisionesPagadasAsync(temporadaId, desde, hasta),
            ComisionesInversores = pagosInversores.Where(p => p.Tipo == TipoPagoInversor.Comision).Sum(p => p.Monto),
            DevolucionesCapital = pagosInversores.Where(p => p.Tipo == TipoPagoInversor.DevolucionCapital).Sum(p => p.Monto)
        };
        pagos.Total = pagos.Proveedores + pagos.Sueldos + pagos.ComisionesRuta + pagos.ComisionesInversores + pagos.DevolucionesCapital;
        var salio = new DashboardFlujoSalioNavDto { Gastos = (await ObtenerGastosAsync(temporadaId, 0, desde, hasta)).Total, Pagos = pagos };
        salio.Total = salio.Gastos + pagos.Total;

        return new DashboardFlujoEfectivoNavDto
        {
            Desde = desde?.Date,
            Hasta = hasta?.Date,
            Entro = entro,
            Salio = salio,
            Neto = entro.Total - salio.Total,
            CajasSinEntregar = await ObtenerCajasSinEntregarAsync(temporadaId)
        };
    }

    // Tiendas cuyo último conteo es un cierre sin entrega de efectivo: su efectivo esperado (informativo, no cambia el total)
    private async Task<DashboardCajasSinEntregarNavDto> ObtenerCajasSinEntregarAsync(int temporadaId)
    {
        var tiendas = (await _temporadaRepo.ObtenerAlmacenesConteoAsync(new[] { temporadaId })).Select(a => a.AlmacenId).Distinct().ToList();
        var lista = new List<DashboardCajaSinEntregarNavDto>();
        foreach (var id in tiendas)
        {
            var estado = await _conteo.ObtenerEstadoAsync(id, null);
            if (!estado.RequiereConteo || !estado.CajaPendiente) continue;
            var resumen = await _cierreCaja.ObtenerResumenAsync(id);
            if (!resumen.Pendiente) continue;
            lista.Add(new DashboardCajaSinEntregarNavDto { AlmacenId = id, AlmacenNombre = resumen.AlmacenNombre, EfectivoEsperado = resumen.EfectivoEsperado });
        }
        return new DashboardCajasSinEntregarNavDto { Monto = lista.Sum(x => x.EfectivoEsperado), Tiendas = lista };
    }

    // ================= Capital, posición y conciliación (al momento actual) =================

    private sealed class Snapshot
    {
        public ResumenCapitalDto Capital { get; set; } = new();
        public List<DeudaProveedorNavDto> DeudasProveedor { get; set; } = new();
        public List<DeudaVendedorRutaDto> DeudasRuta { get; set; } = new();
        public DashboardSueldosNavDto Sueldos { get; set; } = new();
        public DashboardDeudaClientesNavDto Clientes { get; set; } = new();
        public DashboardReservasPendientesNavDto Reservas { get; set; } = new();
        public decimal Stock { get; set; }
        public decimal MercaderiaPorRecibir { get; set; }
    }

    private async Task<Snapshot> ObtenerSnapshotAsync(int temporadaId)
    {
        if (_snapshot != null) return _snapshot;

        var s = new Snapshot
        {
            Capital = await _capital.ObtenerResumenAsync(null),
            DeudasProveedor = await _abastecimiento.ObtenerDeudasAsync(new AbastecimientoNavFiltroDto()),
            DeudasRuta = await _ruta.ObtenerDeudasAsync(null),
            Sueldos = ConstruirSueldos(await _pagosVendedores.ObtenerResumenSueldosAsync(null, null)),
            Clientes = await ObtenerDeudaClientesAsync(temporadaId),
            Reservas = await ObtenerReservasPendientesAsync(temporadaId),
            Stock = await ObtenerStockValorizadoAsync(temporadaId)
        };

        // Mercadería pedida y aún no recibida: el pedido genera deuda con el proveedor desde que se hace, pero el stock solo cuenta lo recibido.
        // Se valora con el precio medio de los pedidos del producto (no considera ajustes manuales del monto total del proveedor)
        var faltantes = (await _abastecimiento.ObtenerFaltantesAsync(new AbastecimientoNavFiltroDto()))
            .Where(f => f.Faltante > 0)
            .GroupBy(f => f.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Faltante));
        if (faltantes.Count > 0)
        {
            var precios = await _pedidoRepo.AsQueryable().AsNoTracking()
                .Where(p => p.IsActive && p.TemporadaId == temporadaId)
                .SelectMany(p => p.Detalles)
                .GroupBy(d => d.ProductoId)
                .Select(g => new { ProductoId = g.Key, Unidades = g.Sum(d => d.CantidadUnidades), Valor = g.Sum(d => d.CantidadUnidades * d.PrecioCompraUnidad) })
                .ToListAsync();
            var medio = precios.Where(x => x.Unidades > 0).ToDictionary(x => x.ProductoId, x => x.Valor / x.Unidades);
            s.MercaderiaPorRecibir = Redondear(faltantes.Sum(kv => kv.Value * medio.GetValueOrDefault(kv.Key)));
        }

        _snapshot = s;
        return s;
    }

    public async Task<DashboardCapitalNavDto> ObtenerCapitalAsync(int temporadaId)
    {
        var s = await ObtenerSnapshotAsync(temporadaId);
        var c = s.Capital;
        return new DashboardCapitalNavDto
        {
            Aportes = c.CapitalTotal,
            CapitalPropio = c.CapitalPropio,
            CapitalInversores = c.CapitalInversores,
            CapitalDevuelto = c.CapitalDevuelto,
            ComisionPagada = c.ComisionPagada,
            PagadoAInversores = c.CapitalDevuelto + c.ComisionPagada,
            CapitalPorDevolver = c.CapitalPorDevolver,
            ComisionPorPagar = c.ComisionPorPagar,
            DeudaInversores = c.CapitalPorDevolver + c.ComisionPorPagar,
            DeudaProveedores = ConstruirDeudaProveedores(s.DeudasProveedor).Total,
            PorCobrarClientes = s.Clientes.Total,
            PorCobrarVendedoresRuta = ConstruirDeudaRuta(s.DeudasRuta).Total,
            SueldosPendientes = s.Sueldos.TotalDebe,
            StockValorizado = s.Stock
        };
    }

    // Tarjeta "Nos deben": ventas a crédito (y la parte entregada de reservas) + deuda de los vendedores de ruta
    public async Task<DashboardNosDebenNavDto> ObtenerNosDebenAsync(int temporadaId)
    {
        var s = await ObtenerSnapshotAsync(temporadaId);
        var ruta = ConstruirDeudaRuta(s.DeudasRuta);
        return new DashboardNosDebenNavDto { Clientes = s.Clientes, VendedoresRuta = ruta, Total = s.Clientes.Total + ruta.Total };
    }

    // Tarjeta "Debo": solo la deuda neta con proveedores (pedidos + compras a crédito − pagos)
    public async Task<DashboardDeboNavDto> ObtenerDeboAsync(int temporadaId)
    {
        var s = await ObtenerSnapshotAsync(temporadaId);
        var proveedores = ConstruirDeudaProveedores(s.DeudasProveedor);
        return new DashboardDeboNavDto
        {
            Total = proveedores.Total,
            PorProveedor = s.DeudasProveedor
                .GroupBy(d => new { d.ProveedorId, d.ProveedorNombre })
                .Select(g => new DashboardDeboProveedorNavDto
                {
                    ProveedorId = g.Key.ProveedorId,
                    ProveedorNombre = g.Key.ProveedorNombre,
                    TotalDeuda = g.Sum(x => x.TotalDeuda),
                    Pagado = g.Sum(x => x.TotalPagado),
                    Saldo = g.Sum(x => x.Saldo)
                })
                .Where(x => x.Saldo != 0)
                .OrderByDescending(x => x.Saldo)
                .ToList(),
            PorCodigo = proveedores.PorCodigo
        };
    }

    // Conciliación de toda la temporada al momento actual ("¿dónde está la ganancia?").
    // Esperado = capital propio aportado + ganancia − retiros. Real = lo que hay (efectivo, stock, lo que nos deben, mercadería pedida por recibir)
    // menos lo que debemos. El aporte de un inversor es DEUDA (se devuelve con comisión): entra al efectivo y sale como pasivo, no es patrimonio.
    public async Task<DashboardConciliacionNavDto> ObtenerConciliacionAsync(int temporadaId)
    {
        var s = await ObtenerSnapshotAsync(temporadaId);
        var resultado = await ObtenerResultadoAsync(temporadaId, null, null);
        var flujo = await ObtenerFlujoEfectivoAsync(temporadaId, null, null);
        var ruta = ConstruirDeudaRuta(s.DeudasRuta).Total;
        var proveedores = ConstruirDeudaProveedores(s.DeudasProveedor).Total;

        var notas = new List<string>
        {
            "Retiros del dueño: el sistema no tiene dónde registrarlos, se toman como 0.",
            "Comisiones de ruta pendientes: la comisión se decide al pagar, no se puede calcular antes; se toman como 0.",
            "El aporte de un inversor es deuda (se devuelve con comisión): entra al efectivo y se resta como deuda con inversores, no suma al capital propio.",
            "Deuda con proveedores incluye lo pedido que aún no llegó; esa mercadería se suma aparte (mercaderiaPorRecibir) para no restar una deuda sin su producto.",
            "Los adelantos de reservas son dinero de clientes por mercadería que todavía no se entregó: se restan como deuda."
        };

        var lineas = new List<DashboardConciliacionLineaNavDto>
        {
            L("efectivoNeto", "Efectivo neto", 1, flujo.Neto, "Flujo de efectivo de toda la temporada (entró − salió)"),
            L("stockValorizado", "Stock valorizado a costo", 1, s.Stock, "Σ stock × costo promedio"),
            L("nosDebenClientes", "Nos deben los clientes", 1, s.Clientes.Total, "Saldo de ventas de tienda a crédito y de lo entregado de reservas"),
            L("nosDebenVendedoresRuta", "Nos deben los vendedores de ruta", 1, ruta, "Saldo de ventas de ruta sin rendir"),
            L("mercaderiaPorRecibir", "Mercadería pedida por recibir", 1, s.MercaderiaPorRecibir, "Faltante de pedidos × precio medio del pedido (estimado)"),
            L("deudaProveedores", "Deuda con proveedores", -1, proveedores, "Pedidos + compras − pagos"),
            L("deudaInversores", "Deuda con inversores", -1, s.Capital.CapitalPorDevolver + s.Capital.ComisionPorPagar, "Capital por devolver + comisión por pagar"),
            L("sueldosPendientes", "Sueldos pendientes", -1, s.Sueldos.TotalDebe, "Sueldo × meses − pagado"),
            L("comisionesRutaPendientes", "Comisiones de ruta pendientes", -1, 0m, "No calculable antes de pagar"),
            L("adelantosReservas", "Adelantos de reservas por entregar", -1, s.Reservas.Adelantos, "Dinero cobrado de reservas aún no entregadas")
        };
        var real = lineas.Sum(l => l.Signo * l.Monto);

        var esperado = new DashboardPatrimonioEsperadoNavDto
        {
            CapitalPropio = s.Capital.CapitalPropio,
            Ganancia = resultado.Ganancia,
            Retiros = 0m
        };
        esperado.Total = esperado.CapitalPropio + esperado.Ganancia - esperado.Retiros;

        var costos = await _stock.ObtenerCostosPromedioAsync(temporadaId);
        var (_, sobrantes) = await ObtenerAjustesConteoAsync(temporadaId, null, null, costos);
        var causas = new List<DashboardConciliacionLineaNavDto>
        {
            L("ajustesPositivosConteo", "Sobrantes de conteo (stock que apareció sin comprarse)", 1, sobrantes, "Σ diferencias positivas de apertura y cierre × costo promedio"),
            L("reservasPorCobrar", "Saldo a cobrar de reservas pendientes (no es activo hasta entregarse)", 0, s.Reservas.SaldoPorCobrar, "Informativo")
        };

        return new DashboardConciliacionNavDto
        {
            Esperado = esperado,
            RealLineas = lineas,
            Real = real,
            Diferencia = real - esperado.Total,
            PosiblesCausas = causas,
            Notas = notas
        };
    }

    private static DashboardConciliacionLineaNavDto L(string clave, string nombre, int signo, decimal monto, string origen) => new()
    {
        Clave = clave,
        Nombre = nombre,
        Signo = signo,
        Monto = Redondear(monto),
        Origen = origen
    };
}
