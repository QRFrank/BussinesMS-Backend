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
using static BussinesMS.Aplicacion.Servicios.Navidad.VentaNavService;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Reservas (replanteo 2026-10-15). Reutiliza los helpers internal de VentaNavService (precio R4, vendedor, cliente, cobro inicial,
// estados, entregas). Crear no descuenta stock: el stock baja con cada entrega, desde cualquier almacén.
// Cada línea se cubre con bloqueos por almacén (ReservaBloqueo) y/o con el "por llegar libre" de los pedidos.
public class ReservaNavService : IReservaNavService
{
    private const string ReferenciaVenta = "Venta";
    private const int SistemaNavidadId = 2;

    private readonly VentaNavService _ventas;
    private readonly IReservaCoberturaNavService _cobertura;
    private readonly IVentaNavRepository _repo;
    private readonly IReservaBloqueoNavRepository _bloqueoRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IStockNavService _stock;
    private readonly IConteoNavService _conteo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ILogger<ReservaNavService> _logger;

    public ReservaNavService(
        VentaNavService ventas,
        IReservaCoberturaNavService cobertura,
        IVentaNavRepository repo,
        IReservaBloqueoNavRepository bloqueoRepo,
        IProductoNavRepository productoRepo,
        IStockAlmacenNavRepository stockRepo,
        IAlmacenRepository almacenRepo,
        IStockNavService stock,
        IConteoNavService conteo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        IAlmacenAccesoNavService acceso,
        ILogger<ReservaNavService> logger)
    {
        _ventas = ventas;
        _cobertura = cobertura;
        _repo = repo;
        _bloqueoRepo = bloqueoRepo;
        _productoRepo = productoRepo;
        _stockRepo = stockRepo;
        _almacenRepo = almacenRepo;
        _stock = stock;
        _conteo = conteo;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _acceso = acceso;
        _logger = logger;
    }

    // ================= Consultas =================

    public async Task<PagedResultDto<VentaNavDto>> ObtenerTodosAsync(VentaNavFiltroDto query)
    {
        query.Tipo = (int)TipoVentaNav.Reserva;
        return await _ventas.ObtenerTodosAsync(query);
    }

    public async Task<VentaNavDto?> ObtenerPorIdAsync(int id)
    {
        var venta = await _ventas.ObtenerPorIdAsync(id);
        return venta != null && venta.Tipo == (int)TipoVentaNav.Reserva ? venta : null;
    }

    public async Task<List<ReservaProductoNavDto>> ObtenerProductosAsync(int? reservaId)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();

            var productos = await _productoRepo.AsQueryable()
                .AsNoTracking()
                .Where(p => p.IsActive && p.TemporadaId == temporada.Id)
                .ToListAsync();

            var almacenes = await _almacenRepo.AsQueryable()
                .AsNoTracking()
                .Where(a => a.IsActive && a.SistemaId == SistemaNavidadId)
                .OrderBy(a => a.Nombre)
                .ThenBy(a => a.Id)
                .Select(a => new { a.Id, a.Nombre })
                .ToListAsync();
            var almacenIds = almacenes.Select(a => a.Id).ToHashSet();

            var stocks = (await _stockRepo.AsQueryable()
                    .AsNoTracking()
                    .Where(s => s.TemporadaId == temporada.Id)
                    .Select(s => new { s.ProductoId, s.AlmacenId, s.Cantidad })
                    .ToListAsync())
                .Where(s => almacenIds.Contains(s.AlmacenId))
                .ToDictionary(s => (s.AlmacenId, s.ProductoId), s => s.Cantidad);
            var bloqueados = await _stock.ObtenerBloqueadosAsync(temporada.Id);
            var porLlegar = await _cobertura.ObtenerPorLlegarAsync(temporada.Id, null, reservaId);

            return productos
                .Select(p =>
                {
                    var filas = almacenes
                        .Select(a =>
                        {
                            var stock = stocks.GetValueOrDefault((a.Id, p.Id));
                            var bloqueado = bloqueados.GetValueOrDefault((a.Id, p.Id));
                            return new ReservaProductoAlmacenNavDto
                            {
                                AlmacenId = a.Id,
                                Nombre = a.Nombre,
                                Stock = stock,
                                Bloqueado = bloqueado,
                                Libre = Math.Max(0, stock - bloqueado)
                            };
                        })
                        .ToList();
                    var (llegar, libre) = porLlegar.TryGetValue(p.Id, out var v) ? v : (0, 0);
                    return new ReservaProductoNavDto
                    {
                        ProductoId = p.Id,
                        ProductoNombreMostrar = NombreMostrar(p),
                        UnidadesPorEmpaque = p.UnidadesPorEmpaque,
                        NombreEmpaque = p.NombreEmpaque,
                        Color = p.Color,
                        PrecioCatalogo = p.PrecioCatalogo,
                        PrecioCompraUnidad = p.PrecioCompraUnidad,
                        StockTotal = filas.Sum(f => f.Stock),
                        PorLlegar = llegar,
                        PorLlegarLibre = libre,
                        Almacenes = filas
                    };
                })
                .OrderBy(x => x.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener productos para reserva");
            throw;
        }
    }

    // ================= Crear =================

    public async Task<VentaNavDto> CrearAsync(CrearReservaNavDto dto)
    {
        try
        {
            ValidarEstructuraCrear(dto);

            var condicion = (CondicionPagoVentaNav)dto.CondicionPago;
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();

            var (esAdmin, vendedorActual) = await _ventas.ObtenerPermisosAsync(temporada.Id);
            var vendedorId = await _ventas.ResolverVendedorAsync(temporada.Id, vendedorActual, dto.VendedorId, esAdmin);

            var lineas = dto.Detalles.Cast<VentaLineaDto>().ToList();
            var productos = await _ventas.CargarProductosAsync(temporada.Id, lineas.Select(d => d.ProductoId));
            foreach (var l in lineas)
                if (!productos[l.ProductoId].IsActive)
                    throw new ValidacionException($"El producto {NombreMostrar(productos[l.ProductoId])} está inactivo");

            var (total, descuento, neto) = CalcularTotales(lineas, dto.Descuento);
            ValidarPrecios(lineas, productos, neto, esAdmin);

            var cobroMonto = ResolverCobroInicial(neto, condicion, dto.Cobro);
            var saldo = neto - cobroMonto;

            await _ventas.ValidarClienteAsync(dto.ClienteId!.Value);

            // Caja de tienda: la tienda de la sesión (opcional) tiene que ser una tienda navideña activa
            if (dto.AlmacenId.HasValue)
            {
                await _ventas.ValidarAlmacenAsync(dto.AlmacenId.Value, exigirTienda: true);
                await _acceso.VerificarAlmacenAsync(temporada.Id, dto.AlmacenId.Value);
            }

            // Bloqueos: almacenes navideños activos y stock libre (stock − bloqueos de otras reservas) en cada uno
            var bloqueos = dto.Detalles.ToDictionary(l => l.ProductoId, l => (l.Bloqueos ?? new List<ReservaBloqueoItemDto>()));
            foreach (var almacenId in bloqueos.Values.SelectMany(b => b).Select(b => b.AlmacenId).Distinct())
                await _ventas.ValidarAlmacenAsync(almacenId, exigirTienda: false);
            foreach (var l in dto.Detalles)
                foreach (var b in bloqueos[l.ProductoId])
                    await _stock.VerificarLibreAsync(b.AlmacenId, l.ProductoId, b.Unidades);

            // La parte sin bloqueo tiene que estar cubierta por lo que falta llegar de los pedidos
            await _cobertura.ValidarCoberturaAsync(temporada.Id,
                dto.Detalles
                    .Select(l => (l.ProductoId, NombreMostrar(productos[l.ProductoId]), l.Unidades - bloqueos[l.ProductoId].Sum(b => b.Unidades)))
                    .ToList());

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;
            var hoy = BoliviaTimeZone.Now().Date;

            var venta = new VentaNav
            {
                TemporadaId = temporada.Id,
                Fecha = hoy,
                AlmacenId = null,
                Canal = CanalVentaNav.Tienda,
                UsuarioId = usuarioId,
                VendedorId = vendedorId,
                ClienteId = dto.ClienteId,
                Tipo = TipoVentaNav.Reserva,
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

            foreach (var l in lineas)
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
            RecalcularEntrega(venta);

            await _uow.BeginTransactionAsync();
            try
            {
                venta.NroVenta = await _repo.ObtenerSiguienteNroAsync(temporada.Id);
                var creada = await _repo.CrearAsync(venta);

                foreach (var d in creada.Detalles)
                    foreach (var b in bloqueos[d.ProductoId])
                        await _bloqueoRepo.CrearAsync(new ReservaBloqueo
                        {
                            VentaId = creada.Id,
                            VentaDetalleId = d.Id,
                            ProductoId = d.ProductoId,
                            AlmacenId = b.AlmacenId,
                            Unidades = b.Unidades
                        });

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Reserva creada: {Id} #{Nro} (temporada {TemporadaId})", venta.Id, venta.NroVenta, temporada.Id);

            return await ObtenerPorIdAsync(venta.Id)
                ?? throw new EntidadNoEncontradaException("Reserva", venta.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear reserva");
            throw;
        }
    }

    // ================= Editar =================

    private sealed class PlanLinea
    {
        public VentaNavDetalle? Existente { get; init; }
        public ReservaLineaDto? Nueva { get; init; }
        public int ProductoId { get; init; }
        public int NuevaCantidad { get; init; }
        public int Devolver { get; init; }
        public int PendienteNuevo { get; init; }
        // Bloqueos actuales recortados al nuevo pendiente (almacén → unidades)
        public Dictionary<int, int> Recortados { get; init; } = new();
        // Estado final deseado de los bloqueos (almacén → unidades)
        public Dictionary<int, int> Deseados { get; init; } = new();
    }

    public async Task<VentaNavDto> EditarAsync(int id, EditarReservaNavDto dto)
    {
        try
        {
            ValidarEstructuraEditar(dto);

            var venta = await _ventas.ObtenerVentaParaEdicionAsync(id);
            if (venta.Tipo != TipoVentaNav.Reserva)
                throw new ValidacionException("La venta no es una reserva");
            if (venta.Anulada)
                throw new ValidacionException("La reserva está anulada");

            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);
            var (esAdmin, vendedorActual) = await _ventas.ObtenerPermisosAsync(venta.TemporadaId);
            VerificarPropietario(venta, esAdmin, vendedorActual);

            var lineas = dto.Detalles.Cast<VentaLineaDto>().ToList();
            var productos = await _ventas.CargarProductosAsync(venta.TemporadaId, lineas.Select(d => d.ProductoId));
            var existentes = venta.Detalles.ToDictionary(d => d.ProductoId);

            // Un producto inactivo solo se tolera si la línea ya existía y no sube de cantidad
            foreach (var l in lineas)
            {
                var p = productos[l.ProductoId];
                if (p.IsActive) continue;
                if (!existentes.TryGetValue(l.ProductoId, out var previa) || l.Unidades > previa.Unidades)
                    throw new ValidacionException($"El producto {NombreMostrar(p)} está inactivo");
            }

            var (total, descuento, neto) = CalcularTotales(lineas, dto.Descuento);
            ValidarPrecios(lineas, productos, neto, esAdmin);

            if (neto < venta.MontoCobrado)
                throw new ExcepcionDominio(
                    $"El nuevo neto ({neto}) es menor a lo ya cobrado ({venta.MontoCobrado}): primero anule un cobro",
                    409, "NETO_MENOR_COBRADO");

            await _ventas.ValidarClienteAsync(dto.ClienteId!.Value);

            var nuevas = dto.Detalles.ToDictionary(l => l.ProductoId);
            var bloqueosVigentes = venta.Bloqueos.Where(b => b.IsActive).ToList();

            // Plan por línea (las existentes que faltan en el body quedan en 0)
            var plan = new List<PlanLinea>();
            foreach (var d in venta.Detalles)
            {
                nuevas.TryGetValue(d.ProductoId, out var l);
                plan.Add(ArmarPlan(d, l, bloqueosVigentes.Where(b => b.VentaDetalleId == d.Id).ToList(), esAdmin, productos));
            }
            foreach (var l in nuevas.Values.Where(l => !existentes.ContainsKey(l.ProductoId)))
                plan.Add(ArmarPlan(null, l, new List<ReservaBloqueo>(), esAdmin, productos));

            // Almacenes nuevos: navideños activos; subir un bloqueo exige stock libre (sin los bloqueos de otras reservas)
            foreach (var almacenId in plan.SelectMany(p => p.Deseados.Keys).Distinct())
                await _ventas.ValidarAlmacenAsync(almacenId, exigirTienda: false);
            foreach (var p in plan)
                foreach (var (almacenId, unidades) in p.Deseados)
                    if (unidades > p.Recortados.GetValueOrDefault(almacenId))
                        await _stock.VerificarLibreAsync(almacenId, p.ProductoId, unidades, venta.Id);

            // Cobertura: solo las líneas cuya parte sin bloqueo crece (o son nuevas) tienen que estar cubiertas por lo que falta llegar
            var aValidar = new List<(int ProductoId, string ProductoNombre, int SinBloqueo)>();
            foreach (var p in plan.Where(p => p.NuevaCantidad > 0))
            {
                var sinBloqueoNuevo = p.PendienteNuevo - p.Deseados.Values.Sum();
                var sinBloqueoActual = p.Existente == null ? 0
                    : Math.Max(0, p.Existente.Unidades - p.Existente.UnidadesEntregadas
                        - bloqueosVigentes.Where(b => b.VentaDetalleId == p.Existente.Id).Sum(b => b.Unidades));
                if (sinBloqueoNuevo > sinBloqueoActual)
                    aValidar.Add((p.ProductoId, NombreMostrar(productos[p.ProductoId]), sinBloqueoNuevo));
            }
            await _cobertura.ValidarCoberturaAsync(venta.TemporadaId, aValidar, venta.Id);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                // 1) Bajar unidades ya entregadas: el stock vuelve al almacén de la última entrega de la línea (DevolucionVenta)
                foreach (var p in plan.Where(p => p.Devolver > 0))
                {
                    var linea = p.Existente!;
                    var almacenDevolucion = venta.Entregas
                        .Where(e => !e.Anulada && e.Detalles.Any(x => x.VentaDetalleId == linea.Id && x.Unidades > 0))
                        .OrderByDescending(e => e.Id)
                        .Select(e => (int?)e.AlmacenId)
                        .FirstOrDefault()
                        ?? throw new ValidacionException("No se encontró la entrega de la que devolver el stock");
                    await _stock.RegistrarEntradaAsync(venta.TemporadaId, almacenDevolucion, linea.ProductoId, p.Devolver,
                        TipoMovimientoNav.DevolucionVenta, ReferenciaVenta, venta.Id);
                    ReducirEntregas(venta, linea, p.Devolver);
                    linea.UnidadesEntregadas -= p.Devolver;
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

                // 3) Bloqueos: recorte automático al nuevo pendiente y después el estado deseado por almacén
                foreach (var p in plan)
                {
                    var detalleId = p.Existente?.Id ?? venta.Detalles.First(d => d.ProductoId == p.ProductoId).Id;
                    var propios = bloqueosVigentes.Where(b => b.VentaDetalleId == detalleId).ToList();

                    var exceso = propios.Where(b => b.IsActive).Sum(b => b.Unidades) - p.PendienteNuevo;
                    if (exceso > 0)
                        ReservaBloqueoHelper.Liberar(propios, exceso, usuarioId, ahora);

                    var almacenes = p.Deseados.Keys.Concat(propios.Where(b => b.IsActive).Select(b => b.AlmacenId)).Distinct().ToList();
                    foreach (var almacenId in almacenes)
                    {
                        var actual = propios.Where(b => b.IsActive && b.AlmacenId == almacenId).Sum(b => b.Unidades);
                        var deseado = p.Deseados.GetValueOrDefault(almacenId);
                        if (deseado < actual)
                            ReservaBloqueoHelper.Liberar(propios.Where(b => b.AlmacenId == almacenId), actual - deseado, usuarioId, ahora);
                        else if (deseado > actual)
                            await _bloqueoRepo.CrearAsync(new ReservaBloqueo
                            {
                                VentaId = venta.Id,
                                VentaDetalleId = detalleId,
                                ProductoId = p.ProductoId,
                                AlmacenId = almacenId,
                                Unidades = deseado - actual
                            });
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

            _logger.LogInformation("Reserva editada: {Id} #{Nro}", venta.Id, venta.NroVenta);

            return await ObtenerPorIdAsync(venta.Id)
                ?? throw new EntidadNoEncontradaException("Reserva", venta.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar reserva {Id}", id);
            throw;
        }
    }

    // Calcula devolución, pendiente nuevo, bloqueos recortados y bloqueos deseados de una línea; valida las reglas de bloqueo
    private static PlanLinea ArmarPlan(VentaNavDetalle? existente, ReservaLineaDto? nueva, List<ReservaBloqueo> bloqueosLinea,
        bool esAdmin, Dictionary<int, ProductoNav> productos)
    {
        var productoId = existente?.ProductoId ?? nueva!.ProductoId;
        var nuevaCantidad = nueva?.Unidades ?? 0;
        var entregadas = existente?.UnidadesEntregadas ?? 0;
        var pendienteNuevo = nuevaCantidad - Math.Min(entregadas, nuevaCantidad);
        var nombre = productos.TryGetValue(productoId, out var p) ? NombreMostrar(p) : $"#{productoId}";

        // Recorte automático: si baja el pendiente se sueltan los bloqueos sobrantes, empezando por el más antiguo
        var recortados = RecortarPorAlmacen(bloqueosLinea, pendienteNuevo);

        Dictionary<int, int> deseados;
        if (nueva?.Bloqueos == null)
            deseados = new Dictionary<int, int>(recortados);
        else
        {
            deseados = nueva.Bloqueos.ToDictionary(b => b.AlmacenId, b => b.Unidades);
            if (deseados.Values.Sum() > pendienteNuevo)
                throw new ValidacionException($"Los bloqueos de {nombre} ({deseados.Values.Sum()}) superan lo pendiente de entrega ({pendienteNuevo})");
        }

        // Solo el admin baja un bloqueo existente (el recorte automático al bajar el pendiente no cuenta)
        if (!esAdmin)
            foreach (var (almacenId, unidades) in recortados)
                if (deseados.GetValueOrDefault(almacenId) < unidades)
                    throw new ExcepcionDominio($"Solo el admin puede bajar un bloqueo de stock existente ({nombre})", 403, "SIN_PERMISO");

        return new PlanLinea
        {
            Existente = existente,
            Nueva = nueva,
            ProductoId = productoId,
            NuevaCantidad = nuevaCantidad,
            Devolver = Math.Max(0, entregadas - nuevaCantidad),
            PendienteNuevo = pendienteNuevo,
            Recortados = recortados,
            Deseados = deseados
        };
    }

    private static Dictionary<int, int> RecortarPorAlmacen(IEnumerable<ReservaBloqueo> bloqueos, int pendienteNuevo)
    {
        var lista = bloqueos.Where(b => b.IsActive).OrderBy(b => b.CreatedAt).ThenBy(b => b.Id).ToList();
        var exceso = lista.Sum(b => b.Unidades) - pendienteNuevo;
        var resultado = new Dictionary<int, int>();
        foreach (var b in lista)
        {
            var quitar = Math.Min(Math.Max(exceso, 0), b.Unidades);
            exceso -= quitar;
            var queda = b.Unidades - quitar;
            if (queda > 0)
                resultado[b.AlmacenId] = resultado.GetValueOrDefault(b.AlmacenId) + queda;
        }
        return resultado;
    }

    // ================= Entregas =================

    public async Task<VentaNavDto> RegistrarEntregaAsync(int ventaId, RegistrarEntregaVentaDto dto)
    {
        try
        {
            if (dto.AlmacenId <= 0)
                throw new ValidacionException("El almacén es obligatorio");
            if (dto.Detalles == null || dto.Detalles.Count == 0)
                throw new ValidacionException("Debe indicar al menos una línea a entregar");
            if (dto.Detalles.Any(d => d.Unidades <= 0))
                throw new ValidacionException("Las unidades a entregar deben ser mayores a 0");
            if (dto.Detalles.Select(d => d.VentaDetalleId).Distinct().Count() != dto.Detalles.Count)
                throw new ValidacionException("Hay líneas repetidas en la entrega");

            var venta = await _ventas.ObtenerVentaParaEdicionAsync(ventaId);
            if (venta.Tipo != TipoVentaNav.Reserva)
                throw new ValidacionException("La venta no es una reserva");
            if (venta.Anulada)
                throw new ValidacionException("La reserva está anulada");

            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);
            await _ventas.ValidarAlmacenAsync(dto.AlmacenId, exigirTienda: false);

            var lineas = new List<(VentaNavDetalle Linea, int Unidades)>();
            foreach (var item in dto.Detalles)
            {
                var linea = venta.Detalles.FirstOrDefault(d => d.Id == item.VentaDetalleId)
                    ?? throw new ValidacionException($"La línea {item.VentaDetalleId} no pertenece a la reserva");
                var pendiente = linea.Unidades - linea.UnidadesEntregadas;
                if (item.Unidades > pendiente)
                    throw new ValidacionException($"La línea {item.VentaDetalleId} tiene {pendiente} unidades pendientes y se intentó entregar {item.Unidades}");
                lineas.Add((linea, item.Unidades));
            }

            // Sale stock del almacén: exige apertura de hoy si es tienda con conteo
            await _conteo.VerificarAperturaParaVentaAsync(dto.AlmacenId);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;
            var hoy = BoliviaTimeZone.Now().Date;

            await _uow.BeginTransactionAsync();
            try
            {
                var entrega = NuevaEntrega(dto.AlmacenId, hoy, usuarioId, ahora);
                foreach (var (linea, unidades) in lineas)
                {
                    // La propia reserva no se bloquea a sí misma (ventaIdPropia): consume su bloqueo del almacén y, si falta, usa stock libre
                    await _stock.RegistrarSalidaAsync(venta.TemporadaId, dto.AlmacenId, linea.ProductoId, unidades,
                        TipoMovimientoNav.EntregaReserva, ReferenciaVenta, venta.Id, ventaIdPropia: venta.Id);
                    entrega.Detalles.Add(new EntregaVentaDetalle
                    {
                        VentaDetalleId = linea.Id,
                        ProductoId = linea.ProductoId,
                        Unidades = unidades,
                        CreatedByUsuarioId = usuarioId,
                        CreatedAt = ahora
                    });
                    linea.UnidadesEntregadas += unidades;
                    Tocar(linea, usuarioId, ahora);

                    // Primero consume su propio bloqueo de ese almacén; lo que sale de stock libre libera la misma cantidad
                    // de los bloqueos de la línea en los otros almacenes (el más antiguo primero)
                    var propios = venta.Bloqueos.Where(b => b.IsActive && b.VentaDetalleId == linea.Id).ToList();
                    var consumido = ReservaBloqueoHelper.Liberar(propios.Where(b => b.AlmacenId == dto.AlmacenId), unidades, usuarioId, ahora);
                    if (unidades > consumido)
                        ReservaBloqueoHelper.Liberar(propios.Where(b => b.AlmacenId != dto.AlmacenId), unidades - consumido, usuarioId, ahora);

                    // Invariante: Σ bloqueos <= pendiente (red de seguridad)
                    var exceso = propios.Where(b => b.IsActive).Sum(b => b.Unidades) - (linea.Unidades - linea.UnidadesEntregadas);
                    if (exceso > 0)
                        ReservaBloqueoHelper.Liberar(propios, exceso, usuarioId, ahora);
                }
                venta.Entregas.Add(entrega);

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

            _logger.LogInformation("Entrega registrada en reserva {Id}", ventaId);
            return await ObtenerPorIdAsync(ventaId) ?? throw new EntidadNoEncontradaException("Reserva", ventaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar entrega de la reserva {Id}", ventaId);
            throw;
        }
    }

    // ================= Desbloquear =================

    public async Task<VentaNavDto> LiberarBloqueoAsync(int ventaId, LiberarBloqueoReservaDto dto)
    {
        try
        {
            var venta = await _ventas.ObtenerVentaParaEdicionAsync(ventaId);
            if (venta.Tipo != TipoVentaNav.Reserva)
                throw new ValidacionException("La venta no es una reserva");
            if (venta.Anulada)
                throw new ValidacionException("La reserva está anulada");

            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);
            var (esAdmin, _) = await _ventas.ObtenerPermisosAsync(venta.TemporadaId);
            // Desbloquear: solo el admin (el vendedor llama al admin)
            if (!esAdmin)
                throw new ExcepcionDominio("Solo el admin puede desbloquear el stock de una reserva", 403, "SIN_PERMISO");

            var bloqueo = venta.Bloqueos.FirstOrDefault(b => b.IsActive && b.Id == dto.BloqueoId)
                ?? throw new ValidacionException($"El bloqueo {dto.BloqueoId} no existe o no pertenece a la reserva");
            var unidades = dto.Unidades ?? bloqueo.Unidades;
            if (unidades <= 0 || unidades > bloqueo.Unidades)
                throw new ValidacionException($"Las unidades a liberar deben estar entre 1 y {bloqueo.Unidades}");

            ReservaBloqueoHelper.Liberar(new[] { bloqueo }, unidades, _currentUser.GetUsuarioId() ?? 1, DateTime.UtcNow);
            await _uow.SaveChangesAsync();

            _logger.LogInformation("Reserva {Id}: bloqueo {BloqueoId} liberado ({Unidades} u)", ventaId, dto.BloqueoId, unidades);
            return await ObtenerPorIdAsync(ventaId) ?? throw new EntidadNoEncontradaException("Reserva", ventaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al liberar bloqueo de la reserva {Id}", ventaId);
            throw;
        }
    }

    // ================= Reglas =================

    private static void ValidarEstructuraCrear(CrearReservaNavDto dto)
    {
        if (!Enum.IsDefined(typeof(CondicionPagoVentaNav), dto.CondicionPago))
            throw new ValidacionException("La condición de pago es inválida");
        if (!dto.ClienteId.HasValue || dto.ClienteId.Value <= 0)
            throw new ValidacionException("El cliente es obligatorio en una reserva");
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        if (dto.Cobro != null)
        {
            if (dto.Cobro.Comprobante != null && dto.Cobro.Comprobante.Trim().Length > 100)
                throw new ValidacionException("El comprobante no puede superar 100 caracteres");
            if (dto.Cobro.Observacion != null && dto.Cobro.Observacion.Trim().Length > 500)
                throw new ValidacionException("La observación del cobro no puede superar 500 caracteres");
        }
        ValidarLineas(dto.Detalles?.Cast<VentaLineaDto>().ToList()!, dto.Descuento);
        ValidarBloqueosEstructura(dto.Detalles!);
    }

    private static void ValidarEstructuraEditar(EditarReservaNavDto dto)
    {
        if (!dto.ClienteId.HasValue || dto.ClienteId.Value <= 0)
            throw new ValidacionException("El cliente es obligatorio en una reserva");
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        ValidarLineas(dto.Detalles?.Cast<VentaLineaDto>().ToList()!, dto.Descuento);
        ValidarBloqueosEstructura(dto.Detalles!);
    }

    // Bloqueos de cada línea: almacén y unidades > 0, sin almacenes repetidos
    private static void ValidarBloqueosEstructura(List<ReservaLineaDto> detalles)
    {
        foreach (var l in detalles)
        {
            if (l.Bloqueos == null) continue;
            if (l.Bloqueos.Any(b => b.AlmacenId <= 0))
                throw new ValidacionException("El almacén del bloqueo es obligatorio");
            if (l.Bloqueos.Any(b => b.Unidades <= 0))
                throw new ValidacionException("Las unidades de un bloqueo deben ser mayores a 0");
            if (l.Bloqueos.Select(b => b.AlmacenId).Distinct().Count() != l.Bloqueos.Count)
                throw new ValidacionException("Hay almacenes repetidos en los bloqueos de una línea");
            if (l.Bloqueos.Sum(b => b.Unidades) > l.Unidades)
                throw new ValidacionException("Los bloqueos de una línea no pueden superar sus unidades");
        }
    }
}
