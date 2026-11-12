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
    private readonly INotificadorNav _notificador;
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
        INotificadorNav notificador,
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
        _notificador = notificador;
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
            var abiertas = await SalidasAbiertasAsync(entidades.Where(e => e.SalidaRutaId.HasValue).Select(e => e.SalidaRutaId!.Value));
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
                    SalidaAbierta = e.SalidaRutaId.HasValue && abiertas.Contains(e.SalidaRutaId.Value),
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
            var dtoVenta = Mapear(venta);
            dtoVenta.SalidaAbierta = venta.SalidaRutaId.HasValue && (await SalidasAbiertasAsync(new[] { venta.SalidaRutaId.Value })).Count > 0;
            return dtoVenta;
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
            // Comprobante por WhatsApp: el número se valida antes de crear nada (la regla de celular de Bolivia la valida el front)
            string? telefonoComprobante = dto.EnviarComprobante
                ? VentaNavService.NormalizarTelefonoComprobante(dto.TelefonoComprobante)
                : null;
            await _ventas.ValidarClienteAsync(dto.ClienteId);

            var productos = await _ventas.CargarProductosAsync(temporada.Id, dto.Detalles.Select(d => d.ProductoId));
            foreach (var l in dto.Detalles)
                if (!productos[l.ProductoId].IsActive)
                    throw new ValidacionException($"El producto {NombreMostrar(productos[l.ProductoId])} está inactivo");

            // SinSalida = venta fuera de ruta; si no, la salida indicada (debe estar abierta) o la abierta del vendedor
            var salida = dto.SinSalida ? null : await ResolverSalidaAsync(temporada.Id, vendedor.Id, dto.SalidaRutaId);
            // Una venta en ruta no puede superar lo que el vendedor tiene en poder
            if (salida != null)
                await VerificarEnPoderAsync(salida.Id, vendedor.Id, dto.Detalles, productos, null);

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
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

            // Pagos (mixto) reemplaza a PagadoAhora/Medio; sin Pagos rige el formato viejo (un solo cobro)
            var pagosVenta = dto.Pagos is { Count: > 0 }
                ? VentaNavService.NormalizarPagos(dto.Pagos).Pagos
                : (Redondear(dto.PagadoAhora) > 0
                    ? new List<PagoMedioDto> { new() { Medio = dto.Medio!.Value, Monto = Redondear(dto.PagadoAhora) } }
                    : new List<PagoMedioDto>());
            var pagado = pagosVenta.Sum(p => p.Monto);
            if (pagado > venta.Total)
                throw new ValidacionException("Lo pagado no puede superar el total de la venta");
            venta.Pagado = pagado;
            venta.Saldo = venta.Total - pagado;
            foreach (var p in pagosVenta)
                venta.Cobros.Add(new CobroClienteRuta
                {
                    Fecha = fecha,
                    Monto = p.Monto,
                    Medio = p.Medio,
                    Anulado = false,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });

            bool? comprobanteEncolado = null;
            await _uow.BeginTransactionAsync();
            try
            {
                await _repo.CrearAsync(venta);
                // El comprobante queda en la bandeja dentro de la misma transacción; si WhatsApp falla después, no afecta a la venta
                if (telefonoComprobante != null)
                    comprobanteEncolado = await EncolarComprobanteSeguroAsync(venta, telefonoComprobante, $"comprobante-misventas:{venta.Id}");
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            var resultado = await ObtenerPorIdAsync(venta.Id);
            resultado.ComprobanteEncolado = comprobanteEncolado;
            resultado.Avisos = new List<string>();

            _logger.LogInformation("Venta a cliente de ruta registrada: {Id}, vendedor {VendedorId}", venta.Id, vendedor.Id);
            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar venta a cliente de ruta");
            throw;
        }
    }

    // ================= Comprobante por WhatsApp (C7) =================

    private Task<NotificacionNav?> EncolarComprobanteAsync(VentaClienteRuta venta, string telefono, string clave)
        => _notificador.EncolarAsync(new NuevaNotificacionNav
        {
            TemporadaId = venta.TemporadaId,
            Evento = EventoNotificacionNav.ComprobanteMisVentas,
            Prioridad = 1,
            Mensaje = MensajesAvisosNav.CaptionComprobante(venta.Id, venta.Total),
            Audiencia = AudienciaNotificacionNav.TelefonoDirecto,
            TelefonoDirecto = telefono,
            AdjuntoTipo = MensajesAvisosNav.AdjuntoComprobanteMisVentasPdf,
            ReferenciaId = venta.Id,
            ClaveUnica = clave
        });

    // En la venta nueva: nunca lanza (un error de WhatsApp no puede romper la venta)
    private async Task<bool> EncolarComprobanteSeguroAsync(VentaClienteRuta venta, string telefono, string clave)
    {
        try
        {
            return await EncolarComprobanteAsync(venta, telefono, clave) != null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo encolar el comprobante de la venta de ruta {Id}", venta.Id);
            return false;
        }
    }

    // Reenviar el comprobante: mismo permiso que ver el detalle (solo el vendedor dueño); siempre encola uno nuevo
    public async Task<ComprobanteEncoladoDto> ReenviarComprobanteAsync(int id, string? telefono)
    {
        try
        {
            var normalizado = VentaNavService.NormalizarTelefonoComprobante(telefono);

            var venta = await _repo.ObtenerConDetallesAsync(id);
            if (venta == null || !venta.IsActive)
                throw new EntidadNoEncontradaException("Venta a cliente", id);
            await ExigirDuenoAsync(venta);
            if (venta.Anulada)
                throw new ExcepcionDominio("La venta está anulada: no se puede enviar el comprobante", 409, "VENTA_ANULADA");

            NotificacionNav? notificacion;
            await _uow.BeginTransactionAsync();
            try
            {
                notificacion = await EncolarComprobanteAsync(venta, normalizado, $"comprobante-misventas:{venta.Id}:reenvio:{Guid.NewGuid():N}");
                if (notificacion == null)
                    throw new ExcepcionDominio("No se pudo encolar el comprobante: WhatsApp no está configurado o el aviso está desactivado", 409, "WHATSAPP_NO_CONFIGURADO");
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            return new ComprobanteEncoladoDto
            {
                NotificacionId = notificacion.Id,
                DestinoEnmascarado = EnmascaradorTelefono.Enmascarar(normalizado)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al reenviar comprobante de la venta de ruta {Id}", id);
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
        if (dto.Pagos is { Count: > 0 })
        {
            VentaNavService.NormalizarPagos(dto.Pagos);
            return;
        }
        if (dto.PagadoAhora < 0)
            throw new ValidacionException("Lo pagado no puede ser negativo");
        if (dto.PagadoAhora > 0 && !dto.Medio.HasValue)
            throw new ValidacionException("El medio de pago es obligatorio si hay un pago");
        if (dto.Medio.HasValue && !Enum.IsDefined(dto.Medio.Value))
            throw new ValidacionException("El medio de pago es inválido");
    }

    // Salida indicada (debe ser suya y estar abierta) o, si se omite, su salida Abierta (si no hay, null)
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
            if (salida.Estado != EstadoSalidaRuta.Abierta)
                throw new ValidacionException("La salida indicada no está abierta");
            return salida;
        }

        return await _salidaRepo.AsQueryable().AsNoTracking()
            .FirstOrDefaultAsync(s => s.IsActive && s.TemporadaId == temporadaId && s.VendedorId == vendedorId && s.Estado == EstadoSalidaRuta.Abierta);
    }

    // Cargado / devuelto / registrado por producto de una salida (registrado = ventas vigentes del vendedor en la salida, sin contar excluirVentaId).
    // productoIds null = todos los productos
    private async Task<(Dictionary<int, int> Cargado, Dictionary<int, int> Devuelto, Dictionary<int, int> Registrado)> ObtenerEnPoderAsync(
        int salidaId, int vendedorId, IReadOnlyCollection<int>? productoIds, int? excluirVentaId)
    {
        var cargado = (await _cargaRepo.AsQueryable().AsNoTracking()
                .Where(c => c.SalidaRutaId == salidaId && !c.Anulada)
                .SelectMany(c => c.Detalles)
                .Where(d => productoIds == null || productoIds.Contains(d.ProductoId))
                .Select(d => new { d.ProductoId, d.Unidades })
                .ToListAsync())
            .GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Unidades));
        var devuelto = (await _devolucionRepo.AsQueryable().AsNoTracking()
                .Where(d => d.SalidaRutaId == salidaId && !d.Anulada)
                .SelectMany(d => d.Detalles)
                .Where(x => productoIds == null || productoIds.Contains(x.ProductoId))
                .Select(x => new { x.ProductoId, x.Unidades })
                .ToListAsync())
            .GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Unidades));
        var registrado = await RegistradoPorProductoAsync(_repo.AsQueryable(), salidaId, vendedorId, productoIds, excluirVentaId);
        return (cargado, devuelto, registrado);
    }

    // Hermano de RegistradoPorProductoAsync (mismo criterio de ventas): Σ Subtotal de las líneas por producto. Lo usa el detalle de la salida
    internal static async Task<Dictionary<int, decimal>> SubtotalRegistradoPorProductoAsync(
        IQueryable<VentaClienteRuta> ventas, int salidaId, int vendedorId)
        => (await ventas.AsNoTracking()
                .Where(v => v.IsActive && !v.Anulada && v.SalidaRutaId == salidaId && v.VendedorId == vendedorId)
                .SelectMany(v => v.Detalles)
                .Select(d => new { d.ProductoId, d.Subtotal })
                .ToListAsync())
            .GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Subtotal));

    // Unidades por producto en las ventas vigentes de "Mis ventas" de la salida. Fuente única: la usan el resumen de Mis ventas y el detalle de la salida (RutaNavService)
    internal static async Task<Dictionary<int, int>> RegistradoPorProductoAsync(
        IQueryable<VentaClienteRuta> ventas, int salidaId, int vendedorId, IReadOnlyCollection<int>? productoIds, int? excluirVentaId)
        => (await ventas.AsNoTracking()
                .Where(v => v.IsActive && !v.Anulada && v.SalidaRutaId == salidaId && v.VendedorId == vendedorId
                    && (!excluirVentaId.HasValue || v.Id != excluirVentaId.Value))
                .SelectMany(v => v.Detalles)
                .Where(d => productoIds == null || productoIds.Contains(d.ProductoId))
                .Select(d => new { d.ProductoId, d.Unidades })
                .ToListAsync())
            .GroupBy(x => x.ProductoId).ToDictionary(g => g.Key, g => g.Sum(x => x.Unidades));

    // Bloqueo (salida abierta): por producto, registrado en las otras ventas de la salida + unidades de esta venta <= cargado - devuelto
    private async Task VerificarEnPoderAsync(int salidaId, int vendedorId, IReadOnlyCollection<VentaClienteRutaLineaDto> detalles,
        Dictionary<int, Producto> productos, int? excluirVentaId)
    {
        var ids = detalles.Select(d => d.ProductoId).ToList();
        var (cargado, devuelto, registrado) = await ObtenerEnPoderAsync(salidaId, vendedorId, ids, excluirVentaId);
        var errores = new List<string>();
        foreach (var l in detalles)
        {
            var enPoder = cargado.GetValueOrDefault(l.ProductoId) - devuelto.GetValueOrDefault(l.ProductoId);
            var vendidas = registrado.GetValueOrDefault(l.ProductoId) + l.Unidades;
            if (vendidas > enPoder)
                errores.Add($"{NombreMostrar(productos[l.ProductoId])}: registraste {vendidas} unidades en esta salida y solo tenés {Math.Max(enPoder, 0)} en poder");
        }
        if (errores.Count > 0)
            throw new ValidacionException(string.Join("; ", errores));
    }

    // Salidas (de las indicadas) que siguen Abiertas
    private async Task<HashSet<int>> SalidasAbiertasAsync(IEnumerable<int> salidaIds)
    {
        var ids = salidaIds.Distinct().ToList();
        if (ids.Count == 0) return new HashSet<int>();
        return (await _salidaRepo.AsQueryable().AsNoTracking()
                .Where(s => ids.Contains(s.Id) && s.Estado == EstadoSalidaRuta.Abierta)
                .Select(s => s.Id)
                .ToListAsync())
            .ToHashSet();
    }

    // Ayuda de la pantalla: cargado/devuelto/en poder/registrado por producto de la salida (la abierta del vendedor si no se indica). Null si no hay
    public async Task<MisVentasSalidaResumenDto?> ObtenerResumenSalidaAsync(int? salidaId, int? excluirVentaId)
    {
        try
        {
            SalidaRuta? salida;
            Vendedor vendedor;
            if (salidaId.HasValue && salidaId.Value > 0)
            {
                salida = await _salidaRepo.AsQueryable().AsNoTracking().FirstOrDefaultAsync(s => s.Id == salidaId.Value);
                if (salida == null || !salida.IsActive)
                    throw new EntidadNoEncontradaException("Salida de ruta", salidaId.Value);
                vendedor = await ObtenerVendedorAsync(salida.TemporadaId);
                if (salida.VendedorId != vendedor.Id)
                    throw SinPermiso("Solo puede consultar sus propias salidas");
            }
            else
            {
                var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(null);
                vendedor = await ObtenerVendedorAsync(temporadaId);
                salida = await ResolverSalidaAsync(temporadaId, vendedor.Id, null);
                if (salida == null) return null;
            }

            var (cargado, devuelto, registrado) = await ObtenerEnPoderAsync(salida.Id, vendedor.Id, null, excluirVentaId);
            var productos = cargado.Count == 0
                ? new Dictionary<int, Producto>()
                : await _ventas.CargarProductosAsync(salida.TemporadaId, cargado.Keys);
            var totalRegistrado = (await _repo.AsQueryable().AsNoTracking()
                    .Where(v => v.IsActive && !v.Anulada && v.SalidaRutaId == salida.Id && v.VendedorId == vendedor.Id
                        && (!excluirVentaId.HasValue || v.Id != excluirVentaId.Value))
                    .Select(v => v.Total)
                    .ToListAsync())
                .Sum();

            return new MisVentasSalidaResumenDto
            {
                SalidaRutaId = salida.Id,
                FechaApertura = salida.FechaApertura,
                Estado = (int)salida.Estado,
                EstadoNombre = salida.Estado.ToString(),
                TotalRegistrado = totalRegistrado,
                Productos = cargado.Keys
                    .Select(pid =>
                    {
                        var p = productos[pid];
                        var enPoder = cargado.GetValueOrDefault(pid) - devuelto.GetValueOrDefault(pid);
                        var reg = registrado.GetValueOrDefault(pid);
                        return new MisVentasSalidaProductoDto
                        {
                            ProductoId = pid,
                            ProductoNombreMostrar = NombreMostrar(p),
                            Color = p.Color,
                            UnidadesPorEmpaque = p.UnidadesPorEmpaque,
                            NombreEmpaque = p.NombreEmpaque,
                            PrecioCatalogo = p.PrecioCatalogo,
                            Cargado = cargado.GetValueOrDefault(pid),
                            Devuelto = devuelto.GetValueOrDefault(pid),
                            EnPoder = enPoder,
                            Registrado = reg,
                            Disponible = Math.Max(0, enPoder - reg)
                        };
                    })
                    .OrderBy(x => x.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el resumen de la salida");
            throw;
        }
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

            // Vínculo con la salida: solo cambia si dto.SinSalida pide un estado distinto del actual
            var nuevaSalidaId = venta.SalidaRutaId;
            SalidaRuta? salidaActual = venta.SalidaRutaId.HasValue
                ? await _salidaRepo.AsQueryable().AsNoTracking().FirstOrDefaultAsync(s => s.Id == venta.SalidaRutaId.Value)
                : null;
            if (dto.SinSalida.HasValue && dto.SinSalida.Value == venta.SalidaRutaId.HasValue)
            {
                if (salidaActual != null && salidaActual.Estado != EstadoSalidaRuta.Abierta)
                    throw new ExcepcionDominio("La salida de la venta ya no está abierta: no se puede cambiar el vínculo", 409, "SALIDA_CERRADA");
                if (dto.SinSalida.Value)
                {
                    nuevaSalidaId = null;
                    salidaActual = null;
                }
                else
                {
                    var abierta = await ResolverSalidaAsync(venta.TemporadaId, venta.VendedorId, null)
                        ?? throw new ValidacionException("No tenés una salida abierta para vincular la venta");
                    nuevaSalidaId = abierta.Id;
                    salidaActual = abierta;
                }
            }
            // Una venta en ruta (salida abierta) no puede superar lo que el vendedor tiene en poder
            if (nuevaSalidaId.HasValue && salidaActual != null && salidaActual.Estado == EstadoSalidaRuta.Abierta)
                await VerificarEnPoderAsync(nuevaSalidaId.Value, venta.VendedorId, dto.Detalles, productos, venta.Id);

            var total = Redondear(dto.Detalles.Sum(l => Redondear(l.Unidades * l.PrecioUnitario)));
            var cobrosVigentes = venta.Cobros.Where(c => c.IsActive && !c.Anulado).ToList();
            var cobrado = Redondear(cobrosVigentes.Sum(c => c.Monto));
            decimal? pagadoNuevo = null;
            List<PagoMedioDto>? pagosNuevos = null;
            if (dto.Pagos is { Count: > 0 })
            {
                // Pago mixto: reemplaza los cobros vigentes (máx. uno por medio entre los vigentes)
                pagosNuevos = VentaNavService.NormalizarPagos(dto.Pagos).Pagos;
                pagadoNuevo = pagosNuevos.Sum(p => p.Monto);
                if (pagadoNuevo > total)
                    throw new ValidacionException("Lo pagado no puede superar el total de la venta");
                if (cobrosVigentes.GroupBy(c => c.Medio).Any(g => g.Count() > 1))
                    throw new ValidacionException("La venta tiene varios cobros del mismo medio: corregilos desde el detalle (anular o registrar cobro)");
                cobrado = pagadoNuevo.Value;
            }
            else if (dto.PagadoAhora.HasValue)
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
            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                venta.ClienteId = dto.ClienteId;
                venta.Cliente = null;
                if (venta.SalidaRutaId != nuevaSalidaId)
                {
                    venta.SalidaRutaId = nuevaSalidaId;
                    venta.SalidaRuta = null;
                }
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
                    var nuevosPagos = pagosNuevos ?? (pagadoNuevo.Value > 0
                        ? new List<PagoMedioDto> { new() { Medio = dto.Medio!.Value, Monto = pagadoNuevo.Value } }
                        : new List<PagoMedioDto>());
                    var cambio = pagosNuevos != null
                        ? cobrosVigentes.Count != nuevosPagos.Count
                            || nuevosPagos.Any(n => !cobrosVigentes.Any(c => c.Medio == n.Medio && c.Monto == n.Monto))
                        : (actual == null || actual.Monto != pagadoNuevo.Value || actual.Medio != dto.Medio);
                    if (cambio)
                    {
                        foreach (var c in cobrosVigentes)
                        {
                            c.Anulado = true;
                            Tocar(c, usuarioId, ahora);
                        }
                        foreach (var n in nuevosPagos)
                            venta.Cobros.Add(new CobroClienteRuta
                            {
                                Fecha = actual?.Fecha ?? (dto.Fecha ?? venta.Fecha).Date,
                                Monto = n.Monto,
                                Medio = n.Medio,
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
            // Pagos (mixto) reemplaza a monto/medio; sin Pagos rige el formato viejo (un solo cobro)
            List<PagoMedioDto> pagosCobro;
            if (dto.Pagos is { Count: > 0 })
                pagosCobro = VentaNavService.NormalizarPagos(dto.Pagos).Pagos;
            else
            {
                if (dto.Monto <= 0)
                    throw new ValidacionException("El monto debe ser mayor a 0");
                if (!Enum.IsDefined(dto.Medio))
                    throw new ValidacionException("El medio de pago es inválido");
                pagosCobro = new List<PagoMedioDto> { new() { Medio = dto.Medio, Monto = Redondear(dto.Monto) } };
            }

            var venta = await ObtenerParaEdicionAsync(id);
            if (venta.Anulada)
                throw new ExcepcionDominio("La venta está anulada", 409, "VENTA_ANULADA");

            var monto = pagosCobro.Sum(p => p.Monto);
            if (monto > venta.Saldo)
                throw new ValidacionException($"El monto no puede superar el saldo pendiente ({venta.Saldo:0.00})");

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var p in pagosCobro)
                    venta.Cobros.Add(new CobroClienteRuta
                    {
                        Fecha = (dto.Fecha ?? BoliviaTimeZone.Now()).Date,
                        Monto = p.Monto,
                        Medio = p.Medio,
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

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
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

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
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
