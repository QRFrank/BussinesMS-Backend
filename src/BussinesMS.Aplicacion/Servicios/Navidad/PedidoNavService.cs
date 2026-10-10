using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductoNav = BussinesMS.Dominio.Entidades.Navidad.Producto;
using ProveedorNav = BussinesMS.Dominio.Entidades.Navidad.Proveedor;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// DTOs armados a mano (CreatedAt → hora de Bolivia aquí).
public class PedidoNavService : IPedidoNavService
{
    private readonly IPedidoNavRepository _repo;
    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly ICodigoClienteRepository _codigoRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IRecepcionNavRepository _recepcionRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PedidoNavService> _logger;

    public PedidoNavService(
        IPedidoNavRepository repo,
        IProveedorNavRepository proveedorRepo,
        ICodigoClienteRepository codigoRepo,
        IProductoNavRepository productoRepo,
        IRecepcionNavRepository recepcionRepo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        ILogger<PedidoNavService> logger)
    {
        _repo = repo;
        _proveedorRepo = proveedorRepo;
        _codigoRepo = codigoRepo;
        _productoRepo = productoRepo;
        _recepcionRepo = recepcionRepo;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResultDto<PedidoNavDto>> ObtenerTodosAsync(PedidoNavFiltroDto query)
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

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.Observacion != null && x.Observacion.ToLower().Contains(f)) ||
                    (x.Proveedor != null && x.Proveedor.Nombre.ToLower().Contains(f)) ||
                    (x.CodigoCliente != null && x.CodigoCliente.Codigo.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<PedidoNavDto>
            {
                Items = entidades.Select(e => MapearCabecera(e)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pedidos");
            throw;
        }
    }

    public async Task<PedidoNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            var recibidos = await ObtenerRecibidosAsync(entidad.ProveedorId, entidad.CodigoClienteId);
            return MapearConDetalles(entidad, recibidos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pedido {Id}", id);
            throw;
        }
    }

