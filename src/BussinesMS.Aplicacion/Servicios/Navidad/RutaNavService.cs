using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
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

// Ruta (módulo 7): salidas del vendedor de ruta con cargas y devoluciones (mueven stock), cierre (crea la venta de ruta a precio de catálogo,
// sin mover stock), abonos del vendedor (se reparten de la venta más antigua a la más nueva) y edición de ventas de ruta (solo admin).
// Reutiliza los helpers internal de VentaNavService (totales, R4, estados de pago/entrega, productos, permisos, nombres).
// DTOs armados a mano; stock siempre vía IStockNavService; transacciones con INavidadUnitOfWork.
public class RutaNavService : IRutaNavService
{
    private const string ReferenciaSalida = "SalidaRuta";

    private readonly VentaNavService _ventas;
    private readonly ISalidaRutaNavRepository _repo;
    private readonly ISalidaRutaCargaNavRepository _cargaRepo;
    private readonly IDevolucionRutaNavRepository _devolucionRepo;
    private readonly IAbonoVendedorNavRepository _abonoRepo;
    private readonly IVentaNavRepository _ventaRepo;
    private readonly IVendedorNavRepository _vendedorRepo;
    private readonly IStockNavService _stock;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly IPrecioCompraAccesoService _precioAcceso;
    private readonly ILogger<RutaNavService> _logger;

    public RutaNavService(
        VentaNavService ventas,
        ISalidaRutaNavRepository repo,
        ISalidaRutaCargaNavRepository cargaRepo,
        IDevolucionRutaNavRepository devolucionRepo,
        IAbonoVendedorNavRepository abonoRepo,
        IVentaNavRepository ventaRepo,
        IVendedorNavRepository vendedorRepo,
        IStockNavService stock,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        IAlmacenAccesoNavService acceso,
        IPrecioCompraAccesoService precioAcceso,
        ILogger<RutaNavService> logger)
    {
        _ventas = ventas;
        _repo = repo;
        _cargaRepo = cargaRepo;
        _devolucionRepo = devolucionRepo;
        _abonoRepo = abonoRepo;
        _ventaRepo = ventaRepo;
        _vendedorRepo = vendedorRepo;
        _stock = stock;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _acceso = acceso;
        _precioAcceso = precioAcceso;
        _logger = logger;
    }

    // ================= Permisos =================

    private sealed record Actor(bool EsAdmin, bool EsEncargado, Vendedor? VendedorRuta)
    {
        // Ver y operar todo: solo el admin (encargado y vendedor de ruta ven y operan solo lo suyo)
        public bool PuedeGestionar => EsAdmin;
    }

    private async Task<Actor> ObtenerActorAsync(int temporadaId)
    {
        var esAdmin = await _acceso.EsAdminAsync();
        var esEncargado = await _acceso.EsEncargadoAsync();
        var (_, vendedor) = await _ventas.ObtenerPermisosAsync(temporadaId);
        var vendedorRuta = vendedor != null && vendedor.Tipo == TipoVendedor.Ruta ? vendedor : null;
        return new Actor(esAdmin, esEncargado, vendedorRuta);
    }

    private static ExcepcionDominio SinPermiso(string mensaje = "No tiene permiso para realizar esta acción")
        => new(mensaje, 403, "SIN_PERMISO");

    private async Task ExigirAdminAsync()
    {
        if (!await _acceso.EsAdminAsync())
            throw SinPermiso("Solo el admin puede realizar esta acción");
    }

    // Lecturas: admin ve todo (null); el vendedor de ruta solo lo suyo (su id); el resto, 403
    private async Task<int?> RestriccionLecturaAsync(int temporadaId)
    {
        var actor = await ObtenerActorAsync(temporadaId);
        if (actor.PuedeGestionar) return null;
        if (actor.VendedorRuta != null) return actor.VendedorRuta.Id;
        throw SinPermiso();
    }

    // ================= Contexto =================

    public async Task<RutaContextoDto> ObtenerContextoAsync()
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var actor = await ObtenerActorAsync(temporada.Id);

            var query = _vendedorRepo.AsQueryable()
                .AsNoTracking()
                .Where(v => v.TemporadaId == temporada.Id && v.IsActive && v.Tipo == TipoVendedor.Ruta);
            if (!actor.PuedeGestionar)
            {
                var propio = actor.VendedorRuta?.Id ?? 0;
                query = query.Where(v => v.Id == propio);
            }
            var vendedores = await query.ToListAsync();
            var nombres = await _ventas.ObtenerNombresUsuarioAsync(vendedores.Select(v => v.UsuarioId));

