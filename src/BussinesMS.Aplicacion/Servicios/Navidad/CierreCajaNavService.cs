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

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Cierre de caja de tienda: resumen del turno (apertura → cierre de conteo) y registro de la entrega de efectivo.
// Turno = entre el conteo de apertura y el de cierre; los montos salen de ventas y cobros (CobroVenta.AlmacenId) de ese rango de horas (UTC).
public class CierreCajaNavService : ICierreCajaNavService
{
    private const int SistemaNavidadId = 2;

    private readonly ICierreCajaNavRepository _repo;
    private readonly IConteoInventarioNavRepository _conteoRepo;
    private readonly IVentaNavRepository _ventaRepo;
    private readonly ICobroVentaNavRepository _cobroRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ILogger<CierreCajaNavService> _logger;

    public CierreCajaNavService(
        ICierreCajaNavRepository repo,
        IConteoInventarioNavRepository conteoRepo,
        IVentaNavRepository ventaRepo,
        ICobroVentaNavRepository cobroRepo,
        IAlmacenRepository almacenRepo,
        IUsuarioRepository usuarioRepo,
        ITemporadaActualService temporadaActual,
        ICurrentUserService currentUser,
        IAlmacenAccesoNavService acceso,
        ILogger<CierreCajaNavService> logger)
    {
        _repo = repo;
        _conteoRepo = conteoRepo;
        _ventaRepo = ventaRepo;
        _cobroRepo = cobroRepo;
        _almacenRepo = almacenRepo;
        _usuarioRepo = usuarioRepo;
        _temporadaActual = temporadaActual;
        _currentUser = currentUser;
        _acceso = acceso;
        _logger = logger;
    }

    // ---------- Resumen ----------

    public async Task<CierreCajaResumenNavDto> ObtenerResumenAsync(int almacenId)
    {
        try
        {
            if (almacenId <= 0)
                throw new ValidacionException("La tienda es obligatoria");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await _acceso.VerificarAlmacenAsync(temporada.Id, almacenId);
            var nombre = await ValidarTiendaConCajaAsync(temporada.Id, almacenId);

            var turno = await ObtenerTurnoAsync(temporada.Id, almacenId, paraRegistrar: false);
            return await ArmarResumenAsync(temporada.Id, almacenId, nombre, turno);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener resumen de cierre de caja");
            throw;
        }
    }

    // Resumen (ventas y cobros) de un cierre ya registrado, recalculado con el rango guardado
    public async Task<CierreCajaResumenNavDto> ObtenerResumenPorIdAsync(int id)
    {
        try
        {
            var cierre = await _repo.AsQueryable().AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            if (cierre == null)
                throw new EntidadNoEncontradaException("Cierre de caja", id);

            await _acceso.VerificarAlmacenAsync(cierre.TemporadaId, cierre.AlmacenId);
            var nombre = await _almacenRepo.AsQueryable().AsNoTracking()
                .Where(a => a.Id == cierre.AlmacenId).Select(a => a.Nombre).FirstOrDefaultAsync() ?? string.Empty;

            var turno = new Turno
            {
                AperturaId = cierre.ConteoAperturaId,
                CierreId = cierre.ConteoCierreId,
                DesdeUtc = cierre.Desde,
                HastaUtc = cierre.Hasta
            };
            var resumen = await ArmarResumenAsync(cierre.TemporadaId, cierre.AlmacenId, nombre, turno);
            resumen.Pendiente = false; // ya registrado: no admite POST
            return resumen;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener resumen del cierre de caja {Id}", id);
            throw;
        }
    }

    // ---------- Registrar ----------