    public async Task<PedidoNavDto> CrearAsync(CrearPedidoNavDto dto)
    {
        try
        {
            ValidarEstructura(dto);

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var proveedor = await ObtenerProveedorValidoAsync(dto.ProveedorId, temporada.Id);
            await ValidarCodigoClienteAsync(proveedor, dto.CodigoClienteId);
            await ValidarPedidoUnicoAsync(proveedor, dto.CodigoClienteId);
            var productos = await ValidarProductosAsync(proveedor, dto.Detalles.Select(d => d.ProductoId));
            ValidarPrecios(dto, productos);

            var detalles = CrearDetalles(dto);
            var pedido = new Pedido
            {
                TemporadaId = temporada.Id,
                ProveedorId = proveedor.Id,
                CodigoClienteId = dto.CodigoClienteId,
                Fecha = dto.Fecha.Date,
                Observacion = NormalizarTexto(dto.Observacion),
                MontoTotalProveedor = ResolverMontoProveedor(dto, detalles)
            };
            foreach (var detalle in detalles)
                pedido.Detalles.Add(detalle);

            // Ajuste 2: el pedido no modifica los precios del producto
            await _repo.CrearAsync(pedido);

            _logger.LogInformation("Pedido creado: {Id} (temporada {TemporadaId})", pedido.Id, temporada.Id);

            return await ObtenerPorIdAsync(pedido.Id)
                ?? throw new EntidadNoEncontradaException("Pedido", pedido.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear pedido");
            throw;
        }
    }

    public async Task<PedidoNavDto> ActualizarAsync(ActualizarPedidoNavDto dto)
    {
        try
        {
            ValidarEstructura(dto);

            // Con tracking: se actualiza la cabecera
            var pedido = await _repo.ObtenerPorIdAsync(dto.Id);
            if (pedido == null || !pedido.IsActive)
                throw new EntidadNoEncontradaException("Pedido", dto.Id);

            await _temporadaActual.VerificarEditableAsync(pedido.TemporadaId);

            if (dto.ProveedorId != pedido.ProveedorId || dto.CodigoClienteId != pedido.CodigoClienteId)
                throw new ValidacionException("No se puede cambiar el proveedor ni el código de un pedido");

            var proveedor = await ObtenerProveedorValidoAsync(pedido.ProveedorId, pedido.TemporadaId);
            await ValidarCodigoClienteAsync(proveedor, pedido.CodigoClienteId);
            var productos = await ValidarProductosAsync(proveedor, dto.Detalles.Select(d => d.ProductoId));
            ValidarPrecios(dto, productos);

            await ValidarNoBajarDeRecibidoAsync(pedido, dto, productos);

            var nuevos = CrearDetalles(dto);

            pedido.Fecha = dto.Fecha.Date;
            pedido.Observacion = NormalizarTexto(dto.Observacion);
            pedido.MontoTotalProveedor = ResolverMontoProveedor(dto, nuevos);

            await _uow.BeginTransactionAsync();
            try
            {
                await _repo.ActualizarAsync(pedido);
                await _repo.ReemplazarDetallesAsync(pedido.Id, nuevos);
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Pedido actualizado: {Id}", pedido.Id);

            return await ObtenerPorIdAsync(pedido.Id)
                ?? throw new EntidadNoEncontradaException("Pedido", pedido.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar pedido {Id}", dto.Id);
            throw;
        }
    }

    // ---------- Validaciones ----------

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarEstructura(CrearPedidoNavDto dto)
    {
        if (dto.ProveedorId <= 0)
            throw new ValidacionException("El proveedor es obligatorio");
        if (dto.Fecha == default)
            throw new ValidacionException("La fecha es obligatoria");
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            throw new ValidacionException("Debe registrar al menos un producto");
        if (dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count)
            throw new ValidacionException("Hay productos repetidos en el pedido");
        if (dto.Detalles.Any(d => d.CantidadUnidades < 0))
            throw new ValidacionException("La cantidad no puede ser negativa");
        if (dto.Detalles.Any(d => d.PrecioCompraUnidad < 0))
            throw new ValidacionException("El precio de compra no puede ser negativo");
        if (dto.MontoTotalProveedor < 0)
            throw new ValidacionException("El monto total del proveedor no puede ser negativo");
    }

    // Precio de compra (de la nota del código) obligatorio en cada línea y > 0 si hay cantidad
    private static void ValidarPrecios(CrearPedidoNavDto dto, Dictionary<int, ProductoNav> productos)
    {
        foreach (var d in dto.Detalles)
        {
            var nombre = NombreMostrar(productos[d.ProductoId]);
            if (!d.PrecioCompraUnidad.HasValue)
                throw new ValidacionException($"El precio de compra de {nombre} es obligatorio");
            if (d.CantidadUnidades > 0 && d.PrecioCompraUnidad.Value <= 0)
                throw new ValidacionException($"El precio de compra de {nombre} debe ser mayor a 0");
        }
    }

    // Uno por código (o por proveedor si no usa códigos) por temporada → 409
    private async Task ValidarPedidoUnicoAsync(ProveedorNav proveedor, int? codigoClienteId)
    {
        var proveedorId = proveedor.Id;
        var existente = await _repo.AsQueryable()
            .AsNoTracking()
            .Where(p => p.IsActive && p.ProveedorId == proveedorId && p.CodigoClienteId == codigoClienteId)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

        if (existente.HasValue)
            throw new ExcepcionDominio(
                codigoClienteId.HasValue
                    ? $"Este código ya tiene un pedido en la temporada (pedido #{existente.Value}): edítelo"
                    : $"El proveedor {proveedor.Nombre} ya tiene un pedido en la temporada (pedido #{existente.Value}): edítelo",
                409, "PEDIDO_YA_EXISTE");
    }

    // Por producto: si el total pedido del código baja y queda por debajo de lo recibido → 400.
    // Los productos cuyo total no baja no se validan (datos viejos que ya superan lo pedido).
    private async Task ValidarNoBajarDeRecibidoAsync(Pedido pedido, CrearPedidoNavDto dto, Dictionary<int, ProductoNav> productos)
    {
        var proveedorId = pedido.ProveedorId;
        var codigoId = pedido.CodigoClienteId;

        var lineas = await _repo.AsQueryable()
            .AsNoTracking()
            .Where(p => p.IsActive && p.ProveedorId == proveedorId && p.CodigoClienteId == codigoId)
            .SelectMany(p => p.Detalles.Select(d => new { PedidoId = p.Id, d.ProductoId, d.CantidadUnidades }))
            .ToListAsync();

        var otros = lineas.Where(l => l.PedidoId != pedido.Id)
            .GroupBy(l => l.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.CantidadUnidades));
        var viejos = lineas.Where(l => l.PedidoId == pedido.Id)
            .GroupBy(l => l.ProductoId)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.CantidadUnidades));
        var nuevos = dto.Detalles.ToDictionary(d => d.ProductoId, d => d.CantidadUnidades);

        var recibidos = await ObtenerRecibidosAsync(proveedorId, codigoId);

        foreach (var productoId in viejos.Keys.Union(nuevos.Keys))
        {
            var otrosTotal = otros.GetValueOrDefault(productoId);
            var antes = otrosTotal + viejos.GetValueOrDefault(productoId);
            var nuevo = nuevos.GetValueOrDefault(productoId);
            var despues = otrosTotal + nuevo;
            var recibido = recibidos.GetValueOrDefault(productoId);

            if (despues < antes && despues < recibido)
            {
                var nombre = productos.TryGetValue(productoId, out var p)
                    ? NombreMostrar(p)
                    : await _productoRepo.AsQueryable().AsNoTracking()
                        .Where(x => x.Id == productoId)
                        .Select(x => x.Nombre ?? x.Descripcion)
                        .FirstOrDefaultAsync() ?? $"#{productoId}";
                throw new ValidacionException(
                    $"No se puede bajar '{nombre}' a {nuevo}: ya se recibieron {recibido} (total pedido del código: {despues})");
            }
        }
    }

