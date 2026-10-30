using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static BussinesMS.Aplicacion.Servicios.Navidad.VentaNavService;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Registro PERSONAL y PRIVADO de ventas del vendedor de ruta a sus clientes (cuaderno digital).
// No crea VentaNav/CobroVenta, no toca stock, deuda del vendedor, comisiones ni reportes.
// Privacidad: TODO endpoint exige ser vendedor de ruta activo de la temporada; el admin recibe 403 siempre
// y cada vendedor solo ve/opera lo suyo (registro ajeno = 403). Transacciones con INavidadUnitOfWork.
public class VentaClienteRutaNavService : IVentaClienteRutaNavService
{
    private readonly VentaNavService _ventas;
    private readonly IVentaClienteRutaNavRepository _repo;
    private readonly ISalidaRutaNavRepository _salidaRepo;
    private readonly ISalidaRutaCargaNavRepository _cargaRepo;
    private readonly IDevolucionRutaNavRepository _devolucionRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ILogger<VentaClienteRutaNavService> _logger;

    public VentaClienteRutaNavService(
        VentaNavService ventas,
        IVentaClienteRutaNavRepository repo,
        ISalidaRutaNavRepository salidaRepo,
        ISalidaRutaCargaNavRepository cargaRepo,
        IDevolucionRutaNavRepository devolucionRepo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        IAlmacenAccesoNavService acceso,
        ILogger<VentaClienteRutaNavService> logger)
    {
        _ventas = ventas;
        _repo = repo;
        _salidaRepo = salidaRepo;
        _cargaRepo = cargaRepo;
        _devolucionRepo = devolucionRepo;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _acceso = acceso;
        _logger = logger;
    }

    // ================= Permisos =================

    private static ExcepcionDominio SinPermiso(string mensaje = "Solo el vendedor de ruta puede ver y gestionar este registro")
        => new(mensaje, 403, "SIN_PERMISO");

    // Vendedor de ruta activo de la temporada vinculado al usuario; admin (aunque sea vendedor) o cualquier otro => 403
    private async Task<Vendedor> ObtenerVendedorAsync(int temporadaId)
    {
        if (await _acceso.EsAdminAsync()) throw SinPermiso();
        var (_, vendedor) = await _ventas.ObtenerPermisosAsync(temporadaId);
        if (vendedor == null || !vendedor.IsActive || vendedor.Tipo != TipoVendedor.Ruta) throw SinPermiso();
        return vendedor;
    }

    // ================= Consultas =================

