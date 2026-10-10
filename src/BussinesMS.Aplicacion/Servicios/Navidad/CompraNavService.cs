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

// Compras a cualquier proveedor (Ajuste 2). DTOs armados a mano (CreatedAt → hora de Bolivia aquí; nombres de almacén desde AuthDB).
public class CompraNavService : ICompraNavService
{
    private const int SistemaNavidadId = 2;
    private const string ReferenciaCompra = "Compra";
    private const string ObsPagoContado = "Pago al contado de compra";
    private const string ObsPagoInicial = "Pago inicial de compra a crédito";

    private readonly ICompraNavRepository _repo;
    private readonly IPagoProveedorNavRepository _pagoRepo;
    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IStockNavService _stock;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<CompraNavService> _logger;

    public CompraNavService(
        ICompraNavRepository repo,
        IPagoProveedorNavRepository pagoRepo,
        IProveedorNavRepository proveedorRepo,
        IProductoNavRepository productoRepo,
        IAlmacenRepository almacenRepo,
        IStockNavService stock,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        ILogger<CompraNavService> logger)
    {
        _repo = repo;
        _pagoRepo = pagoRepo;
        _proveedorRepo = proveedorRepo;
        _productoRepo = productoRepo;
        _almacenRepo = almacenRepo;
        _stock = stock;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResultDto<CompraNavDto>> ObtenerTodosAsync(CompraNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(x => x.Proveedor)
                .Include(x => x.Pagos)
                .Include(x => x.Detalles)
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            if (query.ProveedorId.HasValue)
                baseQuery = baseQuery.Where(x => x.ProveedorId == query.ProveedorId.Value);

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

            if (query.PagadaAlContado.HasValue)
                baseQuery = baseQuery.Where(x => x.PagadaAlContado == query.PagadaAlContado.Value);

            // Estado de pago en la consulta: total (redondeado) vs Σ pagos activos vinculados
            if (query.EstadoPago == 1)
                baseQuery = baseQuery.Where(x => !x.Anulada
                    && Math.Round(x.Detalles.Sum(d => d.CantidadUnidades * d.PrecioCompraUnidad), 2)
                       - x.Pagos.Where(p => p.IsActive).Sum(p => p.Monto) <= 0);
            else if (query.EstadoPago == 2)
                baseQuery = baseQuery.Where(x => !x.Anulada
                    && Math.Round(x.Detalles.Sum(d => d.CantidadUnidades * d.PrecioCompraUnidad), 2)
                       - x.Pagos.Where(p => p.IsActive).Sum(p => p.Monto) > 0);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.NroNota != null && x.NroNota.ToLower().Contains(f)) ||
                    (x.Observacion != null && x.Observacion.ToLower().Contains(f)) ||
                    (x.Proveedor != null && x.Proveedor.Nombre.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<CompraNavDto>
            {
                Items = entidades.Select(e => MapearCabecera(e)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener compras");
            throw;
        }
    }

    public async Task<CompraNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return await MapearConDetallesAsync(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener compra {Id}", id);
            throw;
        }
    }

    public async Task<CompraNavDto> CrearAsync(CrearCompraNavDto dto)
    {
        try
        {
            ValidarEstructura(dto);

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            // Cualquier proveedor (con o sin pedido); sin código de cliente
            var proveedor = await ObtenerProveedorValidoAsync(dto.ProveedorId, temporada.Id);
            await ValidarAlmacenesAsync(dto.Detalles.SelectMany(d => d.Distribucion).Select(x => x.AlmacenId));

            // Con tracking: la compra puede actualizar el precio de compra del producto
            var productoIds = dto.Detalles.Select(d => d.ProductoId).ToList();
            var productos = await _productoRepo.AsQueryable()
                .Where(p => productoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            var compra = new Compra
            {
                TemporadaId = temporada.Id,
                ProveedorId = proveedor.Id,
                NroNota = NormalizarTexto(dto.NroNota),
                Fecha = dto.Fecha.Date,
                PagadaAlContado = dto.PagadaAlContado,
                Observacion = NormalizarTexto(dto.Observacion),
                Anulada = false
            };

            var productosActualizados = new List<ProductoNav>();
            decimal total = 0;

            foreach (var item in dto.Detalles)
            {
                // No se exige que el producto sea del proveedor de la compra
                if (!productos.TryGetValue(item.ProductoId, out var producto) || producto.TemporadaId != temporada.Id)
                {
                    var nombre = producto != null ? NombreMostrar(producto) : item.ProductoId.ToString();
                    throw new ValidacionException($"El producto {nombre} no existe o no es de la temporada abierta");
                }
                if (!producto.IsActive)
                    throw new ValidacionException($"El producto {NombreMostrar(producto)} está inactivo");
                if (item.PrecioCompraUnidad <= 0)
                    throw new ValidacionException($"El precio de compra de {NombreMostrar(producto)} debe ser mayor a 0");

                // Solo el precio de compra; nunca el de catálogo
                if (item.ActualizarPrecioProducto && producto.PrecioCompraUnidad != item.PrecioCompraUnidad)
                {
                    producto.PrecioCompraUnidad = item.PrecioCompraUnidad;
                    productosActualizados.Add(producto);
                }

                var cantidad = item.Distribucion.Sum(d => d.CantidadUnidades);
                total += cantidad * item.PrecioCompraUnidad;

                // RepositorioBase solo pone auditoría en la raíz
                var detalle = new CompraDetalle
                {
                    ProductoId = producto.Id,
                    CantidadUnidades = cantidad,
                    PrecioCompraUnidad = item.PrecioCompraUnidad,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                };
                foreach (var dist in item.Distribucion)
                    detalle.Distribuciones.Add(new CompraDistribucion
                    {
                        AlmacenId = dist.AlmacenId,
                        CantidadUnidades = dist.CantidadUnidades,
                        CreatedByUsuarioId = usuarioId,
                        CreatedAt = ahora
                    });

                compra.Detalles.Add(detalle);
            }

            total = Redondear(total);

            // Crédito con pago inicial parcial: tiene que ser menor al total (si no, al contado)
            var pagoInicial = dto.PagadaAlContado ? 0 : Redondear(dto.MontoPagoInicial ?? 0);
            if (pagoInicial > 0 && pagoInicial >= total)
                throw new ValidacionException($"El pago inicial ({pagoInicial}) debe ser menor al total de la compra ({total}); si se paga todo, registre la compra al contado");

            await _uow.BeginTransactionAsync();
            try
            {
                var creada = await _repo.CrearAsync(compra);

                // Pago automático (contado o pago inicial), vinculado por PagoProveedor.CompraId.
                // Comprobante: nº de nota, o "Compra #id" si no hay.
                if (dto.PagadaAlContado || pagoInicial > 0)
                {
                    await _pagoRepo.CrearAsync(new PagoProveedor
                    {
                        TemporadaId = temporada.Id,
                        ProveedorId = proveedor.Id,
                        CodigoClienteId = null,
                        CompraId = creada.Id,
                        Fecha = compra.Fecha,
                        Monto = dto.PagadaAlContado ? total : pagoInicial,
                        Medio = dto.MedioPago!.Value,
                        Comprobante = compra.NroNota ?? $"Compra #{creada.Id}",
                        Observacion = dto.PagadaAlContado ? ObsPagoContado : ObsPagoInicial
                    });
                }

                // Stock por almacén + movimiento (+), en la misma transacción
                foreach (var detalle in creada.Detalles)
                    foreach (var dist in detalle.Distribuciones)
                        await _stock.RegistrarEntradaAsync(temporada.Id, dist.AlmacenId, detalle.ProductoId,
                            dist.CantidadUnidades, TipoMovimientoNav.Compra, ReferenciaCompra, creada.Id);

                foreach (var producto in productosActualizados)
                    await _productoRepo.ActualizarAsync(producto);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Compra creada: {Id} (temporada {TemporadaId})", compra.Id, temporada.Id);

            return await ObtenerPorIdAsync(compra.Id)
                ?? throw new EntidadNoEncontradaException("Compra", compra.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear compra");
            throw;
        }
    }

    public async Task AnularAsync(int id)
    {
        try
        {
            var compra = await _repo.ObtenerConDetallesAsync(id);
            if (compra == null || !compra.IsActive)
                throw new EntidadNoEncontradaException("Compra", id);
            if (compra.Anulada)
                throw new ValidacionException("La compra ya está anulada");

            await _temporadaActual.VerificarEditableAsync(compra.TemporadaId);

            // Lo que entró, agrupado por almacén + producto: el stock actual de cada uno tiene que alcanzar
            var aRevertir = compra.Detalles
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
                        $"No se puede anular: el stock de '{producto}' en '{almacen}' es {disponible} y la compra ingresó {r.Unidades}",
                        409, "STOCK_INSUFICIENTE");
                }
            }

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var r in aRevertir)
                    await _stock.RegistrarSalidaAsync(compra.TemporadaId, r.AlmacenId, r.ProductoId, r.Unidades,
                        TipoMovimientoNav.AnulacionCompra, ReferenciaCompra, compra.Id);

                // Cabecera con tracking (la consulta anterior es sin tracking).
                var entidad = await _repo.ObtenerPorIdAsync(id)
                    ?? throw new EntidadNoEncontradaException("Compra", id);
                entidad.Anulada = true;
                await _repo.ActualizarAsync(entidad);

                // Todos los pagos vinculados que sigan activos se anulan. El precio del producto no se revierte.
                foreach (var p in compra.Pagos.Where(p => p.IsActive))
                    await _pagoRepo.EliminarAsync(p.Id);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Compra anulada: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular compra {Id}", id);
            throw;
        }
    }

