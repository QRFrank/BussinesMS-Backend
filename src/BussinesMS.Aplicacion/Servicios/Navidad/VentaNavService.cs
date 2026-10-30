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

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Ventas de tienda (módulo 6; las reservas se registran en ReservaNavService, que reutiliza los helpers internal de esta clase). La cabecera guarda los estados (pago y entrega); las entregas son lo único que mueve stock
// y los cobros viven en su tabla. DTOs armados a mano (CreatedAt → hora de Bolivia aquí; nombres desde AuthDB).
// Transacciones con INavidadUnitOfWork; stock siempre vía IStockNavService.
public class VentaNavService : IVentaNavService
{
    private const int SistemaNavidadId = 2;
    private const string ReferenciaVenta = "Venta";
    internal const string MensajeReservaEnReservas = "Las reservas se registran en /Reservas";

    private readonly IVentaNavRepository _repo;
    private readonly IEntregaVentaNavRepository _entregaRepo;
    private readonly ICobroVentaNavRepository _cobroRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IVendedorNavRepository _vendedorRepo;
    private readonly IClienteNavRepository _clienteRepo;
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IRolRepository _rolRepo;
    private readonly IStockNavService _stock;
    private readonly IConteoNavService _conteo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly IPrecioCompraAccesoService _precioAcceso;
    private readonly ILogger<VentaNavService> _logger;

    public VentaNavService(
        IVentaNavRepository repo,
        IEntregaVentaNavRepository entregaRepo,
        ICobroVentaNavRepository cobroRepo,
        IProductoNavRepository productoRepo,
        IVendedorNavRepository vendedorRepo,
        IClienteNavRepository clienteRepo,
        IStockAlmacenNavRepository stockRepo,
        IAlmacenRepository almacenRepo,
        IUsuarioRepository usuarioRepo,
        IRolRepository rolRepo,
        IStockNavService stock,
        IConteoNavService conteo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        IAlmacenAccesoNavService acceso,
        IPrecioCompraAccesoService precioAcceso,
        ILogger<VentaNavService> logger)
    {
        _repo = repo;
        _entregaRepo = entregaRepo;
        _cobroRepo = cobroRepo;
        _productoRepo = productoRepo;
        _vendedorRepo = vendedorRepo;
        _clienteRepo = clienteRepo;
        _stockRepo = stockRepo;
        _almacenRepo = almacenRepo;
        _usuarioRepo = usuarioRepo;
        _rolRepo = rolRepo;
        _stock = stock;
        _conteo = conteo;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _acceso = acceso;
        _precioAcceso = precioAcceso;
        _logger = logger;
    }

    // ================= Consultas =================

    public async Task<PagedResultDto<VentaNavDto>> ObtenerTodosAsync(VentaNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(x => x.Cliente)
                .Include(x => x.Vendedor)
                .Include(x => x.Detalles)
                .Include(x => x.Bloqueos.Where(b => b.IsActive))
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            if (query.Canal.HasValue)
            {
                var canal = (CanalVentaNav)query.Canal.Value;
                baseQuery = baseQuery.Where(x => x.Canal == canal);

                // No admin pidiendo canal Ruta: solo ve sus propias ventas de ruta (sin vendedorActual, lista vacía)
                if (canal == CanalVentaNav.Ruta)
                {
                    var (esAdminRuta, vendedorRuta) = await ObtenerPermisosAsync(temporadaId);
                    if (!esAdminRuta)
                    {
                        var vendedorRutaId = vendedorRuta?.Id ?? 0;
                        baseQuery = baseQuery.Where(x => x.VendedorId == vendedorRutaId);
                    }
                }
            }
            if (query.Tipo.HasValue)
            {
                var tipo = (TipoVentaNav)query.Tipo.Value;
                baseQuery = baseQuery.Where(x => x.Tipo == tipo);
            }
            if (query.CondicionPago.HasValue)
            {
                var cond = (CondicionPagoVentaNav)query.CondicionPago.Value;
                baseQuery = baseQuery.Where(x => x.CondicionPago == cond);
            }
            if (query.EstadoPago.HasValue)
            {
                var estado = (EstadoPagoVentaNav)query.EstadoPago.Value;
                baseQuery = baseQuery.Where(x => x.EstadoPago == estado);
            }
            if (query.EstadoEntrega.HasValue)
            {
                var estado = (EstadoEntregaVentaNav)query.EstadoEntrega.Value;
                baseQuery = baseQuery.Where(x => x.EstadoEntrega == estado);
            }
            if (query.EntregaPendiente.HasValue)
            {
                baseQuery = query.EntregaPendiente.Value
                    ? baseQuery.Where(x => x.EstadoEntrega != EstadoEntregaVentaNav.Entregado)
                    : baseQuery.Where(x => x.EstadoEntrega == EstadoEntregaVentaNav.Entregado);
            }
            if (query.Bloqueada.HasValue)
                baseQuery = query.Bloqueada.Value
                    ? baseQuery.Where(x => x.Bloqueos.Any(b => b.IsActive))
                    : baseQuery.Where(x => !x.Bloqueos.Any(b => b.IsActive));
            if (query.AlmacenId.HasValue)
                baseQuery = baseQuery.Where(x => x.AlmacenId == query.AlmacenId.Value);
            if (query.VendedorId.HasValue)
                baseQuery = baseQuery.Where(x => x.VendedorId == query.VendedorId.Value);
            if (query.ClienteId.HasValue)
                baseQuery = baseQuery.Where(x => x.ClienteId == query.ClienteId.Value);
            if (query.NroVenta.HasValue)
                baseQuery = baseQuery.Where(x => x.NroVenta == query.NroVenta.Value);
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
            if (query.ProductoId.HasValue)
            {
                var productoId = query.ProductoId.Value;
                baseQuery = baseQuery.Where(x => x.Detalles.Any(d => d.ProductoId == productoId && d.Unidades > 0));
            }

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.Trim().ToLower();
                var esNumero = int.TryParse(f, out var nro);
                baseQuery = baseQuery.Where(x =>
                    (x.Cliente != null && x.Cliente.Nombre.ToLower().Contains(f)) ||
                    (x.Observacion != null && x.Observacion.ToLower().Contains(f)) ||
                    (esNumero && x.NroVenta == nro));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var almacenes = await ObtenerNombresAlmacenAsync(entidades.Where(e => e.AlmacenId.HasValue).Select(e => e.AlmacenId!.Value));
            var usuarios = await ObtenerNombresUsuarioAsync(
                entidades.Select(e => e.UsuarioId).Concat(entidades.Where(e => e.Vendedor != null).Select(e => e.Vendedor!.UsuarioId)));
            var (esAdmin, vendedorActual) = await ObtenerPermisosAsync(temporadaId);

            return new PagedResultDto<VentaNavDto>
            {
                Items = entidades.Select(e =>
                {
                    var dto = MapearCabecera(e, almacenes, usuarios, esAdmin, vendedorActual?.Id);
                    if (query.ProductoId.HasValue)
                    {
                        var delProducto = e.Detalles.Where(d => d.ProductoId == query.ProductoId.Value && d.Unidades > 0).ToList();
                        dto.UnidadesProducto = delProducto.Sum(d => d.Unidades);
                        dto.UnidadesEntregadasProducto = delProducto.Sum(d => d.UnidadesEntregadas);
                    }
                    return dto;
                }).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ventas");
            throw;
        }
    }