    // Σ recibido por producto: recepciones activas no anuladas del proveedor + código|null
    private async Task<Dictionary<int, int>> ObtenerRecibidosAsync(int proveedorId, int? codigoId)
    {
        return await _recepcionRepo.AsQueryable()
            .AsNoTracking()
            .Where(r => r.IsActive && !r.Anulada && r.ProveedorId == proveedorId && r.CodigoClienteId == codigoId)
            .SelectMany(r => r.Detalles)
            .GroupBy(d => d.ProductoId)
            .Select(g => new { ProductoId = g.Key, Total = g.Sum(d => d.CantidadUnidades) })
            .ToDictionaryAsync(x => x.ProductoId, x => x.Total);
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
            throw new ValidacionException("El proveedor no pertenece a la temporada del pedido");
        // Ajuste 2: pedidos solo para proveedores que trabajan con pedido
        if (!proveedor.TrabajaConPedido)
            throw new ValidacionException($"El proveedor {proveedor.Nombre} no trabaja con pedido: registre una compra");

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

    // Productos activos y del proveedor (principal). Sin tracking: el pedido no los modifica
    private async Task<Dictionary<int, ProductoNav>> ValidarProductosAsync(ProveedorNav proveedor, IEnumerable<int> productoIds)
    {
        var ids = productoIds.Distinct().ToList();
        var productos = await _productoRepo.AsQueryable()
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var id in ids)
        {
            if (!productos.TryGetValue(id, out var producto))
                throw new ValidacionException($"El producto {id} no existe");
            if (!producto.IsActive)
                throw new ValidacionException($"El producto {NombreMostrar(producto)} está inactivo");
            if (producto.ProveedorId != proveedor.Id)
                throw new ValidacionException($"El producto {NombreMostrar(producto)} no pertenece al proveedor");
        }

        return productos;
    }

    // CreatedBy/CreatedAt a mano: RepositorioBase solo los pone en la raíz.
    // El precio ya fue validado como obligatorio (ValidarPrecios).
    private List<PedidoDetalle> CrearDetalles(CrearPedidoNavDto dto)
    {
        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        var ahora = DateTime.UtcNow;
        return dto.Detalles.Select(d => new PedidoDetalle
        {
            ProductoId = d.ProductoId,
            CantidadUnidades = d.CantidadUnidades,
            PrecioCompraUnidad = d.PrecioCompraUnidad!.Value,
            CreatedByUsuarioId = usuarioId,
            CreatedAt = ahora
        }).ToList();
    }

    // El enviado (redondeado) o, si no viene, el calculado
    private static decimal ResolverMontoProveedor(CrearPedidoNavDto dto, IEnumerable<PedidoDetalle> detalles)
        => dto.MontoTotalProveedor.HasValue
            ? Redondear(dto.MontoTotalProveedor.Value)
            : CalcularMonto(detalles);

    private static decimal CalcularMonto(IEnumerable<PedidoDetalle> detalles)
        => Redondear(detalles.Sum(d => d.CantidadUnidades * d.PrecioCompraUnidad));

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    // ---------- Mapeo ----------

    private static PedidoNavDto MapearCabecera(Pedido e)
    {
        var calculado = CalcularMonto(e.Detalles);
        return new PedidoNavDto
        {
            Id = e.Id,
            TemporadaId = e.TemporadaId,
            ProveedorId = e.ProveedorId,
            ProveedorNombre = e.Proveedor?.Nombre ?? string.Empty,
            CodigoClienteId = e.CodigoClienteId,
            Codigo = e.CodigoCliente?.Codigo,
            CodigoTitular = e.CodigoCliente?.Titular,
            Fecha = e.Fecha,
            Observacion = e.Observacion,
            CantidadProductos = e.Detalles.Count,
            TotalUnidades = e.Detalles.Sum(d => d.CantidadUnidades),
            TrabajaConPedido = e.Proveedor?.TrabajaConPedido ?? false,
            MontoTotalCalculado = calculado,
            MontoTotalProveedor = e.MontoTotalProveedor,
            DifiereMonto = calculado != e.MontoTotalProveedor,
            IsActive = e.IsActive,
            CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt)
        };
    }

    private static PedidoNavDto MapearConDetalles(Pedido e, Dictionary<int, int> recibidos)
    {
        var dto = MapearCabecera(e);
        dto.Detalles = e.Detalles
            .OrderBy(d => d.Id)
            .Select(d => new PedidoDetalleNavDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoNombreMostrar = d.Producto != null ? NombreMostrar(d.Producto) : string.Empty,
                UnidadesPorEmpaque = d.Producto?.UnidadesPorEmpaque,
                NombreEmpaque = d.Producto?.NombreEmpaque,
                CantidadUnidades = d.CantidadUnidades,
                PrecioCompraUnidad = d.PrecioCompraUnidad,
                Subtotal = Redondear(d.CantidadUnidades * d.PrecioCompraUnidad),
                Recibido = recibidos.GetValueOrDefault(d.ProductoId)
            }).ToList();
        return dto;
    }

    private static string NombreMostrar(ProductoNav p) => p.Nombre ?? p.Descripcion;

    private static string? NormalizarTexto(string? texto)
    {
        var t = texto?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
