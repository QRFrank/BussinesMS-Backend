using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductoNav = BussinesMS.Dominio.Entidades.Navidad.Producto;
using ProveedorNav = BussinesMS.Dominio.Entidades.Navidad.Proveedor;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// DTOs armados a mano (CreatedAt → hora de Bolivia aquí; nombres de almacén desde AuthDB).
public class RecepcionNavService : IRecepcionNavService
{
    private const int SistemaNavidadId = 2;
    private const string ReferenciaRecepcion = "Recepcion";

    private readonly IRecepcionNavRepository _repo;
    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly ICodigoClienteRepository _codigoRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IPedidoNavRepository _pedidoRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IStockNavService _stock;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RecepcionNavService> _logger;

    public RecepcionNavService(
        IRecepcionNavRepository repo,
        IProveedorNavRepository proveedorRepo,
        ICodigoClienteRepository codigoRepo,
        IProductoNavRepository productoRepo,
        IPedidoNavRepository pedidoRepo,
        IAlmacenRepository almacenRepo,
        IStockNavService stock,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        ILogger<RecepcionNavService> logger)
    {
        _repo = repo;
        _proveedorRepo = proveedorRepo;
        _codigoRepo = codigoRepo;
        _productoRepo = productoRepo;
        _pedidoRepo = pedidoRepo;
        _almacenRepo = almacenRepo;
        _stock = stock;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResultDto<RecepcionNavDto>> ObtenerTodosAsync(RecepcionNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(x => x.Proveedor)
                .Include(x => x.CodigoCliente)
                .Include(x => x.Detalles)
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            if (query.ProveedorId.HasValue)
                baseQuery = baseQuery.Where(x => x.ProveedorId == query.ProveedorId.Value);

            if (query.CodigoClienteId.HasValue)
                baseQuery = baseQuery.Where(x => x.CodigoClienteId == query.CodigoClienteId.Value);

            if (query.FechaDesde.HasValue)
            {
                var desde = query.FechaDesde.Value.Date;
                baseQuery = baseQuery.Where(x => x.Fecha >= desde);
            }

            if (query.FechaHasta.HasValue)
            {
                var hastaExclusivo = query.FechaHasta.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(x => x.Fecha < hastaExclusivo);
            }

            if (query.Anuladas.HasValue)
                baseQuery = baseQuery.Where(x => x.Anulada == query.Anuladas.Value);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.NroFactura != null && x.NroFactura.ToLower().Contains(f)) ||
                    (x.Observacion != null && x.Observacion.ToLower().Contains(f)) ||
                    (x.Proveedor != null && x.Proveedor.Nombre.ToLower().Contains(f)) ||
                    (x.CodigoCliente != null && x.CodigoCliente.Codigo.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<RecepcionNavDto>
            {
                Items = entidades.Select(e => MapearCabecera(e)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener recepciones");
            throw;
        }
    }

    public async Task<RecepcionNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return await MapearConDetallesAsync(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener recepción {Id}", id);
            throw;
        }
    }

    public async Task<RecepcionNavDto> CrearAsync(CrearRecepcionNavDto dto)
    {
        try
        {
            ValidarEstructura(dto);

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var proveedor = await ObtenerProveedorValidoAsync(dto.ProveedorId, temporada.Id);
            // Ajuste 2: las recepciones son solo de proveedores con pedido; lo demás va por Compras
            if (!proveedor.TrabajaConPedido)
                throw new ValidacionException($"El proveedor {proveedor.Nombre} no trabaja con pedido: registre una compra");
            await ValidarCodigoClienteAsync(proveedor, dto.CodigoClienteId);
            await ValidarAlmacenesAsync(dto.Detalles.SelectMany(d => d.Distribucion).Select(x => x.AlmacenId));

            // Sin tracking: la recepción ya no modifica el producto
            var productoIds = dto.Detalles.Select(d => d.ProductoId).ToList();
            var productos = await _productoRepo.AsQueryable()
                .AsNoTracking()
                .Where(p => productoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            // Líneas de pedido y recibido por producto, clave proveedor + código|null
            var lineasPorProducto = new Dictionary<int, List<(int Cantidad, decimal Precio)>>();
            var recibidos = new Dictionary<int, int>();
            {
                var proveedorId = proveedor.Id;
                var codigoId = dto.CodigoClienteId;

                var lineas = await _pedidoRepo.AsQueryable()
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.ProveedorId == proveedorId && p.CodigoClienteId == codigoId)
                    .SelectMany(p => p.Detalles
                        .Where(d => productoIds.Contains(d.ProductoId))
                        .Select(d => new { p.Fecha, PedidoId = p.Id, DetalleId = d.Id, d.ProductoId, d.CantidadUnidades, d.PrecioCompraUnidad }))
                    .ToListAsync();
                lineasPorProducto = lineas
                    .GroupBy(l => l.ProductoId)
                    .ToDictionary(g => g.Key, g => g
                        .OrderBy(l => l.Fecha).ThenBy(l => l.PedidoId).ThenBy(l => l.DetalleId)
                        .Select(l => (l.CantidadUnidades, l.PrecioCompraUnidad))
                        .ToList());

                recibidos = await _repo.AsQueryable()
                    .AsNoTracking()
                    .Where(r => r.IsActive && !r.Anulada && r.ProveedorId == proveedorId && r.CodigoClienteId == codigoId)
                    .SelectMany(r => r.Detalles)
                    .Where(d => productoIds.Contains(d.ProductoId))
                    .GroupBy(d => d.ProductoId)
                    .Select(g => new { ProductoId = g.Key, Total = g.Sum(d => d.CantidadUnidades) })
                    .ToDictionaryAsync(x => x.ProductoId, x => x.Total);
            }

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            var recepcion = new Recepcion
            {
                TemporadaId = temporada.Id,
                ProveedorId = proveedor.Id,
                CodigoClienteId = dto.CodigoClienteId,
                NroFactura = NormalizarTexto(dto.NroFactura),
                Fecha = dto.Fecha.Date,
                Observacion = NormalizarTexto(dto.Observacion),
                Anulada = false
            };

            foreach (var item in dto.Detalles)
            {
                if (!productos.TryGetValue(item.ProductoId, out var producto) || !producto.IsActive)
                    throw new ValidacionException($"El producto {item.ProductoId} no existe o está inactivo");
                if (producto.ProveedorId != proveedor.Id)
                    throw new ValidacionException($"El producto {NombreMostrar(producto)} no pertenece al proveedor");

                // Precio de la línea del pedido (uno por código → un precio por producto)
                var detalles = ArmarDetallesConPedido(producto, item, dto.CodigoClienteId.HasValue,
                    lineasPorProducto.GetValueOrDefault(producto.Id) ?? new List<(int Cantidad, decimal Precio)>(),
                    recibidos.GetValueOrDefault(producto.Id), usuarioId, ahora);
                foreach (var detalle in detalles)
                    recepcion.Detalles.Add(detalle);
            }

            await _uow.BeginTransactionAsync();
            try
            {
                var creada = await _repo.CrearAsync(recepcion);

                // Stock por almacén + movimiento (+), en la misma transacción
                foreach (var detalle in creada.Detalles)
                    foreach (var dist in detalle.Distribuciones)
                        await _stock.RegistrarEntradaAsync(temporada.Id, dist.AlmacenId, detalle.ProductoId,
                            dist.CantidadUnidades, TipoMovimientoNav.Recepcion, ReferenciaRecepcion, creada.Id);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Recepción creada: {Id} (temporada {TemporadaId})", recepcion.Id, temporada.Id);

            return await ObtenerPorIdAsync(recepcion.Id)
                ?? throw new EntidadNoEncontradaException("Recepción", recepcion.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear recepción");
            throw;
        }
    }

    public async Task AnularAsync(int id)
    {
        try
        {
            var recepcion = await _repo.ObtenerConDetallesAsync(id);
            if (recepcion == null || !recepcion.IsActive)
                throw new EntidadNoEncontradaException("Recepción", id);
            if (recepcion.Anulada)
                throw new ValidacionException("La recepción ya está anulada");

            await _temporadaActual.VerificarEditableAsync(recepcion.TemporadaId);

            // Lo que entró, agrupado por almacén + producto: el stock actual de cada uno tiene que alcanzar
            var aRevertir = recepcion.Detalles
                .SelectMany(d => d.Distribuciones.Select(x => new { x.AlmacenId, d.ProductoId, d.Producto, x.CantidadUnidades }))
                .GroupBy(x => new { x.AlmacenId, x.ProductoId })
                .Select(g => new { g.Key.AlmacenId, g.Key.ProductoId, g.First().Producto, Unidades = g.Sum(x => x.CantidadUnidades) })
                .Where(x => x.Unidades > 0)
                .ToList();

            var nombresAlmacen = await ObtenerNombresAlmacenAsync(aRevertir.Select(x => x.AlmacenId));
            foreach (var r in aRevertir)
            {
                var disponible = await _stock.ObtenerDisponibleAsync(r.AlmacenId, r.ProductoId);
                if (disponible < r.Unidades)
                {
                    var producto = r.Producto != null ? NombreMostrar(r.Producto) : $"#{r.ProductoId}";
                    var almacen = nombresAlmacen.TryGetValue(r.AlmacenId, out var n) ? n : $"#{r.AlmacenId}";
                    throw new ExcepcionDominio(
                        $"No se puede anular: el stock de '{producto}' en '{almacen}' es {disponible} y la recepción ingresó {r.Unidades}",
                        409, "STOCK_INSUFICIENTE");
                }
            }

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var r in aRevertir)
                    await _stock.RegistrarSalidaAsync(recepcion.TemporadaId, r.AlmacenId, r.ProductoId, r.Unidades,
                        TipoMovimientoNav.AnulacionRecepcion, ReferenciaRecepcion, recepcion.Id);

                // Cabecera con tracking (la consulta anterior es sin tracking).
                var entidad = await _repo.ObtenerPorIdAsync(id)
                    ?? throw new EntidadNoEncontradaException("Recepción", id);
                entidad.Anulada = true;
                await _repo.ActualizarAsync(entidad);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Recepción anulada: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular recepción {Id}", id);
            throw;
        }
    }

    // ---------- Validaciones ----------

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarEstructura(CrearRecepcionNavDto dto)
    {
        if (dto.ProveedorId <= 0)
            throw new ValidacionException("El proveedor es obligatorio");
        if (dto.Fecha == default)
            throw new ValidacionException("La fecha es obligatoria");
        if (dto.NroFactura != null && dto.NroFactura.Trim().Length > 50)
            throw new ValidacionException("El número de factura no puede superar 50 caracteres");
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            throw new ValidacionException("Debe registrar al menos un producto");
        if (dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count)
            throw new ValidacionException("Hay productos repetidos en la recepción");

        foreach (var item in dto.Detalles)
        {
            if (item.Distribucion == null || item.Distribucion.Count == 0)
                throw new ValidacionException("Cada producto debe distribuirse al menos a un almacén");
            if (item.Distribucion.Any(d => d.CantidadUnidades <= 0))
                throw new ValidacionException("La cantidad debe ser mayor a 0");
            if (item.Distribucion.Select(d => d.AlmacenId).Distinct().Count() != item.Distribucion.Count)
                throw new ValidacionException("Hay almacenes repetidos en la distribución de un producto");
        }
    }

    // Proveedor activo y de la temporada (sin tracking)
    private async Task<ProveedorNav> ObtenerProveedorValidoAsync(int proveedorId, int temporadaId)
    {
        var proveedor = await _proveedorRepo.AsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == proveedorId);

        if (proveedor == null || !proveedor.IsActive)
            throw new ValidacionException("El proveedor no existe o está inactivo");
        if (proveedor.TemporadaId != temporadaId)
            throw new ValidacionException("El proveedor no pertenece a la temporada abierta");

        return proveedor;
    }

    private async Task ValidarCodigoClienteAsync(ProveedorNav proveedor, int? codigoClienteId)
    {
        if (!proveedor.UsaCodigosCliente)
        {
            if (codigoClienteId.HasValue)
                throw new ValidacionException("El proveedor no usa códigos de cliente");
            return;
        }

        if (!codigoClienteId.HasValue)
            throw new ValidacionException("El código de cliente es obligatorio para este proveedor");

        var codigo = await _codigoRepo.AsQueryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == codigoClienteId.Value);

        if (codigo == null || !codigo.IsActive)
            throw new ValidacionException("El código de cliente no existe o está inactivo");
        if (codigo.ProveedorId != proveedor.Id)
            throw new ValidacionException("El código no pertenece al proveedor");
    }

