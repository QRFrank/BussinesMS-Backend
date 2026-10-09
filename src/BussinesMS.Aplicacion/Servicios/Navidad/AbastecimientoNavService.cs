using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Reportes de abastecimiento (B3 faltantes, B5 deudas; Ajuste 2: deuda = pedidos + compras − pagos): agregados en la BD, unión y nombres en memoria.
public class AbastecimientoNavService : IAbastecimientoNavService
{
    private readonly IPedidoNavRepository _pedidoRepo;
    private readonly IRecepcionNavRepository _recepcionRepo;
    private readonly ICompraNavRepository _compraRepo;
    private readonly IPagoProveedorNavRepository _pagoRepo;
    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly ICodigoClienteRepository _codigoRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly ILogger<AbastecimientoNavService> _logger;

    public AbastecimientoNavService(
        IPedidoNavRepository pedidoRepo,
        IRecepcionNavRepository recepcionRepo,
        ICompraNavRepository compraRepo,
        IPagoProveedorNavRepository pagoRepo,
        IProveedorNavRepository proveedorRepo,
        ICodigoClienteRepository codigoRepo,
        IProductoNavRepository productoRepo,
        ITemporadaActualService temporadaActual,
        ILogger<AbastecimientoNavService> logger)
    {
        _pedidoRepo = pedidoRepo;
        _recepcionRepo = recepcionRepo;
        _compraRepo = compraRepo;
        _pagoRepo = pagoRepo;
        _proveedorRepo = proveedorRepo;
        _codigoRepo = codigoRepo;
        _productoRepo = productoRepo;
        _temporadaActual = temporadaActual;
        _logger = logger;
    }

    public async Task<List<FaltanteNavDto>> ObtenerFaltantesAsync(AbastecimientoNavFiltroDto filtro)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(filtro.TemporadaId);

            // Pedido: Σ detalles de pedidos activos
            var pedidosQuery = _pedidoRepo.AsQueryable().AsNoTracking()
                .Where(p => p.IsActive && p.TemporadaId == temporadaId);
            if (filtro.ProveedorId.HasValue)
                pedidosQuery = pedidosQuery.Where(p => p.ProveedorId == filtro.ProveedorId.Value);
            if (filtro.CodigoClienteId.HasValue)
                pedidosQuery = pedidosQuery.Where(p => p.CodigoClienteId == filtro.CodigoClienteId.Value);

            var pedidos = await pedidosQuery
                .SelectMany(p => p.Detalles.Select(d => new { p.ProveedorId, p.CodigoClienteId, d.ProductoId, d.CantidadUnidades }))
                .GroupBy(x => new { x.ProveedorId, x.CodigoClienteId, x.ProductoId })
                .Select(g => new { g.Key.ProveedorId, g.Key.CodigoClienteId, g.Key.ProductoId, Total = g.Sum(x => x.CantidadUnidades) })
                .ToListAsync();

            // Recibido: Σ detalles de recepciones activas no anuladas
            var recepcionesQuery = FiltrarRecepciones(temporadaId, filtro);
            var recibidos = await recepcionesQuery
                .SelectMany(r => r.Detalles.Select(d => new { r.ProveedorId, r.CodigoClienteId, d.ProductoId, d.CantidadUnidades }))
                .GroupBy(x => new { x.ProveedorId, x.CodigoClienteId, x.ProductoId })
                .Select(g => new { g.Key.ProveedorId, g.Key.CodigoClienteId, g.Key.ProductoId, Total = g.Sum(x => x.CantidadUnidades) })
                .ToListAsync();

            // Unión en memoria (incluye combinaciones con solo recepciones)
            var filas = new Dictionary<(int ProveedorId, int? CodigoClienteId, int ProductoId), (int Pedido, int Recibido)>();
            foreach (var p in pedidos)
            {
                var key = (p.ProveedorId, p.CodigoClienteId, p.ProductoId);
                filas.TryGetValue(key, out var v);
                filas[key] = (v.Pedido + p.Total, v.Recibido);
            }
            foreach (var r in recibidos)
            {
                var key = (r.ProveedorId, r.CodigoClienteId, r.ProductoId);
                filas.TryGetValue(key, out var v);
                filas[key] = (v.Pedido, v.Recibido + r.Total);
            }

            if (filas.Count == 0) return new List<FaltanteNavDto>();

