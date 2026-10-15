using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Kardex: movimientos de stock con saldo real acumulado por (almacén, producto).
// Nombres de almacén y usuario desde AuthDB; textos de referencia desde las cabeceras de recepción/compra.
public class KardexNavService : IKardexNavService
{
    private readonly IMovimientoNavRepository _movimientoRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRecepcionNavRepository _recepcionRepo;
    private readonly ICompraNavRepository _compraRepo;
    private readonly IConteoInventarioNavRepository _conteoRepo;
    private readonly IVentaNavRepository _ventaRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly ILogger<KardexNavService> _logger;

    public KardexNavService(
        IMovimientoNavRepository movimientoRepo,
        IAlmacenRepository almacenRepo,
        IUsuarioRepository usuarioRepo,
        IRecepcionNavRepository recepcionRepo,
        ICompraNavRepository compraRepo,
        IConteoInventarioNavRepository conteoRepo,
        IVentaNavRepository ventaRepo,
        ITemporadaActualService temporadaActual,
        ILogger<KardexNavService> logger)
    {
        _conteoRepo = conteoRepo;
        _ventaRepo = ventaRepo;
        _movimientoRepo = movimientoRepo;
        _almacenRepo = almacenRepo;
        _usuarioRepo = usuarioRepo;
        _recepcionRepo = recepcionRepo;
        _compraRepo = compraRepo;
        _temporadaActual = temporadaActual;
        _logger = logger;
    }