    public async Task<PagedResultDto<VentaClienteRutaListaDto>> ObtenerAsync(MisVentasRutaFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);
            var vendedor = await ObtenerVendedorAsync(temporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Where(v => v.IsActive && v.TemporadaId == temporadaId && v.VendedorId == vendedor.Id);

            if (!query.IncluirAnuladas || query.SoloConSaldo)
                baseQuery = baseQuery.Where(v => !v.Anulada);
            if (query.ClienteId.HasValue)
                baseQuery = baseQuery.Where(v => v.ClienteId == query.ClienteId.Value);
            if (query.SoloConSaldo)
                baseQuery = baseQuery.Where(v => v.Saldo > 0);
            if (query.Desde.HasValue)
            {
                var desde = query.Desde.Value.Date;
                baseQuery = baseQuery.Where(v => v.Fecha >= desde);
            }
            if (query.Hasta.HasValue)
            {
                var hastaExclusivo = query.Hasta.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(v => v.Fecha < hastaExclusivo);
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var ids = entidades.Select(e => e.Id).ToList();
            var lineas = ids.Count == 0
                ? new Dictionary<int, int>()
                : (await _repo.AsQueryable().AsNoTracking()
                        .Where(v => ids.Contains(v.Id))
                        .Select(v => new { v.Id, Cantidad = v.Detalles.Count })
                        .ToListAsync())
                    .ToDictionary(x => x.Id, x => x.Cantidad);

            return new PagedResultDto<VentaClienteRutaListaDto>
            {
                Items = entidades.Select(e => new VentaClienteRutaListaDto
                {
                    Id = e.Id,
                    Fecha = e.Fecha,
                    ClienteId = e.ClienteId,
                    ClienteNombre = e.Cliente?.Nombre ?? string.Empty,
                    SalidaRutaId = e.SalidaRutaId,
                    Total = e.Total,
                    Pagado = e.Pagado,
                    Saldo = e.Saldo,
                    Anulada = e.Anulada,
                    CantidadLineas = lineas.GetValueOrDefault(e.Id)
                }).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener mis ventas a clientes");
            throw;
        }
    }

    public async Task<VentaClienteRutaDto> ObtenerPorIdAsync(int id)
    {
        try
        {
            var venta = await _repo.ObtenerConDetallesAsync(id);
            if (venta == null || !venta.IsActive)
                throw new EntidadNoEncontradaException("Venta a cliente", id);
            await ExigirDuenoAsync(venta);
            return Mapear(venta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener la venta a cliente {Id}", id);
            throw;
        }
    }

    // Verifica que el actor sea el vendedor dueño del registro (si no, 403)
    private async Task<Vendedor> ExigirDuenoAsync(VentaClienteRuta venta)
    {
        var vendedor = await ObtenerVendedorAsync(venta.TemporadaId);
        if (venta.VendedorId != vendedor.Id)
            throw SinPermiso("Solo puede ver sus propios registros");
        return vendedor;
    }

    public async Task<MisVentasMeDebenDto> ObtenerMeDebenAsync(int? temporadaId)
    {
        try
        {
            var tempId = await _temporadaActual.ResolverTemporadaIdAsync(temporadaId);
            var vendedor = await ObtenerVendedorAsync(tempId);

            var ventas = await _repo.AsQueryable()
                .AsNoTracking()
                .Include(v => v.Cliente)
                .Where(v => v.IsActive && !v.Anulada && v.Saldo > 0 && v.TemporadaId == tempId && v.VendedorId == vendedor.Id)
                .OrderBy(v => v.Fecha).ThenBy(v => v.Id)
                .ToListAsync();

            var clientes = ventas
                .GroupBy(v => v.ClienteId)
                .Select(g => new MisVentasMeDebenClienteDto
                {
                    ClienteId = g.Key,
                    ClienteNombre = g.First().Cliente?.Nombre ?? string.Empty,
                    SaldoTotal = g.Sum(v => v.Saldo),
                    VentasPendientes = g.Count(),
                    Ventas = g.Select(v => new MisVentasMeDebenVentaDto
                    {
                        Id = v.Id,
                        Fecha = v.Fecha,
                        Total = v.Total,
                        Pagado = v.Pagado,
                        Saldo = v.Saldo
                    }).ToList()
                })
                .OrderByDescending(c => c.SaldoTotal)
                .ThenBy(c => c.ClienteNombre, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return new MisVentasMeDebenDto { TotalGeneral = clientes.Sum(c => c.SaldoTotal), Clientes = clientes };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el resumen me-deben");
            throw;
        }
    }

    // Ayuda del cierre: solo el vendedor dueño de la salida (el admin recibe 403, por ende nunca ve el dato)
    public async Task<MisVentasTotalSalidaDto> ObtenerTotalRegistradoSalidaAsync(int salidaId)
    {
        try
        {
            var salida = await _salidaRepo.AsQueryable().AsNoTracking().FirstOrDefaultAsync(s => s.Id == salidaId);
            if (salida == null || !salida.IsActive)
                throw new EntidadNoEncontradaException("Salida de ruta", salidaId);

            var vendedor = await ObtenerVendedorAsync(salida.TemporadaId);
            if (salida.VendedorId != vendedor.Id)
                throw SinPermiso("Solo puede consultar sus propias salidas");

            var totales = await _repo.AsQueryable().AsNoTracking()
                .Where(v => v.IsActive && !v.Anulada && v.SalidaRutaId == salidaId && v.VendedorId == vendedor.Id)
                .Select(v => v.Total)
                .ToListAsync();

            return new MisVentasTotalSalidaDto
            {
                SalidaRutaId = salidaId,
                TotalRegistrado = totales.Sum(),
                CantidadVentas = totales.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el total registrado de la salida {Id}", salidaId);
            throw;
        }
    }

    // ================= Registrar =================

    public async Task<VentaClienteRutaDto> RegistrarAsync(RegistrarVentaClienteRutaDto dto)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var vendedor = await ObtenerVendedorAsync(temporada.Id);

            ValidarEstructura(dto);
            await _ventas.ValidarClienteAsync(dto.ClienteId);

            var productos = await _ventas.CargarProductosAsync(temporada.Id, dto.Detalles.Select(d => d.ProductoId));
            foreach (var l in dto.Detalles)
                if (!productos[l.ProductoId].IsActive)
                    throw new ValidacionException($"El producto {NombreMostrar(productos[l.ProductoId])} está inactivo");

            var salida = await ResolverSalidaAsync(temporada.Id, vendedor.Id, dto.SalidaRutaId);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;
            var fecha = (dto.Fecha ?? BoliviaTimeZone.Now()).Date;

            var venta = new VentaClienteRuta
            {
                TemporadaId = temporada.Id,
                VendedorId = vendedor.Id,
                ClienteId = dto.ClienteId,
                SalidaRutaId = salida?.Id,
                Fecha = fecha,
                Observacion = NormalizarTexto(dto.Observacion),
                Anulada = false,
                CreatedByUsuarioId = usuarioId,
                CreatedAt = ahora
            };
            foreach (var l in dto.Detalles)
                venta.Detalles.Add(new VentaClienteRutaDetalle
                {
                    ProductoId = l.ProductoId,
                    Unidades = l.Unidades,
                    PrecioUnitario = l.PrecioUnitario,
                    Subtotal = Redondear(l.Unidades * l.PrecioUnitario),
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });
            venta.Total = Redondear(venta.Detalles.Sum(d => d.Subtotal));

            var pagado = Redondear(dto.PagadoAhora);
            if (pagado > venta.Total)
                throw new ValidacionException("Lo pagado no puede superar el total de la venta");
            venta.Pagado = pagado;
            venta.Saldo = venta.Total - pagado;
            if (pagado > 0)
                venta.Cobros.Add(new CobroClienteRuta
                {
                    Fecha = fecha,
                    Monto = pagado,
                    Medio = dto.Medio,
                    Anulado = false,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });

            await _uow.BeginTransactionAsync();
            try
            {
                await _repo.CrearAsync(venta);
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            var resultado = await ObtenerPorIdAsync(venta.Id);
            resultado.Avisos = salida != null && salida.Estado == EstadoSalidaRuta.Abierta
                ? await CalcularAvisosAsync(salida.Id, vendedor.Id, dto.Detalles.Select(d => d.ProductoId).ToList(), productos)
                : new List<string>();

            _logger.LogInformation("Venta a cliente de ruta registrada: {Id}, vendedor {VendedorId}", venta.Id, vendedor.Id);
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar venta a cliente de ruta");
            throw;
        }
    }

    private static void ValidarEstructuraComun(int clienteId, string? observacion, List<VentaClienteRutaLineaDto> detalles)
    {
        if (clienteId <= 0)
            throw new ValidacionException("El cliente es obligatorio");
        if (observacion != null && observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        if (detalles == null || detalles.Count == 0)
            throw new ValidacionException("Debe registrar al menos un producto");
        if (detalles.Any(d => d.ProductoId <= 0))
            throw new ValidacionException("El producto es obligatorio");
        if (detalles.Any(d => d.Unidades <= 0))
            throw new ValidacionException("Las unidades deben ser mayores a 0");
        if (detalles.Any(d => d.PrecioUnitario < 0))
            throw new ValidacionException("El precio no puede ser negativo");
        if (detalles.Select(d => d.ProductoId).Distinct().Count() != detalles.Count)
            throw new ValidacionException("Hay productos repetidos");
    }

    private static void ValidarEstructura(RegistrarVentaClienteRutaDto dto)
    {
        ValidarEstructuraComun(dto.ClienteId, dto.Observacion, dto.Detalles);
        if (dto.PagadoAhora < 0)
            throw new ValidacionException("Lo pagado no puede ser negativo");
        if (dto.PagadoAhora > 0 && !dto.Medio.HasValue)
            throw new ValidacionException("El medio de pago es obligatorio si hay un pago");
        if (dto.Medio.HasValue && !Enum.IsDefined(dto.Medio.Value))
            throw new ValidacionException("El medio de pago es inválido");
    }

    // Salida indicada (debe ser suya) o, si se omite, su salida Abierta (si no hay, null)
    private async Task<SalidaRuta?> ResolverSalidaAsync(int temporadaId, int vendedorId, int? salidaRutaId)
    {
        if (salidaRutaId.HasValue && salidaRutaId.Value > 0)
        {
            var salida = await _salidaRepo.AsQueryable().AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == salidaRutaId.Value && s.IsActive);
            if (salida == null)
                throw new ValidacionException("La salida indicada no existe");
            if (salida.VendedorId != vendedorId)
                throw SinPermiso("La salida indicada no es suya");
            if (salida.TemporadaId != temporadaId)
                throw new ValidacionException("La salida indicada no es de la temporada");
            if (salida.Estado == EstadoSalidaRuta.Anulada)
                throw new ValidacionException("La salida indicada está anulada");
            return salida;
        }

        return await _salidaRepo.AsQueryable().AsNoTracking()
            .FirstOrDefaultAsync(s => s.IsActive && s.TemporadaId == temporadaId && s.VendedorId == vendedorId && s.Estado == EstadoSalidaRuta.Abierta);
    }

    // Avisos (nunca bloquean): por producto, Σ unidades de sus ventas no anuladas de la salida > cargado - devuelto vigentes
    private async Task<List<string>> CalcularAvisosAsync(int salidaId, int vendedorId, IReadOnlyCollection<int> productoIds,
        Dictionary<int, Producto> productos)
    {
        var cargado = (await _cargaRepo.AsQueryable().AsNoTracking()
                .Where(c => c.SalidaRutaId == salidaId && !c.Anulada)
                .SelectMany(c => c.Detalles)
                .Where(d => productoIds.Contains(d.ProductoId))
                .Select(d => new { d.ProductoId, d.Unidades })
                .ToListAsync())
            .GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Unidades));
        var devuelto = (await _devolucionRepo.AsQueryable().AsNoTracking()
                .Where(d => d.SalidaRutaId == salidaId && !d.Anulada)
                .SelectMany(d => d.Detalles)
                .Where(x => productoIds.Contains(x.ProductoId))
                .Select(x => new { x.ProductoId, x.Unidades })
                .ToListAsync())
            .GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Unidades));
        var registrado = (await _repo.AsQueryable().AsNoTracking()
                .Where(v => v.IsActive && !v.Anulada && v.SalidaRutaId == salidaId && v.VendedorId == vendedorId)
                .SelectMany(v => v.Detalles)
                .Where(d => productoIds.Contains(d.ProductoId))
                .Select(d => new { d.ProductoId, d.Unidades })
                .ToListAsync())
            .GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Unidades));

        var avisos = new List<string>();
        foreach (var productoId in productoIds)
        {
            var enPoder = cargado.GetValueOrDefault(productoId) - devuelto.GetValueOrDefault(productoId);
            var vendidas = registrado.GetValueOrDefault(productoId);
            if (vendidas > enPoder)
                avisos.Add($"{NombreMostrar(productos[productoId])}: registraste {vendidas} unidades en esta salida y solo tenés {Math.Max(enPoder, 0)} en poder");
        }
        return avisos;
    }

    // ================= Editar =================

    public async Task<VentaClienteRutaDto> EditarAsync(int id, EditarVentaClienteRutaDto dto)
    {
        try
        {
            ValidarEstructuraComun(dto.ClienteId, dto.Observacion, dto.Detalles);

            var venta = await ObtenerParaEdicionAsync(id);
            if (venta.Anulada)
                throw new ExcepcionDominio("La venta está anulada", 409, "VENTA_ANULADA");

            await _ventas.ValidarClienteAsync(dto.ClienteId);

            var productos = await _ventas.CargarProductosAsync(venta.TemporadaId, dto.Detalles.Select(d => d.ProductoId));
            var existentes = venta.Detalles.ToDictionary(d => d.ProductoId);

            // Un producto inactivo solo se tolera si la línea ya existía (activa) y no sube de cantidad
            foreach (var l in dto.Detalles)
            {
                var p = productos[l.ProductoId];
                if (p.IsActive) continue;
                if (!existentes.TryGetValue(l.ProductoId, out var previa) || !previa.IsActive || l.Unidades > previa.Unidades)
                    throw new ValidacionException($"El producto {NombreMostrar(p)} está inactivo");
            }

            var total = Redondear(dto.Detalles.Sum(l => Redondear(l.Unidades * l.PrecioUnitario)));
            var cobrosVigentes = venta.Cobros.Where(c => c.IsActive && !c.Anulado).ToList();
            var cobrado = Redondear(cobrosVigentes.Sum(c => c.Monto));
            decimal? pagadoNuevo = null;
            if (dto.PagadoAhora.HasValue)
            {
                pagadoNuevo = Redondear(dto.PagadoAhora.Value);
                if (pagadoNuevo < 0)
                    throw new ValidacionException("Lo pagado no puede ser negativo");
                if (pagadoNuevo > total)
                    throw new ValidacionException("Lo pagado no puede superar el total de la venta");
                if (pagadoNuevo > 0 && !dto.Medio.HasValue)
                    throw new ValidacionException("El medio de pago es obligatorio si hay un pago");
                if (dto.Medio.HasValue && !Enum.IsDefined(dto.Medio.Value))
                    throw new ValidacionException("El medio de pago es inválido");
                if (cobrosVigentes.Count > 1)
                    throw new ValidacionException("La venta tiene varios cobros: corregilos desde el detalle (anular o registrar cobro)");
                cobrado = pagadoNuevo.Value;
            }
            if (total < cobrado)
                throw new ValidacionException($"El total no puede ser menor a lo ya cobrado (Bs {cobrado:0.00})");

            var nuevas = dto.Detalles.ToDictionary(l => l.ProductoId);
            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                venta.ClienteId = dto.ClienteId;
                venta.Cliente = null;
                if (dto.Fecha.HasValue) venta.Fecha = dto.Fecha.Value.Date;
                venta.Observacion = NormalizarTexto(dto.Observacion);

                // Las líneas que ya no vienen se desactivan (quedan en 0); si reaparece el producto se reactiva
                foreach (var d in venta.Detalles)
                {
                    if (nuevas.TryGetValue(d.ProductoId, out var l))
                    {
                        d.IsActive = true;
                        d.Unidades = l.Unidades;
                        d.PrecioUnitario = l.PrecioUnitario;
                        d.Subtotal = Redondear(l.Unidades * l.PrecioUnitario);
                    }
                    else
                    {
                        d.IsActive = false;
                        d.Unidades = 0;
                        d.Subtotal = 0;
                    }
                    Tocar(d, usuarioId, ahora);
                }
                foreach (var l in nuevas.Values.Where(l => !existentes.ContainsKey(l.ProductoId)))
                    venta.Detalles.Add(new VentaClienteRutaDetalle
                    {
                        ProductoId = l.ProductoId,
                        Unidades = l.Unidades,
                        PrecioUnitario = l.PrecioUnitario,
                        Subtotal = Redondear(l.Unidades * l.PrecioUnitario),
                        CreatedByUsuarioId = usuarioId,
                        CreatedAt = ahora
                    });

                // Corrección del pago inicial: los cobros no se editan in situ; se anula el vigente y se crea uno nuevo con la misma fecha
                if (pagadoNuevo.HasValue)
                {
                    var actual = cobrosVigentes.FirstOrDefault();
                    if (actual == null || actual.Monto != pagadoNuevo.Value || actual.Medio != dto.Medio)
                    {
                        if (actual != null)
                        {
                            actual.Anulado = true;
                            Tocar(actual, usuarioId, ahora);
                        }
                        if (pagadoNuevo.Value > 0)
                            venta.Cobros.Add(new CobroClienteRuta
                            {
                                Fecha = actual?.Fecha ?? (dto.Fecha ?? venta.Fecha).Date,
                                Monto = pagadoNuevo.Value,
                                Medio = dto.Medio,
                                Anulado = false,
                                CreatedByUsuarioId = usuarioId,
                                CreatedAt = ahora
                            });
                    }
                }

                venta.Total = total;
                Recalcular(venta);
                Tocar(venta, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Venta a cliente de ruta editada: {Id}", id);
            return await ObtenerPorIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al editar la venta a cliente {Id}", id);
            throw;
        }
    }

    // ================= Cobros y anulaciones =================

    public async Task<VentaClienteRutaDto> RegistrarCobroAsync(int id, RegistrarCobroClienteRutaDto dto)
    {
        try
        {
            if (dto.Monto <= 0)
                throw new ValidacionException("El monto debe ser mayor a 0");
            if (!Enum.IsDefined(dto.Medio))
                throw new ValidacionException("El medio de pago es inválido");

            var venta = await ObtenerParaEdicionAsync(id);
            if (venta.Anulada)
                throw new ExcepcionDominio("La venta está anulada", 409, "VENTA_ANULADA");

            var monto = Redondear(dto.Monto);
            if (monto > venta.Saldo)
                throw new ValidacionException($"El monto no puede superar el saldo pendiente ({venta.Saldo:0.00})");

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                venta.Cobros.Add(new CobroClienteRuta
                {
                    Fecha = (dto.Fecha ?? BoliviaTimeZone.Now()).Date,
                    Monto = monto,
                    Medio = dto.Medio,
                    Anulado = false,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });
                Recalcular(venta);
                Tocar(venta, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            return await ObtenerPorIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar cobro de la venta a cliente {Id}", id);
            throw;
        }
    }

    public async Task<VentaClienteRutaDto> AnularCobroAsync(int id, int cobroId)
    {
        try
        {
            var venta = await ObtenerParaEdicionAsync(id);
            if (venta.Anulada)
                throw new ExcepcionDominio("La venta está anulada", 409, "VENTA_ANULADA");

            var cobro = venta.Cobros.FirstOrDefault(c => c.Id == cobroId && c.IsActive)
                ?? throw new EntidadNoEncontradaException("Cobro", cobroId);
            if (cobro.Anulado)
                throw new ValidacionException("El cobro ya está anulado");

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                cobro.Anulado = true;
                Tocar(cobro, usuarioId, ahora);
                Recalcular(venta);
                Tocar(venta, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            return await ObtenerPorIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular el cobro {CobroId} de la venta a cliente {Id}", cobroId, id);
            throw;
        }
    }

    public async Task<VentaClienteRutaDto> AnularAsync(int id)
    {
        try
        {
            var venta = await ObtenerParaEdicionAsync(id);
            if (venta.Anulada)
                throw new ValidacionException("La venta ya está anulada");

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var cobro in venta.Cobros.Where(c => !c.Anulado))
                {
                    cobro.Anulado = true;
                    Tocar(cobro, usuarioId, ahora);
                }
                venta.Anulada = true;
                venta.Pagado = 0;
                venta.Saldo = 0;
                Tocar(venta, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            return await ObtenerPorIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular la venta a cliente {Id}", id);
            throw;
        }
    }

    // CON tracking; exige ser el dueño y temporada editable
    private async Task<VentaClienteRuta> ObtenerParaEdicionAsync(int id)
    {
        var venta = await _repo.ObtenerParaEdicionAsync(id);
        if (venta == null || !venta.IsActive)
            throw new EntidadNoEncontradaException("Venta a cliente", id);
        await ExigirDuenoAsync(venta);
        await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);
        return venta;
    }

    // Pagado = Σ cobros vigentes; Saldo = Total − Pagado
    private static void Recalcular(VentaClienteRuta venta)
    {
        venta.Pagado = Redondear(venta.Cobros.Where(c => !c.Anulado).Sum(c => c.Monto));
        venta.Saldo = venta.Total - venta.Pagado;
    }

    // ================= Mapeo =================

    private static VentaClienteRutaDto Mapear(VentaClienteRuta v) => new()
    {
        Id = v.Id,
        TemporadaId = v.TemporadaId,
        Fecha = v.Fecha,
        ClienteId = v.ClienteId,
        ClienteNombre = v.Cliente?.Nombre ?? string.Empty,
        SalidaRutaId = v.SalidaRutaId,
        Total = v.Total,
        Pagado = v.Pagado,
        Saldo = v.Saldo,
        Anulada = v.Anulada,
        Observacion = v.Observacion,
        Detalles = v.Detalles.Where(d => d.IsActive).OrderBy(d => d.Id).Select(d => new VentaClienteRutaLineaDetalleDto
        {
            ProductoId = d.ProductoId,
            ProductoNombreMostrar = d.Producto != null ? NombreMostrar(d.Producto) : string.Empty,
            UnidadesPorEmpaque = d.Producto?.UnidadesPorEmpaque,
            NombreEmpaque = d.Producto?.NombreEmpaque,
            Color = d.Producto?.Color,
            Unidades = d.Unidades,
            PrecioUnitario = d.PrecioUnitario,
            Subtotal = d.Subtotal
        }).ToList(),
        Cobros = v.Cobros.Where(c => c.IsActive).OrderBy(c => c.Fecha).ThenBy(c => c.Id).Select(c => new VentaClienteRutaCobroDto
        {
            Id = c.Id,
            Fecha = c.Fecha,
            Monto = c.Monto,
            Medio = c.Medio.HasValue ? (int)c.Medio.Value : null,
            MedioNombre = c.Medio?.ToString(),
            Anulado = c.Anulado,
            CreatedAt = BoliviaTimeZone.ToLocal(c.CreatedAt)
        }).ToList()
    };
}