    // Proveedor con pedido: límite por proveedor + código|null + producto y precio FIFO de las líneas del pedido.
    // Un detalle por precio de línea (con un pedido por código es uno solo; varios solo con pedidos viejos duplicados).
    // La distribución se reparte entre los detalles en orden.
    private static List<RecepcionDetalle> ArmarDetallesConPedido(
        ProductoNav producto,
        CrearRecepcionDetalleNavDto item,
        bool usaCodigo,
        List<(int Cantidad, decimal Precio)> lineas,
        int recibido,
        int usuarioId,
        DateTime ahora)
    {
        var nombre = NombreMostrar(producto);
        var cantidad = item.Distribucion.Sum(d => d.CantidadUnidades);

        var pedidoTotal = lineas.Sum(l => l.Cantidad);
        if (pedidoTotal == 0)
            throw new ValidacionException($"El producto {nombre} no está en ningún pedido de este {(usaCodigo ? "código" : "proveedor")}");
        if (recibido + cantidad > pedidoTotal)
            throw new ValidacionException($"'{nombre}': pedido {pedidoTotal}, recibido {recibido}, intenta recibir {cantidad}: modifique el pedido");

        // Pendiente de cada línea (lo recibido consume las líneas en orden) y asignación de la cantidad
        var segmentos = new List<(int Unidades, decimal Precio)>();
        var yaRecibido = recibido;
        var porAsignar = cantidad;
        foreach (var (cant, precioLinea) in lineas)
        {
            var consumido = Math.Min(cant, yaRecibido);
            yaRecibido -= consumido;
            var pendiente = cant - consumido;
            if (pendiente <= 0 || porAsignar == 0) continue;

            var toma = Math.Min(pendiente, porAsignar);
            porAsignar -= toma;
            if (segmentos.Count > 0 && segmentos[^1].Precio == precioLinea)
                segmentos[^1] = (segmentos[^1].Unidades + toma, precioLinea);
            else
                segmentos.Add((toma, precioLinea));
        }

        // Llenar los lotes en orden recorriendo las filas de distribución (una fila puede partirse entre dos lotes)
        var filas = item.Distribucion;
        var idx = 0;
        var restanteFila = filas[0].CantidadUnidades;
        var lotes = new List<RecepcionDetalle>();
        foreach (var (unidades, precioSegmento) in segmentos)
        {
            var lote = new RecepcionDetalle
            {
                ProductoId = producto.Id,
                CantidadUnidades = unidades,
                PrecioCompraUnidad = precioSegmento,
                CreatedByUsuarioId = usuarioId,
                CreatedAt = ahora
            };

            var falta = unidades;
            while (falta > 0)
            {
                var toma = Math.Min(falta, restanteFila);
                lote.Distribuciones.Add(new RecepcionDistribucion
                {
                    AlmacenId = filas[idx].AlmacenId,
                    CantidadUnidades = toma,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });
                falta -= toma;
                restanteFila -= toma;
                if (restanteFila == 0 && ++idx < filas.Count)
                    restanteFila = filas[idx].CantidadUnidades;
            }

            lotes.Add(lote);
        }

        return lotes;
    }