            return new RutaContextoDto
            {
                EsAdmin = actor.EsAdmin,
                EsEncargado = actor.EsEncargado,
                VendedorId = actor.VendedorRuta?.Id,
                PuedeVerTodo = actor.EsAdmin,
                PuedeAbonar = actor.EsAdmin,
                PuedeCerrar = actor.EsAdmin || actor.VendedorRuta != null,
                VendedorActual = actor.VendedorRuta == null ? null : new RutaVendedorItemDto
                {
                    Id = actor.VendedorRuta.Id,
                    Nombre = nombres.GetValueOrDefault(actor.VendedorRuta.UsuarioId, string.Empty)
                },
                VendedoresRuta = vendedores
                    .Select(v => new RutaVendedorItemDto { Id = v.Id, Nombre = nombres.GetValueOrDefault(v.UsuarioId, string.Empty) })
                    .OrderBy(v => v.Nombre, StringComparer.CurrentCultureIgnoreCase)
                    .ToList(),
                AlmacenesACargo = actor.EsEncargado ? await _acceso.ObtenerAlmacenesACargoAsync(temporada.Id) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener contexto de ruta");
            throw;
        }
    }

    // ================= Consultas: salidas =================

    public async Task<PagedResultDto<SalidaRutaListaDto>> ObtenerSalidasAsync(SalidaRutaFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);
            var forzado = await RestriccionLecturaAsync(temporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(s => s.Vendedor)
                .Where(s => s.IsActive && s.TemporadaId == temporadaId);

            if (forzado.HasValue)
                baseQuery = baseQuery.Where(s => s.VendedorId == forzado.Value);
            else if (query.VendedorId.HasValue)
                baseQuery = baseQuery.Where(s => s.VendedorId == query.VendedorId.Value);
            if (query.Estado.HasValue)
            {
                var estado = (EstadoSalidaRuta)query.Estado.Value;
                baseQuery = baseQuery.Where(s => s.Estado == estado);
            }
            if (query.Desde.HasValue)
            {
                var desde = query.Desde.Value.Date;
                baseQuery = baseQuery.Where(s => s.FechaApertura >= desde);
            }
            if (query.Hasta.HasValue)
            {
                var hastaExclusivo = query.Hasta.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(s => s.FechaApertura < hastaExclusivo);
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(s => s.FechaApertura).ThenByDescending(s => s.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var unidades = await ObtenerUnidadesAsync(entidades.Select(e => e.Id).ToList());
            var ventaIds = entidades.Where(e => e.VentaId.HasValue).Select(e => e.VentaId!.Value).ToList();
            var ventas = ventaIds.Count == 0
                ? new Dictionary<int, (int NroVenta, decimal MontoNeto, bool Anulada, decimal MontoCobrado, decimal MontoSaldo, EstadoPagoVentaNav EstadoPago)>()
                : (await _ventaRepo.AsQueryable().AsNoTracking()
                        .Where(v => ventaIds.Contains(v.Id))
                        .Select(v => new { v.Id, v.NroVenta, v.MontoNeto, v.Anulada, v.MontoCobrado, v.MontoSaldo, v.EstadoPago })
                        .ToListAsync())
                    .ToDictionary(v => v.Id, v => (v.NroVenta, v.MontoNeto, v.Anulada, v.MontoCobrado, v.MontoSaldo, v.EstadoPago));
            var nombres = await _ventas.ObtenerNombresUsuarioAsync(entidades.Where(e => e.Vendedor != null).Select(e => e.Vendedor!.UsuarioId));

            var items = entidades.Select(e =>
            {
                var (cargado, devuelto) = unidades.GetValueOrDefault(e.Id);
                var neto = cargado - devuelto;
                var tieneVenta = e.VentaId.HasValue && ventas.TryGetValue(e.VentaId.Value, out var vv) && !vv.Anulada;
                return new SalidaRutaListaDto
                {
                    Id = e.Id,
                    TemporadaId = e.TemporadaId,
                    VendedorId = e.VendedorId,
                    VendedorNombre = e.Vendedor != null ? nombres.GetValueOrDefault(e.Vendedor.UsuarioId, string.Empty) : string.Empty,
                    FechaApertura = e.FechaApertura,
                    FechaCierre = e.FechaCierre,
                    Estado = (int)e.Estado,
                    EstadoNombre = NombreEstado(e.Estado),
                    UnidadesEnPoder = e.Estado == EstadoSalidaRuta.Abierta ? neto : 0,
                    UnidadesVendidas = neto,
                    MontoVenta = tieneVenta ? ventas[e.VentaId!.Value].MontoNeto : null,
                    VentaId = e.VentaId,
                    NroVenta = tieneVenta ? ventas[e.VentaId!.Value].NroVenta : null,
                    MontoCobrado = tieneVenta ? ventas[e.VentaId!.Value].MontoCobrado : null,
                    MontoSaldo = tieneVenta ? ventas[e.VentaId!.Value].MontoSaldo : null,
                    EstadoRendicion = tieneVenta && e.Estado != EstadoSalidaRuta.Abierta
                        ? ventas[e.VentaId!.Value].EstadoPago switch
                        {
                            EstadoPagoVentaNav.Pagado => "Rendida",
                            EstadoPagoVentaNav.Parcial => "Parcial",
                            _ => "Pendiente"
                        }
                        : null
                };
            }).ToList();

            return new PagedResultDto<SalidaRutaListaDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener salidas de ruta");
            throw;
        }
    }

    public async Task<SalidaRutaDto?> ObtenerSalidaAsync(int id)
    {
        try
        {
            var salida = await _repo.ObtenerConDetallesAsync(id);
            if (salida == null || !salida.IsActive) return null;

            var forzado = await RestriccionLecturaAsync(salida.TemporadaId);
            if (forzado.HasValue && forzado.Value != salida.VendedorId)
                throw SinPermiso("Solo puede ver sus propias salidas");

            return await MapearSalidaAsync(salida);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener salida de ruta {Id}", id);
            throw;
        }
    }

    // Σ unidades de cargas y devoluciones vigentes por salida
    private async Task<Dictionary<int, (int Cargado, int Devuelto)>> ObtenerUnidadesAsync(IReadOnlyCollection<int> salidaIds)
    {
        var resultado = new Dictionary<int, (int Cargado, int Devuelto)>();
        if (salidaIds.Count == 0) return resultado;

        var cargas = await _cargaRepo.AsQueryable().AsNoTracking()
            .Where(c => salidaIds.Contains(c.SalidaRutaId) && !c.Anulada)
            .SelectMany(c => c.Detalles.Select(d => new { c.SalidaRutaId, d.Unidades }))
            .ToListAsync();
        var devoluciones = await _devolucionRepo.AsQueryable().AsNoTracking()
            .Where(d => salidaIds.Contains(d.SalidaRutaId) && !d.Anulada)
            .SelectMany(d => d.Detalles.Select(x => new { d.SalidaRutaId, x.Unidades }))
            .ToListAsync();

        foreach (var g in cargas.GroupBy(c => c.SalidaRutaId))
            resultado[g.Key] = (g.Sum(x => x.Unidades), 0);
        foreach (var g in devoluciones.GroupBy(d => d.SalidaRutaId))
        {
            var actual = resultado.GetValueOrDefault(g.Key);
            resultado[g.Key] = (actual.Cargado, g.Sum(x => x.Unidades));
        }
        return resultado;
    }

    // ================= Cargas =================

    public async Task<SalidaRutaDto> CargarAsync(CargarSalidaRutaDto dto)
    {
        try
        {
            ValidarEstructuraCarga(dto);

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var actor = await ObtenerActorAsync(temporada.Id);

            int vendedorId;
            if (actor.PuedeGestionar)
            {
                // Admin que a la vez es vendedor de ruta: si no manda vendedor, queda él
                var elegido = dto.VendedorId.HasValue && dto.VendedorId.Value > 0 ? dto.VendedorId : actor.VendedorRuta?.Id;
                if (!elegido.HasValue)
                    throw new ValidacionException("El vendedor es obligatorio");
                vendedorId = elegido.Value;
            }
            else if (actor.VendedorRuta != null)
                vendedorId = actor.VendedorRuta.Id;
            else
                throw SinPermiso();

            await ValidarVendedorRutaAsync(temporada.Id, vendedorId);

            foreach (var almacenId in dto.Detalles.Select(d => d.AlmacenOrigenId).Distinct())
            {
                await _ventas.ValidarAlmacenAsync(almacenId, exigirTienda: false);
            }

            var productos = await _ventas.CargarProductosAsync(temporada.Id, dto.Detalles.Select(d => d.ProductoId));
            foreach (var l in dto.Detalles)
                if (!productos[l.ProductoId].IsActive)
                    throw new ValidacionException($"El producto {NombreMostrar(productos[l.ProductoId])} está inactivo");

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;
            var hoy = BoliviaTimeZone.Now();

            var carga = new SalidaRutaCarga
            {
                Fecha = hoy,
                UsuarioId = usuarioId,
                Anulada = false,
                CreatedByUsuarioId = usuarioId,
                CreatedAt = ahora
            };
            foreach (var l in dto.Detalles)
                carga.Detalles.Add(new SalidaRutaCargaDetalle
                {
                    AlmacenOrigenId = l.AlmacenOrigenId,
                    ProductoId = l.ProductoId,
                    Unidades = l.Unidades,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });

            // Una sola salida abierta por vendedor: si existe, la carga se agrega a ella
            var abiertaId = await _repo.AsQueryable().AsNoTracking()
                .Where(s => s.IsActive && s.TemporadaId == temporada.Id && s.VendedorId == vendedorId && s.Estado == EstadoSalidaRuta.Abierta)
                .Select(s => (int?)s.Id)
                .FirstOrDefaultAsync();

            SalidaRuta salida;
            await _uow.BeginTransactionAsync();
            try
            {
                if (abiertaId.HasValue)
                {
                    salida = await ObtenerSalidaParaEdicionAsync(abiertaId.Value);
                    salida.Cargas.Add(carga);
                    if (dto.Observacion != null && NormalizarTexto(dto.Observacion) != null && salida.Observacion == null)
                        salida.Observacion = NormalizarTexto(dto.Observacion);
                    Tocar(salida, usuarioId, ahora);
                    await _uow.SaveChangesAsync();
                }
                else
                {
                    salida = new SalidaRuta
                    {
                        TemporadaId = temporada.Id,
                        VendedorId = vendedorId,
                        FechaApertura = hoy,
                        Estado = EstadoSalidaRuta.Abierta,
                        Observacion = NormalizarTexto(dto.Observacion)
                    };
                    salida.Cargas.Add(carga);
                    await _repo.CrearAsync(salida);
                }

                foreach (var l in dto.Detalles)
                    await _stock.RegistrarSalidaAsync(temporada.Id, l.AlmacenOrigenId, l.ProductoId, l.Unidades,
                        TipoMovimientoNav.SalidaRuta, ReferenciaSalida, salida.Id);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Carga de ruta registrada: salida {Id}, vendedor {VendedorId}", salida.Id, vendedorId);
            return await ObtenerSalidaAsync(salida.Id) ?? throw new EntidadNoEncontradaException("Salida de ruta", salida.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar carga de ruta");
            throw;
        }
    }

    private static void ValidarEstructuraCarga(CargarSalidaRutaDto dto)
    {
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            throw new ValidacionException("Debe registrar al menos un producto");
        if (dto.Detalles.Any(d => d.AlmacenOrigenId <= 0))
            throw new ValidacionException("El almacén de origen es obligatorio");
        if (dto.Detalles.Any(d => d.ProductoId <= 0))
            throw new ValidacionException("El producto es obligatorio");
        if (dto.Detalles.Any(d => d.Unidades <= 0))
            throw new ValidacionException("Las unidades deben ser mayores a 0");
        if (dto.Detalles.Select(d => (d.AlmacenOrigenId, d.ProductoId)).Distinct().Count() != dto.Detalles.Count)
            throw new ValidacionException("Hay productos repetidos para el mismo almacén");
    }

    private async Task ValidarVendedorRutaAsync(int temporadaId, int vendedorId)
    {
        var vendedor = await _vendedorRepo.AsQueryable().AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == vendedorId && v.TemporadaId == temporadaId && v.IsActive);
        if (vendedor == null)
            throw new ValidacionException("El vendedor no existe, está inactivo o no es de la temporada");
        if (vendedor.Tipo != TipoVendedor.Ruta)
            throw new ValidacionException("Solo los vendedores de tipo Ruta pueden salir a ruta");
    }

    // ================= Devoluciones =================

    public async Task<SalidaRutaDto> DevolverAsync(int salidaId, DevolverSalidaRutaDto dto)
    {
        try
        {
            if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
                throw new ValidacionException("La observación no puede superar 500 caracteres");
            if (dto.Detalles == null || dto.Detalles.Count == 0)
                throw new ValidacionException("Debe registrar al menos un producto");
            if (dto.Detalles.Any(d => d.AlmacenDestinoId <= 0))
                throw new ValidacionException("El almacén de destino es obligatorio");
            if (dto.Detalles.Any(d => d.ProductoId <= 0))
                throw new ValidacionException("El producto es obligatorio");
            if (dto.Detalles.Any(d => d.Unidades <= 0))
                throw new ValidacionException("Las unidades deben ser mayores a 0");
            if (dto.Detalles.Select(d => (d.AlmacenDestinoId, d.ProductoId)).Distinct().Count() != dto.Detalles.Count)
                throw new ValidacionException("Hay productos repetidos para el mismo almacén");

            var salida = await ObtenerSalidaParaEdicionAsync(salidaId);
            await _temporadaActual.VerificarEditableAsync(salida.TemporadaId);
            await VerificarPuedeOperarSalidaAsync(salida);
            ExigirAbierta(salida);

            foreach (var almacenId in dto.Detalles.Select(d => d.AlmacenDestinoId).Distinct())
            {
                await _ventas.ValidarAlmacenAsync(almacenId, exigirTienda: false);
            }

            var productos = await _ventas.CargarProductosAsync(salida.TemporadaId, dto.Detalles.Select(d => d.ProductoId));
            var (cargado, devuelto) = CalcularUnidadesPorProducto(salida);
            foreach (var g in dto.Detalles.GroupBy(d => d.ProductoId))
            {
                var enPoder = cargado.GetValueOrDefault(g.Key) - devuelto.GetValueOrDefault(g.Key);
                var pedido = g.Sum(d => d.Unidades);
                if (pedido > enPoder)
                    throw new ValidacionException(
                        $"No se pueden devolver {pedido} de {NombreMostrar(productos[g.Key])}: el vendedor solo tiene {Math.Max(0, enPoder)} en poder");
            }

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            var devolucion = new DevolucionRuta
            {
                Fecha = BoliviaTimeZone.Now(),
                UsuarioId = usuarioId,
                EsTardia = false,
                Anulada = false,
                Observacion = NormalizarTexto(dto.Observacion),
                CreatedByUsuarioId = usuarioId,
                CreatedAt = ahora
            };
            foreach (var l in dto.Detalles)
                devolucion.Detalles.Add(new DevolucionRutaDetalle
                {
                    AlmacenDestinoId = l.AlmacenDestinoId,
                    ProductoId = l.ProductoId,
                    Unidades = l.Unidades,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });

            await _uow.BeginTransactionAsync();
            try
            {
                salida.Devoluciones.Add(devolucion);
                Tocar(salida, usuarioId, ahora);
                await _uow.SaveChangesAsync();

                foreach (var l in dto.Detalles)
                    await _stock.RegistrarEntradaAsync(salida.TemporadaId, l.AlmacenDestinoId, l.ProductoId, l.Unidades,
                        TipoMovimientoNav.DevolucionRuta, ReferenciaSalida, salida.Id);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Devolución de ruta registrada: salida {Id}", salida.Id);
            return await ObtenerSalidaAsync(salida.Id) ?? throw new EntidadNoEncontradaException("Salida de ruta", salida.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar devolución de la salida {Id}", salidaId);
            throw;
        }
    }

    // Admin/encargado: cualquier salida; vendedor de ruta: solo la suya
    private async Task VerificarPuedeOperarSalidaAsync(SalidaRuta salida)
    {
        var actor = await ObtenerActorAsync(salida.TemporadaId);
        if (actor.PuedeGestionar) return;
        if (actor.VendedorRuta != null && actor.VendedorRuta.Id == salida.VendedorId) return;
        throw SinPermiso(actor.VendedorRuta != null ? "Solo puede operar sus propias salidas" : "No tiene permiso para operar salidas de ruta");
    }

    // ================= Cierre =================

    public async Task<SalidaRutaDto> CerrarAsync(int salidaId, CerrarSalidaRutaDto dto)
    {
        try
        {
            if (dto.Descuento < 0)
                throw new ValidacionException("El descuento no puede ser negativo");
            if ((dto.DineroEntregado ?? 0) < 0)
                throw new ValidacionException("El dinero entregado no puede ser negativo");
            if (dto.Comprobante != null && dto.Comprobante.Trim().Length > 100)
                throw new ValidacionException("El comprobante no puede superar 100 caracteres");
            if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
                throw new ValidacionException("La observación no puede superar 500 caracteres");

            var salida = await ObtenerSalidaParaEdicionAsync(salidaId);
            await _temporadaActual.VerificarEditableAsync(salida.TemporadaId);
            await VerificarPuedeOperarSalidaAsync(salida);
            ExigirAbierta(salida);

            var dinero = Redondear(dto.DineroEntregado ?? 0);
            var descuentoPedido = Redondear(dto.Descuento);
            if (dinero > 0 && (dto.Medio == null || !Enum.IsDefined(dto.Medio.Value)))
                throw new ValidacionException("El medio de pago es obligatorio si se entrega dinero");

            var (cargado, devuelto) = CalcularUnidadesPorProducto(salida);
            var vendidos = cargado
                .Select(kv => (ProductoId: kv.Key, Unidades: kv.Value - devuelto.GetValueOrDefault(kv.Key)))
                .Where(x => x.Unidades > 0)
                .OrderBy(x => x.ProductoId)
                .ToList();

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;
            var hoy = BoliviaTimeZone.Now();

            // Devolvió todo: se cierra sin crear venta
            if (vendidos.Count == 0)
            {
                if (dinero != 0 || descuentoPedido != 0)
                    throw new ValidacionException("La salida no tiene nada vendido: el dinero entregado y el descuento deben ser 0");

                salida.Estado = EstadoSalidaRuta.Cerrada;
                salida.FechaCierre = hoy;
                salida.Observacion = NormalizarTexto(dto.Observacion) ?? salida.Observacion;
                Tocar(salida, usuarioId, ahora);
                await _uow.BeginTransactionAsync();
                try
                {
                    await _uow.SaveChangesAsync();
                    await _uow.CommitAsync();
                }
                catch
                {
                    await _uow.RollbackAsync();
                    throw;
                }
                _logger.LogInformation("Salida de ruta {Id} cerrada sin venta", salida.Id);
                return await ObtenerSalidaAsync(salida.Id) ?? throw new EntidadNoEncontradaException("Salida de ruta", salida.Id);
            }

            // Un producto desactivado después de cargarlo no bloquea el cierre
            var productos = await _ventas.CargarProductosAsync(salida.TemporadaId, vendidos.Select(v => v.ProductoId));
            var lineas = vendidos
                .Select(v => new VentaLineaDto
                {
                    ProductoId = v.ProductoId,
                    Unidades = v.Unidades,
                    PrecioUnitario = Redondear(productos[v.ProductoId].PrecioCatalogo)
                })
                .ToList();

            var esAdmin = await _acceso.EsAdminAsync();
            var (total, descuento, neto) = CalcularTotales(lineas, descuentoPedido);
            ValidarPrecios(lineas, productos, neto, esAdmin, await _precioAcceso.PuedeVerPreciosCompraAsync());

            if (dinero > neto)
                throw new ValidacionException($"El dinero entregado ({dinero}) supera el neto de la venta ({neto})");
            var saldo = neto - dinero;

            var venta = new VentaNav
            {
                TemporadaId = salida.TemporadaId,
                Fecha = hoy.Date,
                AlmacenId = null,
                Canal = CanalVentaNav.Ruta,
                UsuarioId = usuarioId,
                VendedorId = salida.VendedorId,
                ClienteId = null,
                Tipo = TipoVentaNav.Directa,
                CondicionPago = saldo == 0 ? CondicionPagoVentaNav.Contado : CondicionPagoVentaNav.Credito,
                MontoTotal = total,
                Descuento = descuento,
                MontoNeto = neto,
                Observacion = NormalizarTexto(dto.Observacion),
                Anulada = false
            };
            foreach (var l in lineas)
                venta.Detalles.Add(new VentaNavDetalle
                {
                    ProductoId = l.ProductoId,
                    Unidades = l.Unidades,
                    PrecioUnitario = l.PrecioUnitario,
                    Subtotal = Redondear(l.Unidades * l.PrecioUnitario),
                    UnidadesEntregadas = l.Unidades,
                    EstadoEntrega = EstadoEntregaVentaNav.Entregado,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });
            if (dinero > 0)
                venta.Cobros.Add(new CobroVenta
                {
                    Fecha = hoy.Date,
                    Monto = dinero,
                    Medio = dto.Medio!.Value,
                    Comprobante = NormalizarTexto(dto.Comprobante),
                    AbonoVendedorId = null,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });
            RecalcularEntrega(venta);
            RecalcularPago(venta);

            await _uow.BeginTransactionAsync();
            try
            {
                venta.NroVenta = await _ventaRepo.ObtenerSiguienteNroAsync(salida.TemporadaId);
                var creada = await _ventaRepo.CrearAsync(venta);

                salida.Estado = EstadoSalidaRuta.Cerrada;
                salida.FechaCierre = hoy;
                salida.VentaId = creada.Id;
                Tocar(salida, usuarioId, ahora);
                await _uow.SaveChangesAsync();

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Salida de ruta {Id} cerrada: venta #{Nro}, neto {Neto}", salida.Id, venta.NroVenta, neto);
            return await ObtenerSalidaAsync(salida.Id) ?? throw new EntidadNoEncontradaException("Salida de ruta", salida.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cerrar la salida de ruta {Id}", salidaId);
            throw;
        }
    }

    // ================= Anulaciones (solo admin) =================

    public async Task<SalidaRutaDto> AnularCargaAsync(int cargaId)
    {
        try
        {
            await ExigirAdminAsync();

            var salidaId = await _cargaRepo.AsQueryable().AsNoTracking()
                .Where(c => c.Id == cargaId)
                .Select(c => (int?)c.SalidaRutaId)
                .FirstOrDefaultAsync();
            if (salidaId == null)
                throw new EntidadNoEncontradaException("Carga", cargaId);

            var salida = await ObtenerSalidaParaEdicionAsync(salidaId.Value);
            var carga = salida.Cargas.First(c => c.Id == cargaId);
            if (carga.Anulada)
                throw new ValidacionException("La carga ya está anulada");
            await _temporadaActual.VerificarEditableAsync(salida.TemporadaId);
            ExigirAbierta(salida);

            // Lo devuelto no puede quedar por encima de lo cargado
            var (cargado, devuelto) = CalcularUnidadesPorProducto(salida);
            foreach (var g in carga.Detalles.GroupBy(d => d.ProductoId))
            {
                var restante = cargado.GetValueOrDefault(g.Key) - g.Sum(d => d.Unidades);
                if (restante < devuelto.GetValueOrDefault(g.Key))
                    throw new ExcepcionDominio(
                        $"No se puede anular la carga: el producto {g.Key} quedaría con {restante} cargadas y {devuelto.GetValueOrDefault(g.Key)} ya devueltas",
                        409, "DEVOLUCION_EXCEDE_CARGA");
            }

            foreach (var almacenId in carga.Detalles.Select(d => d.AlmacenOrigenId).Distinct())
                await _acceso.VerificarAlmacenAsync(salida.TemporadaId, almacenId);

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var d in carga.Detalles)
                    await _stock.RegistrarEntradaAsync(salida.TemporadaId, d.AlmacenOrigenId, d.ProductoId, d.Unidades,
                        TipoMovimientoNav.AnulacionSalidaRuta, ReferenciaSalida, salida.Id);
                carga.Anulada = true;
                Tocar(carga, usuarioId, ahora);
                Tocar(salida, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Carga {CargaId} anulada (salida {Id})", cargaId, salida.Id);
            return await ObtenerSalidaAsync(salida.Id) ?? throw new EntidadNoEncontradaException("Salida de ruta", salida.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular carga {Id}", cargaId);
            throw;
        }
    }

    public async Task<SalidaRutaDto> AnularDevolucionAsync(int devolucionId)
    {
        try
        {
            await ExigirAdminAsync();

            var salidaId = await _devolucionRepo.AsQueryable().AsNoTracking()
                .Where(d => d.Id == devolucionId)
                .Select(d => (int?)d.SalidaRutaId)
                .FirstOrDefaultAsync();
            if (salidaId == null)
                throw new EntidadNoEncontradaException("Devolución", devolucionId);

            var salida = await ObtenerSalidaParaEdicionAsync(salidaId.Value);
            var devolucion = salida.Devoluciones.First(d => d.Id == devolucionId);
            if (devolucion.Anulada)
                throw new ValidacionException("La devolución ya está anulada");
            if (devolucion.EsTardia)
                throw new ValidacionException("Una devolución tardía no se anula por separado");
            await _temporadaActual.VerificarEditableAsync(salida.TemporadaId);
            ExigirAbierta(salida);

            foreach (var almacenId in devolucion.Detalles.Select(d => d.AlmacenDestinoId).Distinct())
                await _acceso.VerificarAlmacenAsync(salida.TemporadaId, almacenId);

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var d in devolucion.Detalles)
                    await _stock.RegistrarSalidaAsync(salida.TemporadaId, d.AlmacenDestinoId, d.ProductoId, d.Unidades,
                        TipoMovimientoNav.AnulacionDevolucionRuta, ReferenciaSalida, salida.Id);
                devolucion.Anulada = true;
                Tocar(devolucion, usuarioId, ahora);
                Tocar(salida, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Devolución {DevolucionId} anulada (salida {Id})", devolucionId, salida.Id);
            return await ObtenerSalidaAsync(salida.Id) ?? throw new EntidadNoEncontradaException("Salida de ruta", salida.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular devolución {Id}", devolucionId);
            throw;
        }
    }

    public async Task<SalidaRutaDto> AnularCierreAsync(int salidaId)
    {
        try
        {
            await ExigirAdminAsync();

            var salida = await ObtenerSalidaParaEdicionAsync(salidaId);
            if (salida.Estado != EstadoSalidaRuta.Cerrada)
                throw new ExcepcionDominio("La salida no está cerrada", 409, "SALIDA_NO_CERRADA");
            await _temporadaActual.VerificarEditableAsync(salida.TemporadaId);

            var otraAbierta = await _repo.AsQueryable().AsNoTracking()
                .AnyAsync(s => s.IsActive && s.TemporadaId == salida.TemporadaId && s.VendedorId == salida.VendedorId
                    && s.Estado == EstadoSalidaRuta.Abierta && s.Id != salida.Id);
            if (otraAbierta)
                throw new ExcepcionDominio("El vendedor ya tiene otra salida abierta: no se puede reabrir esta", 409, "SALIDA_ABIERTA_EXISTE");

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            VentaNav? venta = null;
            if (salida.VentaId.HasValue)
            {
                venta = await _ventas.ObtenerVentaParaEdicionAsync(salida.VentaId.Value);
                if (!venta.Anulada)
                {
                    var conAbonos = await _abonoRepo.AsQueryable().AsNoTracking()
                        .AnyAsync(a => !a.Anulado && a.Aplicaciones.Any(x => x.VentaId == venta.Id));
                    if (venta.Cobros.Any(c => !c.Anulado) || conAbonos)
                        throw new ExcepcionDominio("La venta tiene cobros o abonos vigentes: anúlelos primero", 409, "VENTA_CON_COBROS");
                }
            }

            await _uow.BeginTransactionAsync();
            try
            {
                if (venta != null && !venta.Anulada)
                {
                    // Igual que VentaNavService.AnularAsync
                    venta.Anulada = true;
                    venta.MontoCobrado = 0;
                    venta.MontoSaldo = 0;
                    venta.EstadoPago = EstadoPagoVentaNav.Pendiente;
                    Tocar(venta, usuarioId, ahora);
                }
                salida.Estado = EstadoSalidaRuta.Abierta;
                salida.FechaCierre = null;
                salida.VentaId = null;
                Tocar(salida, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Cierre de la salida de ruta {Id} anulado", salida.Id);
            return await ObtenerSalidaAsync(salida.Id) ?? throw new EntidadNoEncontradaException("Salida de ruta", salida.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular el cierre de la salida {Id}", salidaId);
            throw;
        }
    }

    public async Task<SalidaRutaDto> AnularSalidaAsync(int salidaId)
    {
        try
        {
            await ExigirAdminAsync();

            var salida = await ObtenerSalidaParaEdicionAsync(salidaId);
            await _temporadaActual.VerificarEditableAsync(salida.TemporadaId);
            ExigirAbierta(salida);
            if (salida.Devoluciones.Any(d => !d.Anulada))
                throw new ExcepcionDominio("La salida tiene devoluciones vigentes: anúlelas primero", 409, "SALIDA_CON_DEVOLUCIONES");

            var cargas = salida.Cargas.Where(c => !c.Anulada).ToList();
            foreach (var almacenId in cargas.SelectMany(c => c.Detalles).Select(d => d.AlmacenOrigenId).Distinct())
                await _acceso.VerificarAlmacenAsync(salida.TemporadaId, almacenId);

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var carga in cargas)
                {
                    foreach (var d in carga.Detalles)
                        await _stock.RegistrarEntradaAsync(salida.TemporadaId, d.AlmacenOrigenId, d.ProductoId, d.Unidades,
                            TipoMovimientoNav.AnulacionSalidaRuta, ReferenciaSalida, salida.Id);
                    carga.Anulada = true;
                    Tocar(carga, usuarioId, ahora);
                }
                salida.Estado = EstadoSalidaRuta.Anulada;
                Tocar(salida, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Salida de ruta {Id} anulada", salida.Id);
            return await ObtenerSalidaAsync(salida.Id) ?? throw new EntidadNoEncontradaException("Salida de ruta", salida.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular la salida de ruta {Id}", salidaId);
            throw;
        }
    }

    // ================= Deuda y abonos =================

    public async Task<List<DeudaVendedorRutaDto>> ObtenerDeudasAsync(int? vendedorId)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var forzado = await RestriccionLecturaAsync(temporada.Id);
            var filtro = forzado ?? vendedorId;

            var vendedorQuery = _vendedorRepo.AsQueryable().AsNoTracking()
                .Where(v => v.TemporadaId == temporada.Id && v.IsActive && v.Tipo == TipoVendedor.Ruta);
            if (filtro.HasValue)
                vendedorQuery = vendedorQuery.Where(v => v.Id == filtro.Value);
            var vendedores = await vendedorQuery.ToListAsync();
            var vendedorIds = vendedores.Select(v => v.Id).ToList();
            var nombres = await _ventas.ObtenerNombresUsuarioAsync(vendedores.Select(v => v.UsuarioId));

            var ventas = await _ventaRepo.AsQueryable().AsNoTracking()
                .Where(v => v.IsActive && !v.Anulada && v.Canal == CanalVentaNav.Ruta && v.TemporadaId == temporada.Id && vendedorIds.Contains(v.VendedorId))
                .Select(v => new
                {
                    v.VendedorId,
                    v.MontoTotal,
                    v.Descuento,
                    v.MontoCobrado,
                    v.MontoSaldo,
                    Unidades = v.Detalles.Sum(d => d.Unidades)
                })
                .ToListAsync();

            var abiertas = await _repo.AsQueryable().AsNoTracking()
                .Where(s => s.IsActive && s.TemporadaId == temporada.Id && s.Estado == EstadoSalidaRuta.Abierta && vendedorIds.Contains(s.VendedorId))
                .Select(s => new { s.Id, s.VendedorId })
                .ToListAsync();
            var unidades = await ObtenerUnidadesAsync(abiertas.Select(s => s.Id).ToList());

            return vendedores
                .Select(v =>
                {
                    var suyas = ventas.Where(x => x.VendedorId == v.Id).ToList();
                    var abiertasV = abiertas.Where(s => s.VendedorId == v.Id).ToList();
                    return new DeudaVendedorRutaDto
                    {
                        VendedorId = v.Id,
                        VendedorNombre = nombres.GetValueOrDefault(v.UsuarioId, string.Empty),
                        SalidasAbiertas = abiertasV.Count,
                        UnidadesEnPoder = abiertasV.Sum(s =>
                        {
                            var (c, d) = unidades.GetValueOrDefault(s.Id);
                            return c - d;
                        }),
                        UnidadesVendidas = suyas.Sum(x => x.Unidades),
                        MontoVendido = suyas.Sum(x => x.MontoTotal),
                        Descuentos = suyas.Sum(x => x.Descuento),
                        MontoCobrado = suyas.Sum(x => x.MontoCobrado),
                        Saldo = suyas.Sum(x => x.MontoSaldo)
                    };
                })
                .OrderBy(x => x.VendedorNombre, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la deuda de vendedores de ruta");
            throw;
        }
    }

    public async Task<PagedResultDto<AbonoVendedorDto>> ObtenerAbonosAsync(AbonoVendedorFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);
            var forzado = await RestriccionLecturaAsync(temporadaId);

            var baseQuery = _abonoRepo.AsQueryable()
                .AsNoTracking()
                .Include(a => a.Vendedor)
                .Where(a => a.IsActive && a.TemporadaId == temporadaId);

            if (forzado.HasValue)
                baseQuery = baseQuery.Where(a => a.VendedorId == forzado.Value);
            else if (query.VendedorId.HasValue)
                baseQuery = baseQuery.Where(a => a.VendedorId == query.VendedorId.Value);
            if (!query.IncluirAnulados)
                baseQuery = baseQuery.Where(a => !a.Anulado);
            if (query.Desde.HasValue)
            {
                var desde = query.Desde.Value.Date;
                baseQuery = baseQuery.Where(a => a.Fecha >= desde);
            }
            if (query.Hasta.HasValue)
            {
                var hastaExclusivo = query.Hasta.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(a => a.Fecha < hastaExclusivo);
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var nombres = await _ventas.ObtenerNombresUsuarioAsync(
                entidades.Select(a => a.UsuarioId).Concat(entidades.Where(a => a.Vendedor != null).Select(a => a.Vendedor!.UsuarioId)));

            return new PagedResultDto<AbonoVendedorDto>
            {
                Items = entidades.Select(a => MapearAbono(a, nombres, null)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener abonos de vendedores");
            throw;
        }
    }

    public async Task<AbonoVendedorDto?> ObtenerAbonoAsync(int id)
    {
        try
        {
            var abono = await _abonoRepo.ObtenerConDetallesAsync(id);
            if (abono == null || !abono.IsActive) return null;

            var forzado = await RestriccionLecturaAsync(abono.TemporadaId);
            if (forzado.HasValue && forzado.Value != abono.VendedorId)
                throw SinPermiso("Solo puede ver sus propios abonos");

            return await MapearAbonoConDetallesAsync(abono);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener abono {Id}", id);
            throw;
        }
    }

    public async Task<AbonoVendedorDto> RegistrarAbonoAsync(CrearAbonoVendedorDto dto)
    {
        try
        {
            await ExigirAdminAsync();
            var monto = Redondear(dto.Monto);
            var descuento = Redondear(dto.Descuento);
            if (dto.VendedorId <= 0)
                throw new ValidacionException("El vendedor es obligatorio");
            if (monto < 0)
                throw new ValidacionException("El monto no puede ser negativo");
            if (descuento < 0)
                throw new ValidacionException("El descuento no puede ser negativo");
            if (monto + descuento <= 0)
                throw new ValidacionException("El monto más el descuento debe ser mayor a 0");
            if (monto > 0 && (dto.Medio == null || !Enum.IsDefined(dto.Medio.Value)))
                throw new ValidacionException("El medio de pago es obligatorio si hay monto");
            if (dto.Comprobante != null && dto.Comprobante.Trim().Length > 100)
                throw new ValidacionException("El comprobante no puede superar 100 caracteres");
            if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
                throw new ValidacionException("La observación no puede superar 500 caracteres");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await ExigirAdminAsync();
            await ValidarVendedorRutaAsync(temporada.Id, dto.VendedorId);

            // Si se indica una salida, el abono se aplica solo a la venta de esa salida
            int? ventaSalidaId = null;
            if (dto.SalidaRutaId.HasValue)
            {
                var salida = await _repo.AsQueryable().AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == dto.SalidaRutaId.Value);
                if (salida == null || !salida.IsActive)
                    throw new EntidadNoEncontradaException("Salida de ruta", dto.SalidaRutaId.Value);
                if (salida.TemporadaId != temporada.Id)
                    throw new ValidacionException("La salida no pertenece a la temporada actual");
                if (salida.VendedorId != dto.VendedorId)
                    throw new ValidacionException("La salida no pertenece al vendedor indicado");
                if (!salida.VentaId.HasValue)
                    throw new ValidacionException("La salida no tiene saldo pendiente");
                ventaSalidaId = salida.VentaId.Value;
            }

            // Ventas de ruta con saldo, de la más antigua a la más nueva (con tracking: se modifican)
            var ventasQuery = _ventaRepo.AsQueryable()
                .Include(v => v.Cobros)
                .Include(v => v.Detalles)
                .Where(v => v.IsActive && !v.Anulada && v.Canal == CanalVentaNav.Ruta && v.TemporadaId == temporada.Id
                    && v.VendedorId == dto.VendedorId && v.MontoSaldo > 0);
            if (ventaSalidaId.HasValue)
                ventasQuery = ventasQuery.Where(v => v.Id == ventaSalidaId.Value);
            var ventas = await ventasQuery
                .OrderBy(v => v.Fecha).ThenBy(v => v.NroVenta).ThenBy(v => v.Id)
                .ToListAsync();

            var deuda = ventas.Sum(v => v.MontoSaldo);
            if (ventaSalidaId.HasValue)
            {
                if (ventas.Count == 0)
                    throw new ValidacionException("La salida no tiene saldo pendiente");
                if (monto + descuento > deuda)
                    throw new ValidacionException($"El abono supera el saldo de la salida (Bs {deuda})");
            }
            else if (monto + descuento > deuda)
                throw new ValidacionException($"El monto más el descuento ({monto + descuento}) supera la deuda del vendedor ({deuda})");

            var esAdmin = await _acceso.EsAdminAsync();
            Dictionary<int, ProductoNav>? productos = null;
            if (descuento > 0 && !esAdmin)
                productos = await _ventas.CargarProductosAsync(temporada.Id,
                    ventas.SelectMany(v => v.Detalles).Where(d => d.Unidades > 0).Select(d => d.ProductoId));

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;
            var hoy = BoliviaTimeZone.Now().Date;

            var abono = new AbonoVendedor
            {
                TemporadaId = temporada.Id,
                VendedorId = dto.VendedorId,
                Fecha = hoy,
                UsuarioId = usuarioId,
                Monto = monto,
                Descuento = descuento,
                Medio = monto > 0 ? dto.Medio : null,
                Comprobante = NormalizarTexto(dto.Comprobante),
                Observacion = NormalizarTexto(dto.Observacion),
                Anulado = false
            };

            await _uow.BeginTransactionAsync();
            try
            {
                var montoRestante = monto;
                var descuentoRestante = descuento;
                foreach (var venta in ventas)
                {
                    if (montoRestante <= 0 && descuentoRestante <= 0) break;

                    var saldo = venta.MontoSaldo;
                    var cobro = Math.Min(montoRestante, saldo);
                    var desc = Math.Min(descuentoRestante, saldo - cobro);
                    if (cobro <= 0 && desc <= 0) continue;

                    if (cobro > 0)
                        venta.Cobros.Add(new CobroVenta
                        {
                            Fecha = hoy,
                            Monto = cobro,
                            Medio = dto.Medio!.Value,
                            Comprobante = NormalizarTexto(dto.Comprobante),
                            AbonoVendedor = abono,
                            CreatedByUsuarioId = usuarioId,
                            CreatedAt = ahora
                        });
                    if (desc > 0)
                    {
                        venta.Descuento += desc;
                        venta.MontoNeto = venta.MontoTotal - venta.Descuento;
                        if (productos != null)
                        {
                            var lineas = venta.Detalles.Where(d => d.Unidades > 0)
                                .Select(d => new VentaLineaDto { ProductoId = d.ProductoId, Unidades = d.Unidades, PrecioUnitario = d.PrecioUnitario })
                                .ToList();
                            ValidarPrecios(lineas, productos, venta.MontoNeto, esAdmin, await _precioAcceso.PuedeVerPreciosCompraAsync());
                        }
                    }
                    RecalcularPago(venta);
                    Tocar(venta, usuarioId, ahora);

                    abono.Aplicaciones.Add(new AbonoVendedorAplicacion
                    {
                        VentaId = venta.Id,
                        MontoCobrado = cobro,
                        MontoDescuento = desc,
                        CreatedByUsuarioId = usuarioId,
                        CreatedAt = ahora
                    });
                    montoRestante -= cobro;
                    descuentoRestante -= desc;
                }

                await _abonoRepo.CrearAsync(abono);
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Abono de vendedor {VendedorId} registrado: {Id}, monto {Monto}, descuento {Descuento}",
                dto.VendedorId, abono.Id, monto, descuento);
            return await ObtenerAbonoAsync(abono.Id) ?? throw new EntidadNoEncontradaException("Abono", abono.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar abono de vendedor");
            throw;
        }
    }

    public async Task<AbonoVendedorDto> AnularAbonoAsync(int id)
    {
        try
        {
            await ExigirAdminAsync();

            var abono = await _abonoRepo.ObtenerParaEdicionAsync(id);
            if (abono == null || !abono.IsActive)
                throw new EntidadNoEncontradaException("Abono", id);
            if (abono.Anulado)
                throw new ValidacionException("El abono ya está anulado");
            await _temporadaActual.VerificarEditableAsync(abono.TemporadaId);

            var ventaIds = abono.Aplicaciones.Select(a => a.VentaId).Distinct().ToList();
            var ventas = await _ventaRepo.AsQueryable()
                .Include(v => v.Cobros)
                .Where(v => ventaIds.Contains(v.Id))
                .ToListAsync();

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var venta in ventas)
                {
                    // Si la venta ya está anulada solo se anula el abono
                    if (venta.Anulada) continue;

                    foreach (var cobro in venta.Cobros.Where(c => c.AbonoVendedorId == abono.Id && !c.Anulado))
                    {
                        cobro.Anulado = true;
                        Tocar(cobro, usuarioId, ahora);
                    }
                    var descuentoAbono = abono.Aplicaciones.Where(a => a.VentaId == venta.Id).Sum(a => a.MontoDescuento);
                    venta.Descuento = Math.Max(0, venta.Descuento - descuentoAbono);
                    venta.MontoNeto = venta.MontoTotal - venta.Descuento;
                    RecalcularPago(venta);
                    Tocar(venta, usuarioId, ahora);
                }
                abono.Anulado = true;
                Tocar(abono, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Abono {Id} anulado", id);
            return await ObtenerAbonoAsync(id) ?? throw new EntidadNoEncontradaException("Abono", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular abono {Id}", id);
            throw;
        }
    }

    // ================= Editar venta de ruta (solo admin) =================

    public async Task<VentaNavDto> EditarPreciosVentaAsync(int ventaId, EditarPreciosVentaRutaDto dto)
    {
        try
        {
            await ExigirAdminAsync();

            if (dto.Detalles == null || dto.Detalles.Count == 0)
                throw new ValidacionException("Debe indicar al menos un producto");
            if (dto.Detalles.Any(d => d.ProductoId <= 0))
                throw new ValidacionException("El producto es obligatorio");
            if (dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count)
                throw new ValidacionException("Hay productos repetidos");
            if (dto.Detalles.Any(d => d.PrecioUnitario < 0))
                throw new ValidacionException("El precio unitario no puede ser negativo");
            if (dto.Descuento.HasValue && dto.Descuento.Value < 0)
                throw new ValidacionException("El descuento no puede ser negativo");

            var venta = await ObtenerVentaDeRutaParaEdicionAsync(ventaId);
            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);

            var existentes = venta.Detalles.Where(d => d.Unidades > 0).ToDictionary(d => d.ProductoId);
            foreach (var l in dto.Detalles)
                if (!existentes.ContainsKey(l.ProductoId))
                    throw new ValidacionException($"El producto {l.ProductoId} no está en la venta");

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            // Las líneas no indicadas quedan igual
            var lineas = venta.Detalles.Where(d => d.Unidades > 0)
                .Select(d => new VentaLineaDto
                {
                    ProductoId = d.ProductoId,
                    Unidades = d.Unidades,
                    PrecioUnitario = dto.Detalles.FirstOrDefault(x => x.ProductoId == d.ProductoId)?.PrecioUnitario ?? d.PrecioUnitario
                })
                .ToList();
            if (dto.Descuento.HasValue)
            {
                var descuentoAbonos = (await _abonoRepo.AsQueryable().AsNoTracking()
                    .Where(a => !a.Anulado)
                    .SelectMany(a => a.Aplicaciones)
                    .Where(x => x.VentaId == venta.Id && x.IsActive)
                    .Select(x => x.MontoDescuento)
                    .ToListAsync()).Sum();
                if (Redondear(dto.Descuento.Value) < descuentoAbonos)
                    throw new ValidacionException(
                        $"El descuento no puede ser menor a {descuentoAbonos}, que aportaron los abonos vigentes: anule primero el abono");
            }
            var (total, descuento, neto) = CalcularTotales(lineas, dto.Descuento ?? venta.Descuento);
            // R4: el editor es siempre admin, que puede vender por debajo de la compra (ValidarPrecios no restringe)

            if (neto < venta.MontoCobrado)
                throw new ExcepcionDominio(
                    $"El nuevo neto ({neto}) es menor a lo ya cobrado ({venta.MontoCobrado}): primero anule un cobro",
                    409, "NETO_MENOR_COBRADO");

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var l in dto.Detalles)
                {
                    var d = existentes[l.ProductoId];
                    d.PrecioUnitario = Redondear(l.PrecioUnitario);
                    d.Subtotal = Redondear(d.Unidades * d.PrecioUnitario);
                    Tocar(d, usuarioId, ahora);
                }
                venta.MontoTotal = total;
                venta.Descuento = descuento;
                venta.MontoNeto = neto;
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

            _logger.LogInformation("Precios de la venta de ruta {Id} editados", ventaId);
            return await _ventas.ObtenerPorIdAsync(ventaId) ?? throw new EntidadNoEncontradaException("Venta", ventaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar precios de la venta de ruta {Id}", ventaId);
            throw;
        }
    }

    public async Task<VentaNavDto> DevolverVentaAsync(int ventaId, DevolverVentaRutaDto dto)
    {
        try
        {
            await ExigirAdminAsync();

            if (dto.AlmacenDestinoId <= 0)
                throw new ValidacionException("El almacén de destino es obligatorio");
            if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
                throw new ValidacionException("La observación no puede superar 500 caracteres");
            if (dto.Detalles == null || dto.Detalles.Count == 0)
                throw new ValidacionException("Debe indicar al menos un producto");
            if (dto.Detalles.Any(d => d.ProductoId <= 0))
                throw new ValidacionException("El producto es obligatorio");
            if (dto.Detalles.Any(d => d.Unidades <= 0))
                throw new ValidacionException("Las unidades deben ser mayores a 0");
            if (dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count)
                throw new ValidacionException("Hay productos repetidos");

            var venta = await ObtenerVentaDeRutaParaEdicionAsync(ventaId);
            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);

            var salidaId = await _repo.AsQueryable().AsNoTracking()
                .Where(s => s.VentaId == ventaId && s.IsActive)
                .Select(s => s.Id)
                .FirstAsync();
            var salida = await ObtenerSalidaParaEdicionAsync(salidaId);
            if (salida.Estado != EstadoSalidaRuta.Cerrada)
                throw new ExcepcionDominio("La salida de la venta no está cerrada", 409, "SALIDA_NO_CERRADA");

            await _ventas.ValidarAlmacenAsync(dto.AlmacenDestinoId, exigirTienda: false);

            var lineas = new List<(VentaNavDetalle Linea, int Unidades)>();
            foreach (var l in dto.Detalles)
            {
                var linea = venta.Detalles.FirstOrDefault(d => d.ProductoId == l.ProductoId && d.Unidades > 0)
                    ?? throw new ValidacionException($"El producto {l.ProductoId} no está en la venta");
                if (l.Unidades > linea.Unidades)
                    throw new ValidacionException($"No se pueden devolver {l.Unidades} del producto {l.ProductoId}: la venta solo tiene {linea.Unidades}");
                lineas.Add((linea, l.Unidades));
            }

            var nuevoTotal = Redondear(venta.Detalles.Sum(d =>
            {
                var baja = lineas.Where(x => x.Linea.Id == d.Id).Sum(x => x.Unidades);
                return Redondear((d.Unidades - baja) * d.PrecioUnitario);
            }));
            if (venta.Descuento > nuevoTotal)
                throw new ValidacionException($"El descuento ({venta.Descuento}) superaría el nuevo total ({nuevoTotal})");
            var nuevoNeto = nuevoTotal - venta.Descuento;
            if (nuevoNeto < venta.MontoCobrado)
                throw new ExcepcionDominio(
                    $"El nuevo neto ({nuevoNeto}) es menor a lo ya cobrado ({venta.MontoCobrado}): primero anule un cobro",
                    409, "NETO_MENOR_COBRADO");

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            var devolucion = new DevolucionRuta
            {
                Fecha = BoliviaTimeZone.Now(),
                UsuarioId = usuarioId,
                EsTardia = true,
                Anulada = false,
                Observacion = NormalizarTexto(dto.Observacion),
                CreatedByUsuarioId = usuarioId,
                CreatedAt = ahora
            };

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var (linea, unidades) in lineas)
                {
                    linea.Unidades -= unidades;
                    linea.UnidadesEntregadas = Math.Max(0, linea.UnidadesEntregadas - unidades);
                    linea.Subtotal = Redondear(linea.Unidades * linea.PrecioUnitario);
                    Tocar(linea, usuarioId, ahora);
                    devolucion.Detalles.Add(new DevolucionRutaDetalle
                    {
                        AlmacenDestinoId = dto.AlmacenDestinoId,
                        ProductoId = linea.ProductoId,
                        Unidades = unidades,
                        CreatedByUsuarioId = usuarioId,
                        CreatedAt = ahora
                    });
                }
                venta.MontoTotal = nuevoTotal;
                venta.MontoNeto = nuevoNeto;
                RecalcularEntrega(venta);
                RecalcularPago(venta);
                Tocar(venta, usuarioId, ahora);

                salida.Devoluciones.Add(devolucion);
                Tocar(salida, usuarioId, ahora);
                await _uow.SaveChangesAsync();

                foreach (var d in devolucion.Detalles)
                    await _stock.RegistrarEntradaAsync(venta.TemporadaId, d.AlmacenDestinoId, d.ProductoId, d.Unidades,
                        TipoMovimientoNav.DevolucionRuta, ReferenciaSalida, salida.Id);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Devolución tardía de la venta de ruta {Id} (salida {SalidaId})", ventaId, salida.Id);
            return await _ventas.ObtenerPorIdAsync(ventaId) ?? throw new EntidadNoEncontradaException("Venta", ventaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al devolver productos de la venta de ruta {Id}", ventaId);
            throw;
        }
    }

    // Venta de canal Ruta, no anulada, ligada a una salida
    private async Task<VentaNav> ObtenerVentaDeRutaParaEdicionAsync(int ventaId)
    {
        var venta = await _ventas.ObtenerVentaParaEdicionAsync(ventaId);
        if (venta.Canal != CanalVentaNav.Ruta)
            throw new ValidacionException("La venta no es de ruta");
        if (venta.Anulada)
            throw new ValidacionException("La venta está anulada");
        var ligada = await _repo.AsQueryable().AsNoTracking().AnyAsync(s => s.VentaId == ventaId && s.IsActive);
        if (!ligada)
            throw new ValidacionException("La venta no pertenece a una salida de ruta");
        return venta;
    }

    // ================= Helpers =================

    private async Task<SalidaRuta> ObtenerSalidaParaEdicionAsync(int id)
    {
        var salida = await _repo.ObtenerParaEdicionAsync(id);
        if (salida == null || !salida.IsActive)
            throw new EntidadNoEncontradaException("Salida de ruta", id);
        return salida;
    }

    private static void ExigirAbierta(SalidaRuta salida)
    {
        if (salida.Estado != EstadoSalidaRuta.Abierta)
            throw new ExcepcionDominio("La salida no está abierta", 409, "SALIDA_NO_ABIERTA");
    }

    // Cargado y devuelto vigentes (no anulados; el devuelto incluye las tardías) por producto
    private static (Dictionary<int, int> Cargado, Dictionary<int, int> Devuelto) CalcularUnidadesPorProducto(SalidaRuta salida)
    {
        var cargado = salida.Cargas.Where(c => !c.Anulada).SelectMany(c => c.Detalles)
            .GroupBy(d => d.ProductoId).ToDictionary(g => g.Key, g => g.Sum(d => d.Unidades));
        var devuelto = salida.Devoluciones.Where(d => !d.Anulada).SelectMany(d => d.Detalles)
            .GroupBy(d => d.ProductoId).ToDictionary(g => g.Key, g => g.Sum(d => d.Unidades));
        return (cargado, devuelto);
    }

    // ================= Mapeo =================

    private static string NombreEstado(EstadoSalidaRuta e) => e switch
    {
        EstadoSalidaRuta.Abierta => "Abierta",
        EstadoSalidaRuta.Cerrada => "Cerrada",
        _ => "Anulada"
    };

    private static string NombreEstadoPago(EstadoPagoVentaNav e) => e switch
    {
        EstadoPagoVentaNav.Pagado => "Pagado",
        EstadoPagoVentaNav.Parcial => "Parcial",
        _ => "Pendiente"
    };

    private async Task<SalidaRutaDto> MapearSalidaAsync(SalidaRuta s)
    {
        var (cargado, devuelto) = CalcularUnidadesPorProducto(s);

        var almacenes = await _ventas.ObtenerNombresAlmacenAsync(
            s.Cargas.SelectMany(c => c.Detalles).Select(d => d.AlmacenOrigenId)
                .Concat(s.Devoluciones.SelectMany(d => d.Detalles).Select(d => d.AlmacenDestinoId)));
        var usuarioIds = s.Cargas.Select(c => c.UsuarioId).Concat(s.Devoluciones.Select(d => d.UsuarioId)).ToList();
        if (s.Vendedor != null) usuarioIds.Add(s.Vendedor.UsuarioId);
        var usuarios = await _ventas.ObtenerNombresUsuarioAsync(usuarioIds);

        VentaNav? venta = null;
        if (s.VentaId.HasValue)
            venta = await _ventaRepo.AsQueryable().AsNoTracking()
                .Include(v => v.Detalles)
                .FirstOrDefaultAsync(v => v.Id == s.VentaId.Value);
        var lineasVenta = venta != null && !venta.Anulada
            ? venta.Detalles.Where(d => d.Unidades > 0).ToDictionary(d => d.ProductoId)
            : new Dictionary<int, VentaNavDetalle>();

        var productosPorId = s.Cargas.SelectMany(c => c.Detalles).Select(d => d.Producto)
            .Concat(s.Devoluciones.SelectMany(d => d.Detalles).Select(d => d.Producto))
            .Where(p => p != null)
            .GroupBy(p => p!.Id)
            .ToDictionary(g => g.Key, g => g.First()!);

        var abierta = s.Estado == EstadoSalidaRuta.Abierta;
        var productos = cargado.Keys.Union(devuelto.Keys)
            .Where(id => productosPorId.ContainsKey(id))
            .Select(id =>
            {
                var p = productosPorId[id];
                var c = cargado.GetValueOrDefault(id);
                var d = devuelto.GetValueOrDefault(id);
                var vendido = c - d;
                lineasVenta.TryGetValue(id, out var lv);
                return new SalidaRutaProductoDto
                {
                    ProductoId = id,
                    ProductoNombreMostrar = NombreMostrar(p),
                    UnidadesPorEmpaque = p.UnidadesPorEmpaque,
                    NombreEmpaque = p.NombreEmpaque,
                    Color = p.Color,
                    Cargado = c,
                    Devuelto = d,
                    EnPoder = abierta ? vendido : 0,
                    Vendido = vendido,
                    PrecioCatalogo = p.PrecioCatalogo,
                    Subtotal = Redondear(vendido * p.PrecioCatalogo),
                    PrecioUnitario = lv?.PrecioUnitario,
                    UnidadesVenta = lv?.Unidades
                };
            })
            .OrderBy(p => p.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        return new SalidaRutaDto
        {
            Id = s.Id,
            TemporadaId = s.TemporadaId,
            VendedorId = s.VendedorId,
            VendedorNombre = s.Vendedor != null ? usuarios.GetValueOrDefault(s.Vendedor.UsuarioId, string.Empty) : string.Empty,
            Estado = (int)s.Estado,
            EstadoNombre = NombreEstado(s.Estado),
            FechaApertura = s.FechaApertura,
            FechaCierre = s.FechaCierre,
            Observacion = s.Observacion,
            VentaId = s.VentaId,
            Venta = venta == null ? null : new ResumenVentaRutaDto
            {
                Id = venta.Id,
                NroVenta = venta.NroVenta,
                MontoTotal = venta.MontoTotal,
                Descuento = venta.Descuento,
                MontoNeto = venta.MontoNeto,
                MontoCobrado = venta.MontoCobrado,
                MontoSaldo = venta.MontoSaldo,
                EstadoPago = (int)venta.EstadoPago,
                EstadoPagoNombre = NombreEstadoPago(venta.EstadoPago),
                Anulada = venta.Anulada
            },
            UnidadesCargadas = cargado.Values.Sum(),
            UnidadesDevueltas = devuelto.Values.Sum(),
            UnidadesEnPoder = productos.Sum(p => p.EnPoder),
            UnidadesVendidas = productos.Sum(p => p.Vendido),
            TotalCatalogo = productos.Sum(p => p.Subtotal),
            Productos = productos,
            Cargas = s.Cargas
                .OrderBy(c => c.Fecha).ThenBy(c => c.Id)
                .Select(c => new SalidaRutaCargaDto
                {
                    Id = c.Id,
                    Fecha = c.Fecha,
                    UsuarioId = c.UsuarioId,
                    UsuarioNombre = usuarios.GetValueOrDefault(c.UsuarioId, string.Empty),
                    Anulada = c.Anulada,
                    Detalles = c.Detalles.OrderBy(d => d.Id).Select(d => new SalidaRutaCargaLineaDto
                    {
                        Id = d.Id,
                        AlmacenOrigenId = d.AlmacenOrigenId,
                        AlmacenNombre = almacenes.GetValueOrDefault(d.AlmacenOrigenId, string.Empty),
                        ProductoId = d.ProductoId,
                        ProductoNombreMostrar = d.Producto != null ? NombreMostrar(d.Producto) : string.Empty,
                        UnidadesPorEmpaque = d.Producto?.UnidadesPorEmpaque,
                        NombreEmpaque = d.Producto?.NombreEmpaque,
                        Color = d.Producto?.Color,
                        Unidades = d.Unidades
                    }).ToList()
                }).ToList(),
            Devoluciones = s.Devoluciones
                .OrderBy(d => d.Fecha).ThenBy(d => d.Id)
                .Select(d => new DevolucionRutaDto
                {
                    Id = d.Id,
                    Fecha = d.Fecha,
                    UsuarioId = d.UsuarioId,
                    UsuarioNombre = usuarios.GetValueOrDefault(d.UsuarioId, string.Empty),
                    EsTardia = d.EsTardia,
                    Anulada = d.Anulada,
                    Observacion = d.Observacion,
                    Detalles = d.Detalles.OrderBy(x => x.Id).Select(x => new DevolucionRutaLineaDto
                    {
                        Id = x.Id,
                        AlmacenDestinoId = x.AlmacenDestinoId,
                        AlmacenNombre = almacenes.GetValueOrDefault(x.AlmacenDestinoId, string.Empty),
                        ProductoId = x.ProductoId,
                        ProductoNombreMostrar = x.Producto != null ? NombreMostrar(x.Producto) : string.Empty,
                        UnidadesPorEmpaque = x.Producto?.UnidadesPorEmpaque,
                        NombreEmpaque = x.Producto?.NombreEmpaque,
                        Color = x.Producto?.Color,
                        Unidades = x.Unidades
                    }).ToList()
                }).ToList(),
            IsActive = s.IsActive,
            CreatedAt = BoliviaTimeZone.ToLocal(s.CreatedAt)
        };
    }

    private static AbonoVendedorDto MapearAbono(AbonoVendedor a, Dictionary<int, string> usuarios, Dictionary<int, int>? nrosVenta) => new()
    {
        Id = a.Id,
        TemporadaId = a.TemporadaId,
        VendedorId = a.VendedorId,
        VendedorNombre = a.Vendedor != null ? usuarios.GetValueOrDefault(a.Vendedor.UsuarioId, string.Empty) : string.Empty,
        Fecha = a.Fecha,
        UsuarioId = a.UsuarioId,
        UsuarioNombre = usuarios.GetValueOrDefault(a.UsuarioId, string.Empty),
        Monto = a.Monto,
        Descuento = a.Descuento,
        Medio = a.Medio.HasValue ? (int)a.Medio.Value : null,
        MedioNombre = a.Medio?.ToString(),
        Comprobante = a.Comprobante,
        Observacion = a.Observacion,
        Anulado = a.Anulado,
        Aplicaciones = nrosVenta == null
            ? new List<AbonoVendedorAplicacionDto>()
            : a.Aplicaciones.OrderBy(x => x.Id).Select(x => new AbonoVendedorAplicacionDto
            {
                Id = x.Id,
                VentaId = x.VentaId,
                NroVenta = nrosVenta.GetValueOrDefault(x.VentaId),
                MontoCobrado = x.MontoCobrado,
                MontoDescuento = x.MontoDescuento
            }).ToList(),
        CreatedAt = BoliviaTimeZone.ToLocal(a.CreatedAt)
    };

    private async Task<AbonoVendedorDto> MapearAbonoConDetallesAsync(AbonoVendedor a)
    {
        var usuarios = await _ventas.ObtenerNombresUsuarioAsync(
            new[] { a.UsuarioId }.Concat(a.Vendedor != null ? new[] { a.Vendedor.UsuarioId } : Array.Empty<int>()));
        var ventaIds = a.Aplicaciones.Select(x => x.VentaId).Distinct().ToList();
        var nros = ventaIds.Count == 0
            ? new Dictionary<int, int>()
            : await _ventaRepo.AsQueryable().AsNoTracking()
                .Where(v => ventaIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.NroVenta);
        return MapearAbono(a, usuarios, nros);
    }
}