            var proveedores = await ObtenerProveedoresAsync(filas.Keys.Select(k => k.ProveedorId));
            var codigos = await ObtenerCodigosAsync(filas.Keys.Select(k => k.CodigoClienteId));
            var productoIds = filas.Keys.Select(k => k.ProductoId).Distinct().ToList();
            var productos = await _productoRepo.AsQueryable().AsNoTracking()
                .Where(p => productoIds.Contains(p.Id))
                .Select(p => new { p.Id, NombreMostrar = p.Nombre ?? p.Descripcion, p.UnidadesPorEmpaque, p.NombreEmpaque })
                .ToDictionaryAsync(p => p.Id);

            return filas
                .Select(f =>
                {
                    var codigo = f.Key.CodigoClienteId.HasValue && codigos.TryGetValue(f.Key.CodigoClienteId.Value, out var c) ? c : default;
                    productos.TryGetValue(f.Key.ProductoId, out var prod);
                    var faltante = f.Value.Pedido - f.Value.Recibido;
                    proveedores.TryGetValue(f.Key.ProveedorId, out var prov);
                    return new FaltanteNavDto
                    {
                        ProveedorId = f.Key.ProveedorId,
                        ProveedorNombre = prov.Nombre ?? string.Empty,
                        TrabajaConPedido = prov.TrabajaConPedido,
                        CodigoClienteId = f.Key.CodigoClienteId,
                        Codigo = codigo.Codigo,
                        CodigoTitular = codigo.Titular,
                        ProductoId = f.Key.ProductoId,
                        ProductoNombreMostrar = prod?.NombreMostrar ?? string.Empty,
                        UnidadesPorEmpaque = prod?.UnidadesPorEmpaque,
                        NombreEmpaque = prod?.NombreEmpaque,
                        Pedido = f.Value.Pedido,
                        Recibido = f.Value.Recibido,
                        Faltante = faltante,
                        LlegoDeMas = faltante < 0
                    };
                })
                .OrderBy(x => x.ProveedorNombre)
                .ThenBy(x => x.Codigo)
                .ThenBy(x => x.ProductoNombreMostrar)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener faltantes");
            throw;
        }
    }

    public async Task<List<DeudaProveedorNavDto>> ObtenerDeudasAsync(AbastecimientoNavFiltroDto filtro)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(filtro.TemporadaId);

            // Compras: Σ cantidad × precio de compras no anuladas. No llevan código (fila sin código del proveedor):
            // con filtro por código no aparecen.
            var comprasQuery = _compraRepo.AsQueryable().AsNoTracking()
                .Where(c => c.IsActive && !c.Anulada && c.TemporadaId == temporadaId);
            if (filtro.ProveedorId.HasValue)
                comprasQuery = comprasQuery.Where(c => c.ProveedorId == filtro.ProveedorId.Value);
            if (filtro.CodigoClienteId.HasValue)
                comprasQuery = comprasQuery.Where(c => false);

            var compras = await comprasQuery
                .SelectMany(c => c.Detalles.Select(d => new { c.ProveedorId, Monto = d.CantidadUnidades * d.PrecioCompraUnidad }))
                .GroupBy(x => x.ProveedorId)
                .Select(g => new { ProveedorId = g.Key, CodigoClienteId = (int?)null, Total = g.Sum(x => x.Monto) })
                .ToListAsync();

            // Pagado: Σ pagos activos
            var pagosQuery = _pagoRepo.AsQueryable().AsNoTracking()
                .Where(p => p.IsActive && p.TemporadaId == temporadaId);
            if (filtro.ProveedorId.HasValue)
                pagosQuery = pagosQuery.Where(p => p.ProveedorId == filtro.ProveedorId.Value);
            if (filtro.CodigoClienteId.HasValue)
                pagosQuery = pagosQuery.Where(p => p.CodigoClienteId == filtro.CodigoClienteId.Value);

            var pagados = await pagosQuery
                .GroupBy(p => new { p.ProveedorId, p.CodigoClienteId })
                .Select(g => new { g.Key.ProveedorId, g.Key.CodigoClienteId, Total = g.Sum(x => x.Monto) })
                .ToListAsync();

            // Pedido: Σ MontoTotalProveedor de pedidos activos
            var pedidosQuery = _pedidoRepo.AsQueryable().AsNoTracking()
                .Where(p => p.IsActive && p.TemporadaId == temporadaId);
            if (filtro.ProveedorId.HasValue)
                pedidosQuery = pedidosQuery.Where(p => p.ProveedorId == filtro.ProveedorId.Value);
            if (filtro.CodigoClienteId.HasValue)
                pedidosQuery = pedidosQuery.Where(p => p.CodigoClienteId == filtro.CodigoClienteId.Value);

            var pedidos = await pedidosQuery
                .GroupBy(p => new { p.ProveedorId, p.CodigoClienteId })
                .Select(g => new { g.Key.ProveedorId, g.Key.CodigoClienteId, Total = g.Sum(x => x.MontoTotalProveedor) })
                .ToListAsync();

            var filas = new Dictionary<(int ProveedorId, int? CodigoClienteId), (decimal Pedido, decimal Compra, decimal Pagado)>();
            foreach (var p in pedidos)
            {
                var key = (p.ProveedorId, p.CodigoClienteId);
                filas.TryGetValue(key, out var v);
                filas[key] = (v.Pedido + p.Total, v.Compra, v.Pagado);
            }
            foreach (var c in compras)
            {
                var key = (c.ProveedorId, c.CodigoClienteId);
                filas.TryGetValue(key, out var v);
                filas[key] = (v.Pedido, v.Compra + c.Total, v.Pagado);
            }
            foreach (var p in pagados)
            {
                var key = (p.ProveedorId, p.CodigoClienteId);
                filas.TryGetValue(key, out var v);
                filas[key] = (v.Pedido, v.Compra, v.Pagado + p.Total);
            }

            // Solo filas con algo
            var conMovimiento = filas.Where(f => f.Value.Pedido != 0 || f.Value.Compra != 0 || f.Value.Pagado != 0).ToList();
            if (conMovimiento.Count == 0) return new List<DeudaProveedorNavDto>();

            var proveedores = await ObtenerProveedoresAsync(conMovimiento.Select(f => f.Key.ProveedorId));
            var codigos = await ObtenerCodigosAsync(conMovimiento.Select(f => f.Key.CodigoClienteId));

            return conMovimiento
                .Select(f =>
                {
                    var codigo = f.Key.CodigoClienteId.HasValue && codigos.TryGetValue(f.Key.CodigoClienteId.Value, out var c) ? c : default;
                    proveedores.TryGetValue(f.Key.ProveedorId, out var prov);
                    // Ajuste 2: la deuda nace con el pedido y con la compra; las recepciones no generan deuda
                    var totalPedidos = Redondear(f.Value.Pedido);
                    var totalCompras = Redondear(f.Value.Compra);
                    var totalPagado = Redondear(f.Value.Pagado);
                    var totalDeuda = totalPedidos + totalCompras;
                    return new DeudaProveedorNavDto
                    {
                        ProveedorId = f.Key.ProveedorId,
                        ProveedorNombre = prov.Nombre ?? string.Empty,
                        CodigoClienteId = f.Key.CodigoClienteId,
                        Codigo = codigo.Codigo,
                        CodigoTitular = codigo.Titular,
                        TrabajaConPedido = prov.TrabajaConPedido,
                        TotalPedidos = totalPedidos,
                        TotalCompras = totalCompras,
                        TotalDeuda = totalDeuda,
                        TotalPagado = totalPagado,
                        Saldo = Redondear(totalDeuda - totalPagado)
                    };
                })
                .OrderBy(x => x.ProveedorNombre)
                .ThenBy(x => x.Codigo)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener deudas con proveedores");
            throw;
        }
    }

    // ---------- Auxiliares ----------

    private IQueryable<Recepcion> FiltrarRecepciones(int temporadaId, AbastecimientoNavFiltroDto filtro)
    {
        var query = _recepcionRepo.AsQueryable().AsNoTracking()
            .Where(r => r.IsActive && !r.Anulada && r.TemporadaId == temporadaId);
        if (filtro.ProveedorId.HasValue)
            query = query.Where(r => r.ProveedorId == filtro.ProveedorId.Value);
        if (filtro.CodigoClienteId.HasValue)
            query = query.Where(r => r.CodigoClienteId == filtro.CodigoClienteId.Value);
        return query;
    }

    private async Task<Dictionary<int, (string Nombre, bool TrabajaConPedido)>> ObtenerProveedoresAsync(IEnumerable<int> proveedorIds)
    {
        var ids = proveedorIds.Distinct().ToList();
        var proveedores = await _proveedorRepo.AsQueryable().AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Nombre, p.TrabajaConPedido })
            .ToListAsync();
        return proveedores.ToDictionary(p => p.Id, p => (p.Nombre, p.TrabajaConPedido));
    }

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private async Task<Dictionary<int, (string? Codigo, string? Titular)>> ObtenerCodigosAsync(IEnumerable<int?> codigoIds)
    {
        var ids = codigoIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, (string? Codigo, string? Titular)>();
        var codigos = await _codigoRepo.AsQueryable().AsNoTracking()
            .Where(c => ids.Contains(c.Id))
            .Select(c => new { c.Id, c.Codigo, c.Titular })
            .ToListAsync();
        return codigos.ToDictionary(c => c.Id, c => ((string?)c.Codigo, (string?)c.Titular));
    }
}