    // AuthDB: existen, son del sistema navideño y están activos
    private async Task ValidarAlmacenesAsync(IEnumerable<int> almacenIds)
    {
        var ids = almacenIds.Distinct().ToList();
        var almacenes = await _almacenRepo.AsQueryable()
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id);

        foreach (var id in ids)
        {
            if (!almacenes.TryGetValue(id, out var almacen))
                throw new ValidacionException($"El almacén {id} no es un almacén navideño activo");
            if (!almacen.IsActive || almacen.SistemaId != SistemaNavidadId)
                throw new ValidacionException($"El almacén {almacen.Nombre} no es un almacén navideño activo");
        }
    }

    // ---------- Mapeo ----------

    private static RecepcionNavDto MapearCabecera(Recepcion e) => new()
    {
        Id = e.Id,
        TemporadaId = e.TemporadaId,
        ProveedorId = e.ProveedorId,
        ProveedorNombre = e.Proveedor?.Nombre ?? string.Empty,
        TrabajaConPedido = e.Proveedor?.TrabajaConPedido ?? false,
        CodigoClienteId = e.CodigoClienteId,
        Codigo = e.CodigoCliente?.Codigo,
        CodigoTitular = e.CodigoCliente?.Titular,
        NroFactura = e.NroFactura,
        Fecha = e.Fecha,
        Observacion = e.Observacion,
        Anulada = e.Anulada,
        CantidadProductos = e.Detalles.Count,
        TotalUnidades = e.Detalles.Sum(d => d.CantidadUnidades),
        MontoTotal = e.Detalles.Sum(d => d.CantidadUnidades * d.PrecioCompraUnidad),
        IsActive = e.IsActive,
        CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt)
    };

    private async Task<RecepcionNavDto> MapearConDetallesAsync(Recepcion e)
    {
        var dto = MapearCabecera(e);
        var nombresAlmacen = await ObtenerNombresAlmacenAsync(
            e.Detalles.SelectMany(d => d.Distribuciones).Select(x => x.AlmacenId));

        dto.Detalles = e.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new RecepcionDetalleNavDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoNombreMostrar = d.Producto != null ? NombreMostrar(d.Producto) : string.Empty,
                UnidadesPorEmpaque = d.Producto?.UnidadesPorEmpaque,
                NombreEmpaque = d.Producto?.NombreEmpaque,
                CantidadUnidades = d.CantidadUnidades,
                PrecioCompraUnidad = d.PrecioCompraUnidad,
                Subtotal = d.CantidadUnidades * d.PrecioCompraUnidad,
                Distribucion = d.Distribuciones
                    .OrderBy(x => x.Id)
                    .Select(x => new RecepcionDistribucionNavDto
                    {
                        Id = x.Id,
                        AlmacenId = x.AlmacenId,
                        AlmacenNombre = nombresAlmacen.TryGetValue(x.AlmacenId, out var n) ? n : string.Empty,
                        CantidadUnidades = x.CantidadUnidades
                    }).ToList()
            }).ToList();

        return dto;
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

    private static string NombreMostrar(ProductoNav p) => p.Nombre ?? p.Descripcion;

    private static string? NormalizarTexto(string? texto)
    {
        var t = texto?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