    public async Task<CierreCajaNavDto> RegistrarAsync(RegistrarCierreCajaNavDto dto)
    {
        try
        {
            if (dto.AlmacenId <= 0)
                throw new ValidacionException("La tienda es obligatoria");
            var entregado = VentaNavService.Redondear(dto.EfectivoEntregado);
            if (entregado < 0)
                throw new ValidacionException("El efectivo entregado no puede ser negativo");
            var observacion = VentaNavService.NormalizarTexto(dto.Observacion);
            if (observacion != null && observacion.Length > 500)
                throw new ValidacionException("La observación no puede superar 500 caracteres");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await _acceso.VerificarAlmacenAsync(temporada.Id, dto.AlmacenId);
            var nombre = await ValidarTiendaConCajaAsync(temporada.Id, dto.AlmacenId);

            var turno = await ObtenerTurnoAsync(temporada.Id, dto.AlmacenId, paraRegistrar: true);
            var resumen = await ArmarResumenAsync(temporada.Id, dto.AlmacenId, nombre, turno);

            var diferencia = entregado - resumen.EfectivoEsperado;
            if (diferencia != 0 && observacion == null)
                throw new ValidacionException(
                    $"Hay una diferencia de {diferencia} entre lo entregado y lo esperado ({resumen.EfectivoEsperado}): la observación es obligatoria");

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var entidad = new CierreCajaTienda
            {
                TemporadaId = temporada.Id,
                AlmacenId = dto.AlmacenId,
                ConteoAperturaId = turno.AperturaId,
                ConteoCierreId = turno.CierreId!.Value,
                Desde = turno.DesdeUtc,
                Hasta = turno.HastaUtc,
                TotalVendido = resumen.TotalVendido,
                EfectivoEsperado = resumen.EfectivoEsperado,
                Transferencias = resumen.Transferencias,
                Credito = resumen.Credito,
                EfectivoEntregado = entregado,
                Diferencia = diferencia,
                Observacion = observacion,
                UsuarioId = usuarioId
            };

            var creado = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Cierre de caja registrado: {Id} (tienda {AlmacenId}, esperado {Esperado}, entregado {Entregado})",
                creado.Id, dto.AlmacenId, resumen.EfectivoEsperado, entregado);

            var usuarios = await ObtenerNombresUsuarioAsync(new[] { creado.UsuarioId });
            var fecha = await _conteoRepo.AsQueryable().AsNoTracking()
                .Where(c => c.Id == creado.ConteoCierreId).Select(c => c.Fecha).FirstAsync();
            return Mapear(creado, nombre, usuarios, fecha);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar cierre de caja");
            throw;
        }
    }

    // ---------- Historial ----------

    public async Task<PagedResultDto<CierreCajaNavDto>> ObtenerHistorialAsync(CierreCajaNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(x => x.ConteoCierre)
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            // EncargadoAlmacen: solo los cierres de sus tiendas
            var aCargo = await _acceso.ObtenerRestriccionAsync(temporadaId);
            if (aCargo != null)
                baseQuery = baseQuery.Where(x => aCargo.Contains(x.AlmacenId));

            if (query.AlmacenId.HasValue)
                baseQuery = baseQuery.Where(x => x.AlmacenId == query.AlmacenId.Value);
            if (query.FechaDesde.HasValue)
            {
                var desde = query.FechaDesde.Value.Date;
                baseQuery = baseQuery.Where(x => x.ConteoCierre!.Fecha >= desde);
            }
            if (query.FechaHasta.HasValue)
            {
                var hastaExclusivo = query.FechaHasta.Value.Date.AddDays(1);
                baseQuery = baseQuery.Where(x => x.ConteoCierre!.Fecha < hastaExclusivo);
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Hasta).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var almacenes = await ObtenerNombresAlmacenAsync(entidades.Select(e => e.AlmacenId));
            var usuarios = await ObtenerNombresUsuarioAsync(entidades.Select(e => e.UsuarioId));

            return new PagedResultDto<CierreCajaNavDto>
            {
                Items = entidades.Select(e => Mapear(e, almacenes.TryGetValue(e.AlmacenId, out var a) ? a : string.Empty,
                    usuarios, e.ConteoCierre?.Fecha ?? e.CreatedAt.Date)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener historial de cierres de caja");
            throw;
        }
    }

    // ---------- Turno ----------

    private sealed class Turno
    {
        public int AperturaId { get; set; }
        public int? CierreId { get; set; }
        public DateTime DesdeUtc { get; set; }
        public DateTime HastaUtc { get; set; }
    }

    // Turno cerrado sin entrega (último conteo = Cierre sin caja) o, si paraRegistrar es false, el turno actual (último = Apertura)
    private async Task<Turno> ObtenerTurnoAsync(int temporadaId, int almacenId, bool paraRegistrar)
    {
        var ultimo = await _conteoRepo.AsQueryable()
            .AsNoTracking()
            .Where(c => c.IsActive && c.TemporadaId == temporadaId && c.AlmacenId == almacenId)
            .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
            .Select(c => new { c.Id, c.Fecha, c.Tipo, c.CreatedAt })
            .FirstOrDefaultAsync();

        if (ultimo == null)
            throw new ValidacionException("La tienda no tiene aperturas ni cierres en la temporada");

        if (ultimo.Tipo == TipoConteoInventario.Apertura)
        {
            if (paraRegistrar)
                throw new ExcepcionDominio("El turno sigue abierto: primero hacé el cierre de la tienda", 409, "TURNO_ABIERTO");
            return new Turno { AperturaId = ultimo.Id, DesdeUtc = ultimo.CreatedAt, HastaUtc = DateTime.UtcNow };
        }

        if (await _repo.AsQueryable().AsNoTracking().AnyAsync(c => c.IsActive && c.ConteoCierreId == ultimo.Id))
            throw new ExcepcionDominio("La entrega de caja del último cierre ya está registrada", 409, "CAJA_YA_REGISTRADA");

        // Apertura del turno: la inmediata anterior al cierre
        var apertura = await _conteoRepo.AsQueryable()
            .AsNoTracking()
            .Where(c => c.IsActive && c.TemporadaId == temporadaId && c.AlmacenId == almacenId &&
                        c.Tipo == TipoConteoInventario.Apertura &&
                        (c.Fecha < ultimo.Fecha || (c.Fecha == ultimo.Fecha && c.Id < ultimo.Id)))
            .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
            .Select(c => new { c.Id, c.CreatedAt })
            .FirstOrDefaultAsync();
        if (apertura == null)
            throw new ValidacionException("El cierre no tiene una apertura asociada: no se puede armar el turno");

        return new Turno { AperturaId = apertura.Id, CierreId = ultimo.Id, DesdeUtc = apertura.CreatedAt, HastaUtc = ultimo.CreatedAt };
    }

    private async Task<CierreCajaResumenNavDto> ArmarResumenAsync(int temporadaId, int almacenId, string almacenNombre, Turno turno)
    {
        var desde = turno.DesdeUtc;
        var hasta = turno.HastaUtc;

        // Ventas de tienda del turno (netas, sin anuladas)
        var ventas = await _ventaRepo.AsQueryable()
            .AsNoTracking()
            .Include(v => v.Vendedor)
            .Where(v => v.IsActive && !v.Anulada && v.TemporadaId == temporadaId && v.AlmacenId == almacenId &&
                        v.Canal == CanalVentaNav.Tienda && v.CreatedAt >= desde && v.CreatedAt <= hasta)
            .OrderBy(v => v.CreatedAt).ThenBy(v => v.Id)
            .ToListAsync();

        // Cobros recibidos en la tienda durante el turno (de cualquier venta), sin anulados
        var cobros = await _cobroRepo.AsQueryable()
            .AsNoTracking()
            .Include(c => c.Venta).ThenInclude(v => v!.Cliente)
            .Where(c => c.IsActive && !c.Anulado && c.AlmacenId == almacenId && c.Venta!.TemporadaId == temporadaId &&
                        !c.Venta.Anulada && c.CreatedAt >= desde && c.CreatedAt <= hasta)
            .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
            .ToListAsync();

        var usuarios = await ObtenerNombresUsuarioAsync(ventas.Where(v => v.Vendedor != null).Select(v => v.Vendedor!.UsuarioId));

        // Origen del cobro (solo informativo): reserva, venta directa creada en el turno, o cobro de deuda (venta directa anterior)
        string OrigenDe(CobroVenta c)
            => c.Venta!.Tipo == TipoVentaNav.Reserva ? "Reserva"
                : c.Venta.CreatedAt >= desde && c.Venta.CreatedAt <= hasta ? "VentaTurno" : "CobroDeuda";
        decimal Suma(MedioPagoNav medio, string origen)
            => cobros.Where(c => c.Medio == medio && OrigenDe(c) == origen).Sum(c => c.Monto);

        // Mismos cobros que suman en EfectivoVentasTurno/TransferenciasVentasTurno (origen "VentaTurno")
        var cobrosVentaTurno = cobros.Where(c => OrigenDe(c) == "VentaTurno").ToList();

        return new CierreCajaResumenNavDto
        {
            AlmacenId = almacenId,
            AlmacenNombre = almacenNombre,
            Pendiente = turno.CierreId.HasValue,
            ConteoAperturaId = turno.AperturaId,
            ConteoCierreId = turno.CierreId,
            Desde = BoliviaTimeZone.ToLocal(desde),
            Hasta = BoliviaTimeZone.ToLocal(hasta),
            TotalVendido = ventas.Sum(v => v.MontoNeto),
            EfectivoEsperado = cobros.Where(c => c.Medio == MedioPagoNav.Efectivo).Sum(c => c.Monto),
            Transferencias = cobros.Where(c => c.Medio == MedioPagoNav.Transferencia).Sum(c => c.Monto),
            Credito = ventas.Where(v => v.MontoSaldo > 0).Sum(v => v.MontoSaldo),
            EfectivoVentasTurno = Suma(MedioPagoNav.Efectivo, "VentaTurno"),
            EfectivoReservas = Suma(MedioPagoNav.Efectivo, "Reserva"),
            EfectivoCobrosDeuda = Suma(MedioPagoNav.Efectivo, "CobroDeuda"),
            TransferenciasVentasTurno = Suma(MedioPagoNav.Transferencia, "VentaTurno"),
            TransferenciasReservas = Suma(MedioPagoNav.Transferencia, "Reserva"),
            TransferenciasCobrosDeuda = Suma(MedioPagoNav.Transferencia, "CobroDeuda"),
            Ventas = ventas.Select(v => new CierreCajaVentaNavDto
            {
                Id = v.Id,
                NroVenta = v.NroVenta,
                Hora = BoliviaTimeZone.ToLocal(v.CreatedAt),
                VendedorNombre = v.Vendedor != null && usuarios.TryGetValue(v.Vendedor.UsuarioId, out var n) ? n : string.Empty,
                MontoNeto = v.MontoNeto,
                MontoCobrado = v.MontoCobrado,
                MontoSaldo = v.MontoSaldo,
                CondicionPago = (int)v.CondicionPago,
                CondicionPagoNombre = v.CondicionPago == CondicionPagoVentaNav.Credito ? "Crédito" : "Contado",
                VendedorId = v.VendedorId,
                Efectivo = cobrosVentaTurno.Where(c => c.VentaId == v.Id && c.Medio == MedioPagoNav.Efectivo).Sum(c => c.Monto),
                Transferencia = cobrosVentaTurno.Where(c => c.VentaId == v.Id && c.Medio == MedioPagoNav.Transferencia).Sum(c => c.Monto)
            }).ToList(),
            Cobros = cobros.Select(c => new CierreCajaCobroNavDto
            {
                Id = c.Id,
                VentaId = c.VentaId,
                NroVenta = c.Venta!.NroVenta,
                Hora = BoliviaTimeZone.ToLocal(c.CreatedAt),
                Monto = c.Monto,
                Medio = (int)c.Medio,
                MedioNombre = c.Medio == MedioPagoNav.Efectivo ? "Efectivo" : "Transferencia",
                Origen = OrigenDe(c),
                OrigenNombre = OrigenDe(c) switch
                {
                    "Reserva" => "Adelanto/cobro de reserva",
                    "VentaTurno" => "Venta del turno",
                    _ => "Cobro de deuda"
                },
                TipoVenta = c.Venta.Tipo == TipoVentaNav.Reserva ? "Reserva" : "Directa",
                ClienteNombre = c.Venta.Cliente?.Nombre
            }).ToList()
        };
    }

    // ---------- Validaciones y mapeo ----------

    // Tienda navideña activa con apertura y cierre obligatorios; devuelve su nombre
    private async Task<string> ValidarTiendaConCajaAsync(int temporadaId, int almacenId)
    {
        var almacen = await _almacenRepo.AsQueryable().AsNoTracking().FirstOrDefaultAsync(a => a.Id == almacenId);
        if (almacen == null || !almacen.IsActive || almacen.SistemaId != SistemaNavidadId)
            throw new ValidacionException($"El almacén {almacenId} no es un almacén navideño activo");
        if (!almacen.EsTienda)
            throw new ValidacionException($"El almacén {almacen.Nombre} no es una tienda");
        if (!await _temporadaActual.RequiereConteoAsync(temporadaId, almacenId))
            throw new ValidacionException($"La tienda {almacen.Nombre} no requiere apertura y cierre diario: no tiene cierre de caja");
        return almacen.Nombre;
    }

    private static CierreCajaNavDto Mapear(CierreCajaTienda e, string almacenNombre, Dictionary<int, string> usuarios, DateTime fecha) => new()
    {
        Id = e.Id,
        TemporadaId = e.TemporadaId,
        AlmacenId = e.AlmacenId,
        AlmacenNombre = almacenNombre,
        ConteoAperturaId = e.ConteoAperturaId,
        ConteoCierreId = e.ConteoCierreId,
        Fecha = fecha,
        Desde = BoliviaTimeZone.ToLocal(e.Desde),
        Hasta = BoliviaTimeZone.ToLocal(e.Hasta),
        TotalVendido = e.TotalVendido,
        EfectivoEsperado = e.EfectivoEsperado,
        Transferencias = e.Transferencias,
        Credito = e.Credito,
        EfectivoEntregado = e.EfectivoEntregado,
        Diferencia = e.Diferencia,
        Observacion = e.Observacion,
        UsuarioId = e.UsuarioId,
        UsuarioNombre = usuarios.TryGetValue(e.UsuarioId, out var u) ? u : string.Empty,
        CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt)
    };

    private async Task<Dictionary<int, string>> ObtenerNombresAlmacenAsync(IEnumerable<int> almacenIds)
    {
        var ids = almacenIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();
        return await _almacenRepo.AsQueryable()
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Nombre);
    }

    // AuthDB (solo lectura): "Nombre Apellido"; vacío si el usuario ya no existe
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