    public async Task<VentaNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return await MapearConDetallesAsync(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener venta {Id}", id);
            throw;
        }
    }

    public async Task<VentaContextoDto> ObtenerContextoAsync()
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var (esAdmin, vendedor) = await ObtenerPermisosAsync(temporada.Id);

            var contexto = new VentaContextoDto { EsAdmin = esAdmin };
            if (vendedor != null)
            {
                var usuarios = await ObtenerNombresUsuarioAsync(new[] { vendedor.UsuarioId });
                contexto.VendedorActual = new VendedorActualDto
                {
                    Id = vendedor.Id,
                    Nombre = usuarios.TryGetValue(vendedor.UsuarioId, out var n) ? n : string.Empty,
                    Tipo = (int)vendedor.Tipo,
                    TipoNombre = vendedor.Tipo == TipoVendedor.Tienda ? "Tienda" : "Ruta"
                };
            }
            return contexto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener contexto de ventas");
            throw;
        }
    }

    public async Task<List<VentaProductoNavDto>> ObtenerProductosAsync(int almacenId)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await ValidarAlmacenAsync(almacenId, exigirTienda: false);

            var productos = await _productoRepo.AsQueryable()
                .AsNoTracking()
                .Where(p => p.IsActive && p.TemporadaId == temporada.Id)
                .ToListAsync();

            var stocks = await _stockRepo.AsQueryable()
                .AsNoTracking()
                .Where(s => s.AlmacenId == almacenId)
                .ToDictionaryAsync(s => s.ProductoId, s => s.Cantidad);
            var bloqueados = await _stock.ObtenerBloqueadosAsync(temporada.Id, almacenId);

            return productos
                .Select(p =>
                {
                    var stock = stocks.GetValueOrDefault(p.Id);
                    var bloqueado = bloqueados.GetValueOrDefault((almacenId, p.Id));
                    return new VentaProductoNavDto
                    {
                        ProductoId = p.Id,
                        ProductoNombreMostrar = NombreMostrar(p),
                        UnidadesPorEmpaque = p.UnidadesPorEmpaque,
                        NombreEmpaque = p.NombreEmpaque,
                        Color = p.Color,
                        PrecioCatalogo = p.PrecioCatalogo,
                        PrecioCompraUnidad = p.PrecioCompraUnidad,
                        Stock = stock,
                        Bloqueado = bloqueado,
                        Disponible = Math.Max(0, stock - bloqueado)
                    };
                })
                .OrderBy(x => x.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener productos para venta");
            throw;
        }
    }

    // ================= Crear =================

    public Task<VentaNavDto> CrearAsync(CrearVentaNavDto dto) => CrearCoreAsync(dto, desdeCierre: false);

    // Venta del faltante "Venta no registrada" del cierre de conteo: corre dentro de la transacción del conteo
    // (no abre otra), a precio de catálogo (sin R4) y sin exigir apertura de hoy (el cierre atrasado también la crea)
    public Task<VentaNavDto> CrearDesdeCierreAsync(CrearVentaNavDto dto) => CrearCoreAsync(dto, desdeCierre: true);

    private async Task<VentaNavDto> CrearCoreAsync(CrearVentaNavDto dto, bool desdeCierre)
    {
        try
        {
            ValidarEstructuraCrear(dto);

            var tipo = (TipoVentaNav)dto.Tipo;
            var condicion = (CondicionPagoVentaNav)dto.CondicionPago;

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await ValidarAlmacenAsync(dto.AlmacenId, exigirTienda: true);
            // EncargadoAlmacen: solo vende en sus tiendas
            await _acceso.VerificarAlmacenAsync(temporada.Id, dto.AlmacenId);

            var (esAdmin, vendedorActual) = await ObtenerPermisosAsync(temporada.Id);
            var vendedorId = await ResolverVendedorAsync(temporada.Id, vendedorActual, dto.VendedorId, esAdmin);

            var productos = await CargarProductosAsync(temporada.Id, dto.Detalles.Select(d => d.ProductoId));
            foreach (var l in dto.Detalles)
                if (!productos[l.ProductoId].IsActive)
                    throw new ValidacionException($"El producto {NombreMostrar(productos[l.ProductoId])} está inactivo");

            var (total, descuento, neto) = CalcularTotales(dto.Detalles, dto.Descuento);
            if (!desdeCierre)
                ValidarPrecios(dto.Detalles, productos, neto, esAdmin, await _precioAcceso.PuedeVerPreciosCompraAsync());

            var cobroMonto = ResolverCobroInicial(neto, condicion, dto.Cobro);

            var saldo = neto - cobroMonto;
            if (dto.ClienteId.HasValue)
                await ValidarClienteAsync(dto.ClienteId.Value);
            else if (tipo == TipoVentaNav.Reserva || condicion == CondicionPagoVentaNav.Credito || saldo > 0)
                throw new ValidacionException("El cliente es obligatorio en reservas, ventas a crédito o con saldo pendiente");

            // Sale stock de la tienda en el momento: exige apertura de hoy si la tienda lleva conteo
            if (!desdeCierre)
                await _conteo.VerificarAperturaParaVentaAsync(dto.AlmacenId);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;
            var hoy = BoliviaTimeZone.Now().Date;

            var venta = new VentaNav
            {
                TemporadaId = temporada.Id,
                Fecha = hoy,
                AlmacenId = dto.AlmacenId,
                Canal = CanalVentaNav.Tienda,
                UsuarioId = usuarioId,
                VendedorId = vendedorId,
                ClienteId = dto.ClienteId,
                Tipo = tipo,
                CondicionPago = condicion,
                MontoTotal = total,
                Descuento = descuento,
                MontoNeto = neto,
                MontoCobrado = cobroMonto,
                MontoSaldo = saldo,
                EstadoPago = EstadoPagoDe(neto, cobroMonto),
                Observacion = NormalizarTexto(dto.Observacion),
                Anulada = false
            };

            // RepositorioBase solo pone auditoría en la raíz
            foreach (var l in dto.Detalles)
                venta.Detalles.Add(new VentaNavDetalle
                {
                    ProductoId = l.ProductoId,
                    Unidades = l.Unidades,
                    PrecioUnitario = Redondear(l.PrecioUnitario),
                    Subtotal = Redondear(l.Unidades * l.PrecioUnitario),
                    UnidadesEntregadas = 0,
                    EstadoEntrega = EstadoEntregaVentaNav.Pendiente,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });

            if (cobroMonto > 0)
                venta.Cobros.Add(new CobroVenta
                {
                    Fecha = hoy,
                    Monto = cobroMonto,
                    Medio = dto.Cobro!.Medio,
                    Comprobante = NormalizarTexto(dto.Cobro.Comprobante),
                    Observacion = NormalizarTexto(dto.Cobro.Observacion),
                    AlmacenId = dto.AlmacenId,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });

            // Directa: entrega total automática desde la tienda
            if (tipo == TipoVentaNav.Directa)
            {
                var entrega = NuevaEntrega(dto.AlmacenId, hoy, usuarioId, ahora);
                foreach (var d in venta.Detalles)
                {
                    d.UnidadesEntregadas = d.Unidades;
                    d.EstadoEntrega = EstadoEntregaVentaNav.Entregado;
                    entrega.Detalles.Add(new EntregaVentaDetalle
                    {
                        VentaDetalle = d,
                        ProductoId = d.ProductoId,
                        Unidades = d.Unidades,
                        CreatedByUsuarioId = usuarioId,
                        CreatedAt = ahora
                    });
                }
                venta.Entregas.Add(entrega);
            }
            RecalcularEntrega(venta);

            if (!desdeCierre) await _uow.BeginTransactionAsync();
            try
            {
                venta.NroVenta = await _repo.ObtenerSiguienteNroAsync(temporada.Id);
                var creada = await _repo.CrearAsync(venta);

                if (tipo == TipoVentaNav.Directa)
                    foreach (var d in creada.Detalles)
                        await _stock.RegistrarSalidaAsync(temporada.Id, dto.AlmacenId, d.ProductoId, d.Unidades,
                            TipoMovimientoNav.Venta, ReferenciaVenta, creada.Id);

                if (!desdeCierre) await _uow.CommitAsync();
            }
            catch
            {
                // Desde el cierre, el rollback lo hace quien abrió la transacción
                if (!desdeCierre) await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Venta creada: {Id} #{Nro} ({Tipo}, temporada {TemporadaId})", venta.Id, venta.NroVenta, tipo, temporada.Id);

            return await ObtenerPorIdAsync(venta.Id)
                ?? throw new EntidadNoEncontradaException("Venta", venta.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear venta");
            throw;
        }
    }

    // ================= Editar =================

    public async Task<VentaNavDto> EditarAsync(int id, EditarVentaNavDto dto)
    {
        try
        {
            ValidarEstructuraEditar(dto);

            var venta = await ObtenerVentaParaEdicionAsync(id);
            if (venta.Anulada)
                throw new ValidacionException("La venta está anulada");
            if (venta.Tipo == TipoVentaNav.Reserva)
                throw new ValidacionException(MensajeReservaEnReservas);
            if (venta.Canal != CanalVentaNav.Tienda)
                throw new ValidacionException("Solo se pueden editar ventas de tienda; las de ruta se editan desde el módulo de ruta");
            var almacenVentaId = venta.AlmacenId ?? throw new ValidacionException("La venta no tiene tienda asignada");
            await _acceso.VerificarAlmacenAsync(venta.TemporadaId, almacenVentaId);

            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);
            var (esAdmin, vendedorActual) = await ObtenerPermisosAsync(venta.TemporadaId);
            VerificarPropietario(venta, esAdmin, vendedorActual);

            var productos = await CargarProductosAsync(venta.TemporadaId, dto.Detalles.Select(d => d.ProductoId));
            var existentes = venta.Detalles.ToDictionary(d => d.ProductoId);

            // Un producto inactivo solo se tolera si la línea ya existía y no sube de cantidad
            foreach (var l in dto.Detalles)
            {
                var p = productos[l.ProductoId];
                if (p.IsActive) continue;
                if (!existentes.TryGetValue(l.ProductoId, out var previa) || l.Unidades > previa.Unidades)
                    throw new ValidacionException($"El producto {NombreMostrar(p)} está inactivo");
            }

            var (total, descuento, neto) = CalcularTotales(dto.Detalles, dto.Descuento);
            ValidarPrecios(dto.Detalles, productos, neto, esAdmin, await _precioAcceso.PuedeVerPreciosCompraAsync());

            if (neto < venta.MontoCobrado)
                throw new ExcepcionDominio(
                    $"El nuevo neto ({neto}) es menor a lo ya cobrado ({venta.MontoCobrado}): primero anule un cobro",
                    409, "NETO_MENOR_COBRADO");

            var saldo = neto - venta.MontoCobrado;
            if (dto.ClienteId.HasValue)
                await ValidarClienteAsync(dto.ClienteId.Value);
            else if (venta.Tipo == TipoVentaNav.Reserva || venta.CondicionPago == CondicionPagoVentaNav.Credito || saldo > 0)
                throw new ValidacionException("El cliente es obligatorio en reservas, ventas a crédito o con saldo pendiente");

            var nuevas = dto.Detalles.ToDictionary(l => l.ProductoId);

            // Plan por línea existente (las que faltan en el body quedan en 0)
            var aDevolver = new List<(VentaNavDetalle Linea, int Cantidad)>();
            var aSumarEnEntrega = 0;
            foreach (var d in venta.Detalles)
            {
                var nuevaCantidad = nuevas.TryGetValue(d.ProductoId, out var l) ? l.Unidades : 0;
                if (nuevaCantidad < d.UnidadesEntregadas)
                    aDevolver.Add((d, d.UnidadesEntregadas - nuevaCantidad));
                var entregadasFinal = Math.Min(d.UnidadesEntregadas, nuevaCantidad);
                if (venta.Tipo == TipoVentaNav.Directa && nuevaCantidad > entregadasFinal)
                    aSumarEnEntrega += nuevaCantidad - entregadasFinal;
            }
            foreach (var l in nuevas.Values.Where(l => !existentes.ContainsKey(l.ProductoId)))
                if (venta.Tipo == TipoVentaNav.Directa)
                    aSumarEnEntrega += l.Unidades;

            // Directa: el extra sale de la tienda (apertura de hoy si lleva conteo)
            if (aSumarEnEntrega > 0)
                await _conteo.VerificarAperturaParaVentaAsync(almacenVentaId);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                // 1) Bajar unidades ya entregadas: vuelven a la tienda de la venta (DevolucionVenta) y se descuentan de las entregas más recientes
                foreach (var (linea, cantidad) in aDevolver)
                {
                    await _stock.RegistrarEntradaAsync(venta.TemporadaId, almacenVentaId, linea.ProductoId, cantidad,
                        TipoMovimientoNav.DevolucionVenta, ReferenciaVenta, venta.Id);
                    ReducirEntregas(venta, linea, cantidad);
                    linea.UnidadesEntregadas -= cantidad;
                }

                // 2) Líneas: actualizar existentes (las que faltan quedan en 0) y agregar las nuevas
                foreach (var d in venta.Detalles)
                {
                    if (nuevas.TryGetValue(d.ProductoId, out var l))
                    {
                        d.Unidades = l.Unidades;
                        d.PrecioUnitario = Redondear(l.PrecioUnitario);
                        d.Subtotal = Redondear(l.Unidades * l.PrecioUnitario);
                    }
                    else
                    {
                        d.Unidades = 0;
                        d.Subtotal = 0;
                    }
                    Tocar(d, usuarioId, ahora);
                }
                foreach (var l in nuevas.Values.Where(l => !existentes.ContainsKey(l.ProductoId)))
                    venta.Detalles.Add(new VentaNavDetalle
                    {
                        ProductoId = l.ProductoId,
                        Unidades = l.Unidades,
                        PrecioUnitario = Redondear(l.PrecioUnitario),
                        Subtotal = Redondear(l.Unidades * l.PrecioUnitario),
                        UnidadesEntregadas = 0,
                        EstadoEntrega = EstadoEntregaVentaNav.Pendiente,
                        CreatedByUsuarioId = usuarioId,
                        CreatedAt = ahora
                    });
                await _uow.SaveChangesAsync();

                // 3) Directa: lo que sube sale de la tienda con una entrega automática
                if (venta.Tipo == TipoVentaNav.Directa)
                {
                    var extras = venta.Detalles.Where(d => d.Unidades > d.UnidadesEntregadas).ToList();
                    if (extras.Count > 0)
                    {
                        var hoy = BoliviaTimeZone.Now().Date;
                        var entrega = NuevaEntrega(almacenVentaId, hoy, usuarioId, ahora);
                        foreach (var d in extras)
                        {
                            var cantidad = d.Unidades - d.UnidadesEntregadas;
                            await _stock.RegistrarSalidaAsync(venta.TemporadaId, almacenVentaId, d.ProductoId, cantidad,
                                TipoMovimientoNav.Venta, ReferenciaVenta, venta.Id);
                            entrega.Detalles.Add(new EntregaVentaDetalle
                            {
                                VentaDetalleId = d.Id,
                                ProductoId = d.ProductoId,
                                Unidades = cantidad,
                                CreatedByUsuarioId = usuarioId,
                                CreatedAt = ahora
                            });
                            d.UnidadesEntregadas = d.Unidades;
                        }
                        venta.Entregas.Add(entrega);
                    }
                }

                // 4) Cabecera: montos y estados
                venta.ClienteId = dto.ClienteId;
                venta.Descuento = descuento;
                venta.MontoTotal = total;
                venta.MontoNeto = neto;
                venta.Observacion = NormalizarTexto(dto.Observacion);
                RecalcularEntrega(venta);
                RecalcularPago(venta);
                Tocar(venta, usuarioId, ahora);
                await _uow.SaveChangesAsync();

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Venta editada: {Id} #{Nro}", venta.Id, venta.NroVenta);

            return await ObtenerPorIdAsync(venta.Id)
                ?? throw new EntidadNoEncontradaException("Venta", venta.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar venta {Id}", id);
            throw;
        }
    }

    // ================= Entregas =================

    public async Task<VentaNavDto> AnularEntregaAsync(int entregaId)
    {
        try
        {
            var ventaId = await _entregaRepo.AsQueryable()
                .AsNoTracking()
                .Where(e => e.Id == entregaId)
                .Select(e => (int?)e.VentaId)
                .FirstOrDefaultAsync();
            if (ventaId == null)
                throw new EntidadNoEncontradaException("Entrega", entregaId);

            var venta = await ObtenerVentaParaEdicionAsync(ventaId.Value);
            var entrega = venta.Entregas.First(e => e.Id == entregaId);
            if (entrega.Anulada)
                throw new ValidacionException("La entrega ya está anulada");
            if (venta.Anulada)
                throw new ValidacionException("La venta está anulada");
            if (venta.Tipo != TipoVentaNav.Reserva)
                throw new ValidacionException("La entrega de una venta directa no se anula por separado: anule la venta");

            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);
            var (esAdmin, vendedorActual) = await ObtenerPermisosAsync(venta.TemporadaId);
            VerificarPropietario(venta, esAdmin, vendedorActual);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                // El stock vuelve al almacén de la entrega; lo pendiente de cada línea vuelve a subir
                foreach (var ed in entrega.Detalles.Where(x => x.Unidades > 0))
                {
                    await _stock.RegistrarEntradaAsync(venta.TemporadaId, entrega.AlmacenId, ed.ProductoId, ed.Unidades,
                        TipoMovimientoNav.AnulacionVenta, ReferenciaVenta, venta.Id);
                    var linea = venta.Detalles.First(d => d.Id == ed.VentaDetalleId);
                    linea.UnidadesEntregadas -= ed.Unidades;
                    Tocar(linea, usuarioId, ahora);
                }
                entrega.Anulada = true;
                Tocar(entrega, usuarioId, ahora);

                RecalcularEntrega(venta);
                Tocar(venta, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Entrega {EntregaId} anulada (venta {VentaId})", entregaId, venta.Id);
            return await ObtenerPorIdAsync(venta.Id) ?? throw new EntidadNoEncontradaException("Venta", venta.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular entrega {Id}", entregaId);
            throw;
        }
    }

    // ================= Cobros =================

    public async Task<VentaNavDto> RegistrarCobroAsync(int ventaId, RegistrarCobroVentaDto dto)
    {
        try
        {
            var monto = Redondear(dto.Monto);
            if (monto <= 0)
                throw new ValidacionException("El monto del cobro debe ser mayor a 0");
            if (!Enum.IsDefined(dto.Medio))
                throw new ValidacionException("El medio de pago es inválido");
            if (dto.Comprobante != null && dto.Comprobante.Trim().Length > 100)
                throw new ValidacionException("El comprobante no puede superar 100 caracteres");
            if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
                throw new ValidacionException("La observación no puede superar 500 caracteres");

            var venta = await ObtenerVentaParaEdicionAsync(ventaId);
            if (venta.Anulada)
                throw new ValidacionException("La venta está anulada");

            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);

            if (monto > venta.MontoSaldo)
                throw new ValidacionException($"El cobro ({monto}) supera el saldo de la venta ({venta.MontoSaldo})");

            // Caja de tienda: la tienda de la sesión (opcional) tiene que ser una tienda navideña activa
            if (dto.AlmacenId.HasValue)
            {
                await ValidarAlmacenAsync(dto.AlmacenId.Value, exigirTienda: true);
                await _acceso.VerificarAlmacenAsync(venta.TemporadaId, dto.AlmacenId.Value);
            }

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                venta.Cobros.Add(new CobroVenta
                {
                    Fecha = BoliviaTimeZone.Now().Date,
                    Monto = monto,
                    Medio = dto.Medio,
                    Comprobante = NormalizarTexto(dto.Comprobante),
                    Observacion = NormalizarTexto(dto.Observacion),
                    AlmacenId = dto.AlmacenId,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });
                RecalcularPago(venta);
                Tocar(venta, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Cobro registrado en venta {Id}: {Monto}", ventaId, monto);
            return await ObtenerPorIdAsync(ventaId) ?? throw new EntidadNoEncontradaException("Venta", ventaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar cobro de la venta {Id}", ventaId);
            throw;
        }
    }

    public async Task<VentaNavDto> AnularCobroAsync(int cobroId)
    {
        try
        {
            var ventaId = await _cobroRepo.AsQueryable()
                .AsNoTracking()
                .Where(c => c.Id == cobroId)
                .Select(c => (int?)c.VentaId)
                .FirstOrDefaultAsync();
            if (ventaId == null)
                throw new EntidadNoEncontradaException("Cobro", cobroId);

            var venta = await ObtenerVentaParaEdicionAsync(ventaId.Value);
            var cobro = venta.Cobros.First(c => c.Id == cobroId);
            if (cobro.Anulado)
                throw new ValidacionException("El cobro ya está anulado");
            if (venta.Anulada)
                throw new ValidacionException("La venta está anulada");
            if (cobro.AbonoVendedorId != null)
                throw new ExcepcionDominio("El cobro pertenece a un abono de vendedor: anule el abono", 409, "COBRO_DE_ABONO");

            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);
            var (esAdmin, vendedorActual) = await ObtenerPermisosAsync(venta.TemporadaId);
            VerificarPropietario(venta, esAdmin, vendedorActual);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                cobro.Anulado = true;
                Tocar(cobro, usuarioId, ahora);
                RecalcularPago(venta);
                Tocar(venta, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Cobro {CobroId} anulado (venta {VentaId})", cobroId, venta.Id);
            return await ObtenerPorIdAsync(venta.Id) ?? throw new EntidadNoEncontradaException("Venta", venta.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular cobro {Id}", cobroId);
            throw;
        }
    }

    // ================= Anulación =================

    public async Task AnularAsync(int id)
    {
        try
        {
            var venta = await ObtenerVentaParaEdicionAsync(id);
            if (venta.Anulada)
                throw new ValidacionException("La venta ya está anulada");
            if (venta.Canal != CanalVentaNav.Tienda)
                throw new ValidacionException("Solo se pueden anular ventas de tienda; las de ruta se gestionan desde el módulo de ruta");
            if (venta.AlmacenId.HasValue)
                await _acceso.VerificarAlmacenAsync(venta.TemporadaId, venta.AlmacenId.Value);

            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);
            var (esAdmin, vendedorActual) = await ObtenerPermisosAsync(venta.TemporadaId);
            VerificarPropietario(venta, esAdmin, vendedorActual);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                // Entregas: el stock vuelve a su almacén
                foreach (var entrega in venta.Entregas.Where(e => !e.Anulada))
                {
                    foreach (var ed in entrega.Detalles.Where(x => x.Unidades > 0))
                        await _stock.RegistrarEntradaAsync(venta.TemporadaId, entrega.AlmacenId, ed.ProductoId, ed.Unidades,
                            TipoMovimientoNav.AnulacionVenta, ReferenciaVenta, venta.Id);
                    entrega.Anulada = true;
                    Tocar(entrega, usuarioId, ahora);
                }

                foreach (var cobro in venta.Cobros.Where(c => !c.Anulado))
                {
                    cobro.Anulado = true;
                    Tocar(cobro, usuarioId, ahora);
                }

                // Reserva: se eliminan sus bloqueos de stock
                foreach (var bloqueo in venta.Bloqueos.Where(b => b.IsActive))
                    ReservaBloqueoHelper.Desactivar(bloqueo, usuarioId, ahora);

                // Queda marcada como anulada; los reportes de deuda y cobertura la excluyen
                venta.Anulada = true;
                venta.MontoCobrado = 0;
                venta.MontoSaldo = 0;
                venta.EstadoPago = EstadoPagoVentaNav.Pendiente;
                Tocar(venta, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Venta anulada: {Id} #{Nro}", venta.Id, venta.NroVenta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular venta {Id}", id);
            throw;
        }
    }

    // ================= Reglas =================

    private static void ValidarEstructuraCrear(CrearVentaNavDto dto)
    {
        if (dto.AlmacenId <= 0)
            throw new ValidacionException("La tienda es obligatoria");
        if (!Enum.IsDefined(typeof(TipoVentaNav), dto.Tipo))
            throw new ValidacionException("El tipo de venta es inválido");
        if (!Enum.IsDefined(typeof(CondicionPagoVentaNav), dto.CondicionPago))
            throw new ValidacionException("La condición de pago es inválida");
        if (dto.Tipo == (int)TipoVentaNav.Reserva)
            throw new ValidacionException(MensajeReservaEnReservas);
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        if (dto.Cobro != null)
        {
            if (dto.Cobro.Comprobante != null && dto.Cobro.Comprobante.Trim().Length > 100)
                throw new ValidacionException("El comprobante no puede superar 100 caracteres");
            if (dto.Cobro.Observacion != null && dto.Cobro.Observacion.Trim().Length > 500)
                throw new ValidacionException("La observación del cobro no puede superar 500 caracteres");
        }
        ValidarLineas(dto.Detalles, dto.Descuento);
    }

    private static void ValidarEstructuraEditar(EditarVentaNavDto dto)
    {
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        ValidarLineas(dto.Detalles, dto.Descuento);
    }

    internal static void ValidarLineas(List<VentaLineaDto> detalles, decimal descuento)
    {
        if (descuento < 0)
            throw new ValidacionException("El descuento no puede ser negativo");
        if (detalles == null || detalles.Count == 0)
            throw new ValidacionException("Debe registrar al menos un producto");
        if (detalles.Any(d => d.ProductoId <= 0))
            throw new ValidacionException("El producto es obligatorio");
        if (detalles.Select(d => d.ProductoId).Distinct().Count() != detalles.Count)
            throw new ValidacionException("Hay productos repetidos en la venta");
        if (detalles.Any(d => d.Unidades <= 0))
            throw new ValidacionException("Las unidades deben ser mayores a 0");
        if (detalles.Any(d => d.PrecioUnitario < 0))
            throw new ValidacionException("El precio unitario no puede ser negativo");
    }

    // Cobro inicial: Contado = neto (medio obligatorio); Crédito = pago inicial opcional (> 0 y < neto). Devuelve el monto cobrado al crear
    internal static decimal ResolverCobroInicial(decimal neto, CondicionPagoVentaNav condicion, CobroInicialVentaDto? cobro)
    {
        decimal cobroMonto = 0;
        if (neto > 0)
        {
            if (condicion == CondicionPagoVentaNav.Contado)
            {
                if (cobro == null)
                    throw new ValidacionException("El medio de pago es obligatorio para una venta al contado");
                if (cobro.Monto.HasValue && Redondear(cobro.Monto.Value) != neto)
                    throw new ValidacionException($"En una venta al contado el cobro debe ser igual al neto ({neto})");
                cobroMonto = neto;
            }
            else if (cobro != null)
            {
                var m = Redondear(cobro.Monto ?? 0);
                if (m <= 0 || m >= neto)
                    throw new ValidacionException($"El pago inicial debe ser mayor a 0 y menor al neto ({neto}); si se cobra todo, use contado");
                cobroMonto = m;
            }
        }
        if (cobro != null && !Enum.IsDefined(cobro.Medio))
            throw new ValidacionException("El medio de pago es inválido");
        return cobroMonto;
    }

    // Total de líneas (subtotales redondeados), descuento y neto
    internal static (decimal Total, decimal Descuento, decimal Neto) CalcularTotales(List<VentaLineaDto> detalles, decimal descuentoDto)
    {
        var total = Redondear(detalles.Sum(l => Redondear(l.Unidades * l.PrecioUnitario)));
        var descuento = Redondear(descuentoDto);
        if (descuento > total)
            throw new ValidacionException($"El descuento ({descuento}) no puede superar el total ({total})");
        return (total, descuento, total - descuento);
    }

    // R4: nunca por debajo del precio de compra (por línea y en el neto); solo el admin puede. Compra 0 = sin restricción
    internal static void ValidarPrecios(List<VentaLineaDto> detalles, Dictionary<int, ProductoNav> productos, decimal neto, bool esAdmin, bool verCosto = true)
    {
        if (esAdmin) return;

        foreach (var l in detalles)
        {
            var p = productos[l.ProductoId];
            if (p.PrecioCompraUnidad > 0 && l.PrecioUnitario < p.PrecioCompraUnidad)
                throw new ExcepcionDominio(
                    verCosto
                        ? $"El precio de {NombreMostrar(p)} ({Redondear(l.PrecioUnitario)}) es menor al precio de compra ({p.PrecioCompraUnidad}). Solo el admin puede vender por debajo de la compra"
                        : $"El precio de {NombreMostrar(p)} está por debajo del permitido",
                    400, "PRECIO_BAJO_COMPRA");
        }

        var costo = Redondear(detalles.Sum(l => l.Unidades * productos[l.ProductoId].PrecioCompraUnidad));
        if (neto < costo)
            throw new ExcepcionDominio(
                verCosto
                    ? $"El neto de la venta ({neto}) es menor al precio de compra de los productos ({costo}). Solo el admin puede vender por debajo de la compra"
                    : $"El neto de la venta ({neto}) está por debajo del permitido",
                400, "PRECIO_BAJO_COMPRA");
    }

    internal async Task<Dictionary<int, ProductoNav>> CargarProductosAsync(int temporadaId, IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        var productos = await _productoRepo.AsQueryable()
            .AsNoTracking()
            .Where(p => lista.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var id in lista)
            if (!productos.TryGetValue(id, out var p) || p.TemporadaId != temporadaId)
                throw new ValidacionException($"El producto {id} no existe o no es de la temporada");

        return productos;
    }

    internal async Task ValidarClienteAsync(int clienteId)
    {
        var cliente = await _clienteRepo.AsQueryable().AsNoTracking().FirstOrDefaultAsync(c => c.Id == clienteId);
        if (cliente == null || !cliente.IsActive)
            throw new ValidacionException("El cliente no existe o está inactivo");
    }

    // AuthDB: existe, es del sistema navideño y está activo (y tienda, si se exige)
    internal async Task ValidarAlmacenAsync(int almacenId, bool exigirTienda)
    {
        var almacen = await _almacenRepo.AsQueryable().AsNoTracking().FirstOrDefaultAsync(a => a.Id == almacenId);
        if (almacen == null || !almacen.IsActive || almacen.SistemaId != SistemaNavidadId)
            throw new ValidacionException($"El almacén {almacenId} no es un almacén navideño activo");
        if (exigirTienda && !almacen.EsTienda)
            throw new ValidacionException($"El almacén {almacen.Nombre} no es una tienda");
    }

    // Admin = rol "Admin" (mismo criterio que EsAdminAsync del regular); vendedor = el activo de la temporada vinculado al usuario
    internal async Task<(bool EsAdmin, Vendedor? Vendedor)> ObtenerPermisosAsync(int temporadaId)
    {
        var esAdmin = false;
        var rolId = _currentUser.GetRolId();
        if (rolId.HasValue)
        {
            var rol = await _rolRepo.ObtenerPorIdAsync(rolId.Value);
            esAdmin = rol != null && string.Equals(rol.Nombre, "Admin", StringComparison.OrdinalIgnoreCase);
        }

        Vendedor? vendedor = null;
        var usuarioId = _currentUser.GetUsuarioId();
        if (usuarioId.HasValue)
            vendedor = await _vendedorRepo.AsQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.TemporadaId == temporadaId && v.IsActive && v.UsuarioId == usuarioId.Value);

        return (esAdmin, vendedor);
    }

    // Si el usuario es vendedor de la temporada queda él mismo; si no, tiene que elegir uno activo
    internal async Task<int> ResolverVendedorAsync(int temporadaId, Vendedor? vendedorActual, int? vendedorId, bool esAdmin = false)
    {
        // Admin: si manda vendedorId se respeta (vende a nombre de otro); si no, usa el suyo
        var adminElige = esAdmin && vendedorId.HasValue && vendedorId.Value > 0;
        if (vendedorActual != null && !adminElige) return vendedorActual.Id;

        if (!vendedorId.HasValue || vendedorId.Value <= 0)
            throw new ValidacionException("El vendedor es obligatorio");

        var existe = await _vendedorRepo.AsQueryable()
            .AsNoTracking()
            .AnyAsync(v => v.Id == vendedorId.Value && v.TemporadaId == temporadaId && v.IsActive);
        if (!existe)
            throw new ValidacionException("El vendedor no existe, está inactivo o no es de la temporada");
        return vendedorId.Value;
    }

    // El vendedor opera solo sus propias ventas; el admin, cualquiera
    internal static void VerificarPropietario(VentaNav venta, bool esAdmin, Vendedor? vendedorActual)
    {
        if (esAdmin) return;
        if (vendedorActual != null && vendedorActual.Id == venta.VendedorId) return;
        throw new ExcepcionDominio("Solo el vendedor de la venta o el admin pueden realizar esta acción", 403, "SIN_PERMISO");
    }

    internal async Task<VentaNav> ObtenerVentaParaEdicionAsync(int id)
    {
        var venta = await _repo.ObtenerParaEdicionAsync(id);
        if (venta == null || !venta.IsActive)
            throw new EntidadNoEncontradaException("Venta", id);
        return venta;
    }

    // ================= Cálculo de estados =================

    internal static EstadoPagoVentaNav EstadoPagoDe(decimal neto, decimal cobrado)
        => neto - cobrado <= 0 ? EstadoPagoVentaNav.Pagado
            : cobrado > 0 ? EstadoPagoVentaNav.Parcial : EstadoPagoVentaNav.Pendiente;

    internal static void RecalcularPago(VentaNav venta)
    {
        venta.MontoCobrado = Redondear(venta.Cobros.Where(c => !c.Anulado).Sum(c => c.Monto));
        venta.MontoSaldo = venta.MontoNeto - venta.MontoCobrado;
        venta.EstadoPago = EstadoPagoDe(venta.MontoNeto, venta.MontoCobrado);
    }

    // Estado de cada línea y de la cabecera (solo cuentan las líneas con unidades)
    internal static void RecalcularEntrega(VentaNav venta)
    {
        foreach (var d in venta.Detalles)
            d.EstadoEntrega = d.Unidades == 0 || d.UnidadesEntregadas == 0 ? EstadoEntregaVentaNav.Pendiente
                : d.UnidadesEntregadas >= d.Unidades ? EstadoEntregaVentaNav.Entregado
                : EstadoEntregaVentaNav.Parcial;

        var activas = venta.Detalles.Where(d => d.Unidades > 0).ToList();
        venta.EstadoEntrega = activas.Count == 0 || activas.All(d => d.UnidadesEntregadas == 0) ? EstadoEntregaVentaNav.Pendiente
            : activas.All(d => d.UnidadesEntregadas >= d.Unidades) ? EstadoEntregaVentaNav.Entregado
            : EstadoEntregaVentaNav.Parcial;
    }

    // Mantiene Σ entregas no anuladas = UnidadesEntregadas: al bajar una línea ya entregada se descuenta de las entregas más recientes
    internal static void ReducirEntregas(VentaNav venta, VentaNavDetalle linea, int cantidad)
    {
        var restante = cantidad;
        var detallesEntrega = venta.Entregas
            .Where(e => !e.Anulada)
            .OrderByDescending(e => e.Id)
            .SelectMany(e => e.Detalles.Where(x => x.VentaDetalleId == linea.Id && x.Unidades > 0));

        foreach (var ed in detallesEntrega)
        {
            if (restante <= 0) break;
            var quitar = Math.Min(restante, ed.Unidades);
            ed.Unidades -= quitar;
            restante -= quitar;
        }
    }

    internal static EntregaVenta NuevaEntrega(int almacenId, DateTime fecha, int usuarioId, DateTime ahora) => new()
    {
        AlmacenId = almacenId,
        Fecha = fecha,
        UsuarioId = usuarioId,
        Anulada = false,
        CreatedByUsuarioId = usuarioId,
        CreatedAt = ahora
    };

    // Auditoría de modificación en entidades hijas / cabecera (RepositorioBase solo la pone en la raíz al guardar por repositorio)
    internal static void Tocar(BussinesMS.Dominio.Entidades.Compartido.EntidadBase entidad, int usuarioId, DateTime ahora)
    {
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = ahora;
    }

    // ================= Mapeo =================

    private static string NombreTipo(TipoVentaNav t) => t == TipoVentaNav.Directa ? "Directa" : "Reserva";
    private static string NombreCondicion(CondicionPagoVentaNav c) => c == CondicionPagoVentaNav.Contado ? "Contado" : "Crédito";
    private static string NombreCanal(CanalVentaNav c) => c == CanalVentaNav.Tienda ? "Tienda" : "Ruta";
    private static string NombreEstadoPago(EstadoPagoVentaNav e) => e switch
    {
        EstadoPagoVentaNav.Pagado => "Pagado",
        EstadoPagoVentaNav.Parcial => "Parcial",
        _ => "Pendiente"
    };
    private static string NombreEstadoEntrega(EstadoEntregaVentaNav e) => e switch
    {
        EstadoEntregaVentaNav.Entregado => "Entregado",
        EstadoEntregaVentaNav.Parcial => "Parcial",
        _ => "Pendiente"
    };

    private static VentaNavDto MapearCabecera(VentaNav e, Dictionary<int, string> almacenes, Dictionary<int, string> usuarios,
        bool esAdmin, int? vendedorActualId)
    {
        var activas = e.Detalles.Where(d => d.Unidades > 0).ToList();
        var puede = e.Canal == CanalVentaNav.Tienda && !e.Anulada && (esAdmin || (vendedorActualId.HasValue && vendedorActualId == e.VendedorId));
        return new VentaNavDto
        {
            Id = e.Id,
            TemporadaId = e.TemporadaId,
            NroVenta = e.NroVenta,
            Fecha = e.Fecha,
            AlmacenId = e.AlmacenId,
            AlmacenNombre = e.AlmacenId.HasValue && almacenes.TryGetValue(e.AlmacenId.Value, out var a) ? a : string.Empty,
            Canal = (int)e.Canal,
            CanalNombre = NombreCanal(e.Canal),
            UsuarioId = e.UsuarioId,
            UsuarioNombre = usuarios.TryGetValue(e.UsuarioId, out var u) ? u : string.Empty,
            VendedorId = e.VendedorId,
            VendedorNombre = e.Vendedor != null && usuarios.TryGetValue(e.Vendedor.UsuarioId, out var vn) ? vn : string.Empty,
            ClienteId = e.ClienteId,
            ClienteNombre = e.Cliente?.Nombre,
            Tipo = (int)e.Tipo,
            TipoNombre = NombreTipo(e.Tipo),
            CondicionPago = (int)e.CondicionPago,
            CondicionPagoNombre = NombreCondicion(e.CondicionPago),
            BloquearStock = e.Bloqueos.Any(b => b.IsActive),
            UnidadesBloqueadas = e.Bloqueos.Where(b => b.IsActive).Sum(b => b.Unidades),
            MontoTotal = e.MontoTotal,
            Descuento = e.Descuento,
            MontoNeto = e.MontoNeto,
            MontoCobrado = e.MontoCobrado,
            MontoSaldo = e.MontoSaldo,
            EstadoPago = (int)e.EstadoPago,
            EstadoPagoNombre = NombreEstadoPago(e.EstadoPago),
            EstadoEntrega = (int)e.EstadoEntrega,
            EstadoEntregaNombre = NombreEstadoEntrega(e.EstadoEntrega),
            Anulada = e.Anulada,
            Observacion = e.Observacion,
            CantidadProductos = activas.Count,
            TotalUnidades = activas.Sum(d => d.Unidades),
            UnidadesPendientes = activas.Sum(d => d.Unidades - d.UnidadesEntregadas),
            PuedeEditar = puede,
            PuedeAnular = puede,
            IsActive = e.IsActive,
            CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt)
        };
    }

    private async Task<VentaNavDto> MapearConDetallesAsync(VentaNav e)
    {
        var almacenIds = e.Entregas.Select(x => x.AlmacenId)
            .Concat(e.AlmacenId.HasValue ? new[] { e.AlmacenId.Value } : Array.Empty<int>())
            .Concat(e.Bloqueos.Select(x => x.AlmacenId));
        var almacenes = await ObtenerNombresAlmacenAsync(almacenIds);
        var usuarioIds = new List<int> { e.UsuarioId };
        if (e.Vendedor != null) usuarioIds.Add(e.Vendedor.UsuarioId);
        usuarioIds.AddRange(e.Entregas.Select(x => x.UsuarioId));
        usuarioIds.AddRange(e.Cobros.Select(x => x.CreatedByUsuarioId));
        var usuarios = await ObtenerNombresUsuarioAsync(usuarioIds);
        var (esAdmin, vendedorActual) = await ObtenerPermisosAsync(e.TemporadaId);

        var dto = MapearCabecera(e, almacenes, usuarios, esAdmin, vendedorActual?.Id);

        // Las líneas quitadas (0 unidades) no se muestran; siguen existiendo por el historial de entregas
        dto.Detalles = e.Detalles
            .Where(d => d.Unidades > 0)
            .OrderBy(d => d.Id)
            .Select(d => new VentaNavDetalleDto
            {
                Id = d.Id,
                ProductoId = d.ProductoId,
                ProductoNombreMostrar = d.Producto != null ? NombreMostrar(d.Producto) : string.Empty,
                UnidadesPorEmpaque = d.Producto?.UnidadesPorEmpaque,
                NombreEmpaque = d.Producto?.NombreEmpaque,
                Color = d.Producto?.Color,
                Unidades = d.Unidades,
                PrecioUnitario = d.PrecioUnitario,
                Subtotal = d.Subtotal,
                PrecioCatalogo = d.Producto?.PrecioCatalogo ?? 0,
                PrecioCompraUnidad = d.Producto?.PrecioCompraUnidad ?? 0,
                UnidadesEntregadas = d.UnidadesEntregadas,
                UnidadesPendientes = d.Unidades - d.UnidadesEntregadas,
                EstadoEntrega = (int)d.EstadoEntrega,
                EstadoEntregaNombre = NombreEstadoEntrega(d.EstadoEntrega),
                Bloqueado = e.Bloqueos.Where(b => b.IsActive && b.VentaDetalleId == d.Id).Sum(b => b.Unidades),
                SinBloqueo = e.Tipo == TipoVentaNav.Reserva && !e.Anulada
                    ? Math.Max(0, d.Unidades - d.UnidadesEntregadas - e.Bloqueos.Where(b => b.IsActive && b.VentaDetalleId == d.Id).Sum(b => b.Unidades))
                    : 0,
                Bloqueos = e.Bloqueos
                    .Where(b => b.IsActive && b.VentaDetalleId == d.Id)
                    .OrderBy(b => b.CreatedAt).ThenBy(b => b.Id)
                    .Select(b => new ReservaBloqueoNavDto
                    {
                        Id = b.Id,
                        VentaDetalleId = b.VentaDetalleId,
                        ProductoId = b.ProductoId,
                        AlmacenId = b.AlmacenId,
                        AlmacenNombre = almacenes.TryGetValue(b.AlmacenId, out var bn) ? bn : string.Empty,
                        Unidades = b.Unidades,
                        RecepcionId = b.RecepcionId,
                        CreatedAt = BoliviaTimeZone.ToLocal(b.CreatedAt)
                    }).ToList()
            }).ToList();

        dto.Entregas = e.Entregas
            .OrderBy(x => x.Id)
            .Select(x => new EntregaVentaDto
            {
                Id = x.Id,
                VentaId = x.VentaId,
                AlmacenId = x.AlmacenId,
                AlmacenNombre = almacenes.TryGetValue(x.AlmacenId, out var an) ? an : string.Empty,
                Fecha = x.Fecha,
                UsuarioId = x.UsuarioId,
                UsuarioNombre = usuarios.TryGetValue(x.UsuarioId, out var un) ? un : string.Empty,
                Anulada = x.Anulada,
                CreatedAt = BoliviaTimeZone.ToLocal(x.CreatedAt),
                // Se ocultan los renglones que quedaron en 0 por una baja de cantidad
                Detalles = x.Detalles
                    .Where(ed => ed.Unidades > 0)
                    .OrderBy(ed => ed.Id)
                    .Select(ed => new EntregaVentaDetalleDto
                    {
                        Id = ed.Id,
                        VentaDetalleId = ed.VentaDetalleId,
                        ProductoId = ed.ProductoId,
                        ProductoNombreMostrar = ed.Producto != null ? NombreMostrar(ed.Producto) : string.Empty,
                        Unidades = ed.Unidades
                    }).ToList()
            }).ToList();

        dto.Cobros = e.Cobros
            .OrderBy(c => c.Id)
            .Select(c => new CobroVentaDto
            {
                Id = c.Id,
                VentaId = c.VentaId,
                Fecha = c.Fecha,
                Monto = c.Monto,
                Medio = (int)c.Medio,
                MedioNombre = c.Medio.ToString(),
                Comprobante = c.Comprobante,
                Observacion = c.Observacion,
                Anulado = c.Anulado,
                UsuarioId = c.CreatedByUsuarioId,
                UsuarioNombre = usuarios.TryGetValue(c.CreatedByUsuarioId, out var cn) ? cn : string.Empty,
                CreatedAt = BoliviaTimeZone.ToLocal(c.CreatedAt)
            }).ToList();

        return dto;
    }

    internal async Task<Dictionary<int, string>> ObtenerNombresAlmacenAsync(IEnumerable<int> almacenIds)
    {
        var ids = almacenIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();
        return await _almacenRepo.AsQueryable()
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Nombre);
    }

    // AuthDB (solo lectura): "Nombre Apellido"; vacío si el usuario ya no existe
    internal async Task<Dictionary<int, string>> ObtenerNombresUsuarioAsync(IEnumerable<int> usuarioIds)
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

    internal static string NombreMostrar(ProductoNav p) => p.Nombre ?? p.Descripcion;

    internal static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    internal static string? NormalizarTexto(string? texto)
    {
        var t = texto?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