    // ---------- Validaciones ----------

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarEstructura(CrearCompraNavDto dto)
    {
        if (dto.ProveedorId <= 0)
            throw new ValidacionException("El proveedor es obligatorio");
        if (dto.Fecha == default)
            throw new ValidacionException("La fecha es obligatoria");
        if (dto.NroNota != null && dto.NroNota.Trim().Length > 50)
            throw new ValidacionException("El número de nota no puede superar 50 caracteres");
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        if (dto.PagadaAlContado && !dto.MedioPago.HasValue)
            throw new ValidacionException("El medio de pago es obligatorio para una compra al contado");
        if (dto.MontoPagoInicial < 0)
            throw new ValidacionException("El pago inicial no puede ser negativo");
        if (!dto.PagadaAlContado && dto.MontoPagoInicial > 0 && !dto.MedioPago.HasValue)
            throw new ValidacionException("El medio de pago es obligatorio para el pago inicial");
        if (dto.MedioPago.HasValue && !Enum.IsDefined(dto.MedioPago.Value))
            throw new ValidacionException("El medio de pago es inválido");
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            throw new ValidacionException("Debe registrar al menos un producto");
        if (dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count)
            throw new ValidacionException("Hay productos repetidos en la compra");

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

    private static CompraNavDto MapearCabecera(Compra e)
    {
        var total = Redondear(e.Detalles.Sum(d => d.CantidadUnidades * d.PrecioCompraUnidad));
        // Pagado = Σ pagos activos vinculados (editados o anulados desde Pagos a proveedores)
        var pagado = e.Pagos.Where(p => p.IsActive).Sum(p => p.Monto);
        var saldo = Redondear(total - pagado);
        var auto = PagoAutomatico(e);
        var dto = MapearCabeceraBase(e, total, auto);
        dto.MontoPagoInicial = auto != null && auto.IsActive ? auto.Monto : 0m;
        dto.MontoPagado = pagado;
        dto.SaldoCompra = saldo;
        dto.EstadoPago = e.Anulada ? null : saldo <= 0 ? 1 : 2;
        dto.FormaPago = pagado <= 0 ? "credito" : saldo <= 0 ? "contado" : "inicial";
        return dto;
    }

    // Pago automático = el primer pago vinculado con la observación que le pone la compra al crearlo
    // (si el usuario edita esa observación, deja de reconocerse como automático)
    private static PagoProveedor? PagoAutomatico(Compra e)
        => e.Pagos.OrderBy(p => p.Id).FirstOrDefault(p => p.Observacion == ObsPagoContado || p.Observacion == ObsPagoInicial);

    private static CompraNavDto MapearCabeceraBase(Compra e, decimal total, PagoProveedor? auto) => new()
    {
        Id = e.Id,
        TemporadaId = e.TemporadaId,
        ProveedorId = e.ProveedorId,
        ProveedorNombre = e.Proveedor?.Nombre ?? string.Empty,
        NroNota = e.NroNota,
        Fecha = e.Fecha,
        PagadaAlContado = e.PagadaAlContado,
        PagoProveedorId = auto?.Id,
        MedioPago = auto?.Medio,
        MedioPagoNombre = auto?.Medio.ToString(),
        Observacion = e.Observacion,
        Anulada = e.Anulada,
        CantidadProductos = e.Detalles.Count,
        TotalUnidades = e.Detalles.Sum(d => d.CantidadUnidades),
        MontoTotal = total,
        IsActive = e.IsActive,
        CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt)
    };

    private async Task<CompraNavDto> MapearConDetallesAsync(Compra e)
    {
        var dto = MapearCabecera(e);
        var nombresAlmacen = await ObtenerNombresAlmacenAsync(
            e.Detalles.SelectMany(d => d.Distribuciones).Select(x => x.AlmacenId));

        dto.Detalles = e.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new CompraDetalleNavDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoNombreMostrar = d.Producto != null ? NombreMostrar(d.Producto) : string.Empty,
                ProveedorPrincipalId = d.Producto?.ProveedorId ?? 0,
                UnidadesPorEmpaque = d.Producto?.UnidadesPorEmpaque,
                NombreEmpaque = d.Producto?.NombreEmpaque,
                CantidadUnidades = d.CantidadUnidades,
                PrecioCompraUnidad = d.PrecioCompraUnidad,
                Subtotal = Redondear(d.CantidadUnidades * d.PrecioCompraUnidad),
                Distribucion = d.Distribuciones
                    .OrderBy(x => x.Id)
                    .Select(x => new CompraDistribucionNavDto
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

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static string? NormalizarTexto(string? texto)
    {
        var t = texto?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