    public async Task<PagedResultDto<KardexNavDto>> ObtenerAsync(KardexNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _movimientoRepo.AsQueryable()
                .AsNoTracking()
                .Where(m => m.TemporadaId == temporadaId);

            if (query.AlmacenId.HasValue)
                baseQuery = baseQuery.Where(m => m.AlmacenId == query.AlmacenId.Value);
            if (query.ProductoId.HasValue)
                baseQuery = baseQuery.Where(m => m.ProductoId == query.ProductoId.Value);
            if (query.Tipo.HasValue)
            {
                var tipo = (TipoMovimientoNav)query.Tipo.Value;
                baseQuery = baseQuery.Where(m => m.Tipo == tipo);
            }

            // Días en hora de Bolivia (inclusive) → rango UTC; si falta un límite, solo el otro
            if (query.FechaDesde.HasValue)
            {
                var (inicioUtc, _) = BoliviaTimeZone.RangoFechasUtc(DateOnly.FromDateTime(query.FechaDesde.Value), DateOnly.FromDateTime(query.FechaDesde.Value));
                baseQuery = baseQuery.Where(m => m.Fecha >= inicioUtc);
            }
            if (query.FechaHasta.HasValue)
            {
                var (_, finUtc) = BoliviaTimeZone.RangoFechasUtc(DateOnly.FromDateTime(query.FechaHasta.Value), DateOnly.FromDateTime(query.FechaHasta.Value));
                baseQuery = baseQuery.Where(m => m.Fecha < finUtc);
            }

            var totalCount = await baseQuery.CountAsync();

            var ordenada = query.Descendente
                ? baseQuery.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
                : baseQuery.OrderBy(m => m.Fecha).ThenBy(m => m.Id);

            var page = query.GetPageValue();
            var pageSize = query.GetPageSizeValue();
            var pagina = query.GetIsPagedValue()
                ? ordenada.Skip((page - 1) * pageSize).Take(pageSize)
                : ordenada;

            var movimientos = await pagina.Include(m => m.Producto).ToListAsync();

            var saldos = await CalcularSaldosAsync(movimientos);
            var almacenes = await ObtenerNombresAlmacenAsync(movimientos.Select(m => m.AlmacenId));
            var usuarios = await ObtenerNombresUsuarioAsync(movimientos.Select(m => m.UsuarioId));
            var facturas = await ObtenerFacturasAsync(movimientos
                .Where(m => m.ReferenciaTipo == "Recepcion").Select(m => m.ReferenciaId));
            var notas = await ObtenerNotasAsync(movimientos
                .Where(m => m.ReferenciaTipo == "Compra").Select(m => m.ReferenciaId));
            var conteos = await ObtenerConteosAsync(movimientos
                .Where(m => m.ReferenciaTipo == "ConteoInventario").Select(m => m.ReferenciaId));
            var ventas = await ObtenerNrosVentaAsync(movimientos
                .Where(m => m.ReferenciaTipo == "Venta").Select(m => m.ReferenciaId));

            var items = movimientos.Select(m => new KardexNavDto
            {
                Id = m.Id,
                Fecha = BoliviaTimeZone.ToLocal(m.Fecha),
                Tipo = (int)m.Tipo,
                TipoNombre = NombreTipo(m.Tipo),
                AlmacenId = m.AlmacenId,
                AlmacenNombre = almacenes.TryGetValue(m.AlmacenId, out var an) ? an : string.Empty,
                ProductoId = m.ProductoId,
                ProductoNombreMostrar = m.Producto != null ? (m.Producto.Nombre ?? m.Producto.Descripcion) : string.Empty,
                UnidadesPorEmpaque = m.Producto?.UnidadesPorEmpaque,
                NombreEmpaque = m.Producto?.NombreEmpaque,
                Color = m.Producto?.Color,
                ReferenciaTipo = m.ReferenciaTipo,
                ReferenciaId = m.ReferenciaId,
                Referencia = TextoReferencia(m.Tipo, m.ReferenciaTipo, m.ReferenciaId, facturas, notas, conteos, ventas),
                Entrada = m.Cantidad > 0 ? m.Cantidad : 0,
                Salida = m.Cantidad < 0 ? -m.Cantidad : 0,
                Saldo = saldos.TryGetValue(m.Id, out var s) ? s : 0,
                UsuarioId = m.UsuarioId,
                UsuarioNombre = usuarios.TryGetValue(m.UsuarioId, out var un) ? un : string.Empty,
                Motivo = m.Motivo
            }).ToList();

            return new PagedResultDto<KardexNavDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener kardex");
            throw;
        }
    }

    // Saldo acumulado real por (almacén, producto) en orden (Fecha, Id), sin importar los filtros de la consulta.
    // Solo se trae lo necesario: movimientos de los pares de la página hasta el máximo (Fecha, Id) de la página.
    private async Task<Dictionary<int, int>> CalcularSaldosAsync(List<BussinesMS.Dominio.Entidades.Navidad.MovimientoNav> pagina)
    {
        var resultado = new Dictionary<int, int>();
        if (pagina.Count == 0) return resultado;

        var almacenIds = pagina.Select(m => m.AlmacenId).Distinct().ToList();
        var productoIds = pagina.Select(m => m.ProductoId).Distinct().ToList();

        // Máximo (Fecha, Id) por par dentro de la página, y el máximo global para acotar la consulta
        var maxPorPar = pagina
            .GroupBy(m => (m.AlmacenId, m.ProductoId))
            .ToDictionary(g => g.Key, g => g.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).First());
        var maxGlobal = pagina.OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).First();
        var maxFecha = maxGlobal.Fecha;
        var maxId = maxGlobal.Id;

        var historial = await _movimientoRepo.AsQueryable()
            .AsNoTracking()
            .Where(m => almacenIds.Contains(m.AlmacenId) && productoIds.Contains(m.ProductoId)
                && (m.Fecha < maxFecha || (m.Fecha == maxFecha && m.Id <= maxId)))
            .Select(m => new { m.Id, m.Fecha, m.AlmacenId, m.ProductoId, m.Cantidad })
            .ToListAsync();

        foreach (var grupo in historial.GroupBy(h => (h.AlmacenId, h.ProductoId)))
        {
            if (!maxPorPar.TryGetValue(grupo.Key, out var tope)) continue;
            var acumulado = 0;
            foreach (var h in grupo.OrderBy(h => h.Fecha).ThenBy(h => h.Id))
            {
                if (h.Fecha > tope.Fecha || (h.Fecha == tope.Fecha && h.Id > tope.Id)) break;
                acumulado += h.Cantidad;
                resultado[h.Id] = acumulado;
            }
        }

        return resultado;
    }

    private static string TextoReferencia(TipoMovimientoNav tipo, string referenciaTipo, int referenciaId,
        Dictionary<int, string?> facturas, Dictionary<int, string?> notas,
        Dictionary<int, (TipoConteoInventario Tipo, DateTime Fecha)> conteos, Dictionary<int, int> ventas)
    {
        // Ventas: la referencia es el id de la venta; se muestra su número (NroVenta)
        var nroVenta = ventas.TryGetValue(referenciaId, out var nro) ? nro : referenciaId;
        switch (tipo)
        {
            case TipoMovimientoNav.Venta when referenciaTipo == "Venta":
                return $"Venta #{nroVenta}";
            case TipoMovimientoNav.EntregaReserva when referenciaTipo == "Venta":
                return $"Entrega venta #{nroVenta}";
            case TipoMovimientoNav.AnulacionVenta when referenciaTipo == "Venta":
                return $"Anulación venta #{nroVenta}";
            case TipoMovimientoNav.DevolucionVenta when referenciaTipo == "Venta":
                return $"Devolución venta #{nroVenta}";
            case TipoMovimientoNav.Consumo when referenciaTipo == "Consumo":
                return $"Consumo #{referenciaId}";
            case TipoMovimientoNav.AnulacionConsumo when referenciaTipo == "Consumo":
                return $"Anulación consumo #{referenciaId}";
            case TipoMovimientoNav.AnulacionRecepcion:
                return $"Anulación recepción #{referenciaId}";
            case TipoMovimientoNav.AnulacionCompra:
                return $"Anulación compra #{referenciaId}" + SufijoNota(notas, referenciaId);
            case TipoMovimientoNav.AnulacionTraslado:
                return $"Anulación traslado #{referenciaId}";
        }

        return referenciaTipo switch
        {
            "Recepcion" => $"Recepción #{referenciaId}"
                + (facturas.TryGetValue(referenciaId, out var f) && !string.IsNullOrWhiteSpace(f) ? $" · Factura {f}" : string.Empty),
            "Compra" => $"Compra #{referenciaId}" + SufijoNota(notas, referenciaId),
            "Traslado" => $"Traslado #{referenciaId}",
            "ConteoInventario" => conteos.TryGetValue(referenciaId, out var c)
                ? $"{(c.Tipo == TipoConteoInventario.Apertura ? "Apertura" : "Cierre")} #{referenciaId} · {c.Fecha:dd/MM/yyyy}"
                : $"Conteo #{referenciaId}",
            _ => $"{referenciaTipo} #{referenciaId}"
        };
    }

    private static string SufijoNota(Dictionary<int, string?> notas, int id)
        => notas.TryGetValue(id, out var n) && !string.IsNullOrWhiteSpace(n) ? $" · Nota {n}" : string.Empty;

    private static string NombreTipo(TipoMovimientoNav tipo) => tipo switch
    {
        TipoMovimientoNav.Recepcion => "Recepción",
        TipoMovimientoNav.TrasladoSalida => "Traslado salida",
        TipoMovimientoNav.TrasladoEntrada => "Traslado entrada",
        TipoMovimientoNav.Venta => "Venta",
        TipoMovimientoNav.EntregaReserva => "Entrega reserva",
        TipoMovimientoNav.Consumo => "Consumo",
        TipoMovimientoNav.SalidaRuta => "Salida a ruta",
        TipoMovimientoNav.DevolucionRuta => "Devolución de ruta",
        TipoMovimientoNav.AjusteConteo => "Ajuste de conteo",
        TipoMovimientoNav.AnulacionVenta => "Anulación de venta",
        TipoMovimientoNav.AnulacionRecepcion => "Anulación de recepción",
        TipoMovimientoNav.Compra => "Compra",
        TipoMovimientoNav.AnulacionCompra => "Anulación de compra",
        TipoMovimientoNav.AnulacionTraslado => "Anulación de traslado",
        TipoMovimientoNav.DevolucionVenta => "Devolución de venta",
        TipoMovimientoNav.AnulacionConsumo => "Anulación de consumo",
        _ => tipo.ToString()
    };

    // id de venta → NroVenta
    private async Task<Dictionary<int, int>> ObtenerNrosVentaAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<int, int>();
        return await _ventaRepo.AsQueryable()
            .AsNoTracking()
            .Where(v => lista.Contains(v.Id))
            .ToDictionaryAsync(v => v.Id, v => v.NroVenta);
    }

    private async Task<Dictionary<int, string?>> ObtenerFacturasAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<int, string?>();
        return await _recepcionRepo.AsQueryable()
            .AsNoTracking()
            .Where(r => lista.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.NroFactura);
    }

    private async Task<Dictionary<int, (TipoConteoInventario Tipo, DateTime Fecha)>> ObtenerConteosAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<int, (TipoConteoInventario, DateTime)>();
        var filas = await _conteoRepo.AsQueryable()
            .AsNoTracking()
            .Where(c => lista.Contains(c.Id))
            .Select(c => new { c.Id, c.Tipo, c.Fecha })
            .ToListAsync();
        return filas.ToDictionary(c => c.Id, c => (c.Tipo, c.Fecha));
    }

    private async Task<Dictionary<int, string?>> ObtenerNotasAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        if (lista.Count == 0) return new Dictionary<int, string?>();
        return await _compraRepo.AsQueryable()
            .AsNoTracking()
            .Where(c => lista.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.NroNota);
    }

    private async Task<Dictionary<int, string>> ObtenerNombresAlmacenAsync(IEnumerable<int> almacenIds)
    {
        var ids = almacenIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();
        return await _almacenRepo.AsQueryable()
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Nombre);
    }

    // "Nombre Apellido"; vacío si el usuario ya no existe
    private async Task<Dictionary<int, string>> ObtenerNombresUsuarioAsync(IEnumerable<int> usuarioIds)
    {
        var ids = usuarioIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();
        var usuarios = await _usuarioRepo.AsQueryable()
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nombre, u.Apellido })
            .ToListAsync();
        return usuarios.ToDictionary(u => u.Id, u => $"{u.Nombre?.Trim()} {u.Apellido?.Trim()}".Trim());
    }
}
