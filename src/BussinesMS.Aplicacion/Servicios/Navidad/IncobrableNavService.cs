using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static BussinesMS.Aplicacion.Servicios.Navidad.VentaNavService;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Cierre de temporada: dar por incobrable el saldo de una venta de cliente o el saldo por rendir de un vendedor de ruta (solo admin).
// Descuenta de VentaNav.MontoSaldo vía MontoIncobrable; anular revierte. DTOs armados a mano; transacciones con INavidadUnitOfWork.
public class IncobrableNavService : IIncobrableNavService
{
    private const int MaxMotivo = 500;

    private readonly VentaNavService _ventas;
    private readonly IIncobrableNavRepository _repo;
    private readonly IVentaNavRepository _ventaRepo;
    private readonly IVendedorNavRepository _vendedorRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ILogger<IncobrableNavService> _logger;

    public IncobrableNavService(
        VentaNavService ventas,
        IIncobrableNavRepository repo,
        IVentaNavRepository ventaRepo,
        IVendedorNavRepository vendedorRepo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        IAlmacenAccesoNavService acceso,
        ILogger<IncobrableNavService> logger)
    {
        _ventas = ventas;
        _repo = repo;
        _ventaRepo = ventaRepo;
        _vendedorRepo = vendedorRepo;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _acceso = acceso;
        _logger = logger;
    }

    private async Task ExigirAdminAsync()
    {
        if (!await _acceso.EsAdminAsync())
            throw new ExcepcionDominio("Solo el admin puede dar deudas por incobrables", 403, "SIN_PERMISO");
    }

    private static string ValidarMotivo(string? motivo)
    {
        var m = motivo?.Trim();
        if (string.IsNullOrEmpty(m))
            throw new ValidacionException("El motivo es obligatorio");
        if (m.Length > MaxMotivo)
            throw new ValidacionException($"El motivo no puede superar {MaxMotivo} caracteres");
        return m;
    }

    // ================= Consulta =================

    public async Task<List<IncobrableNavDto>> ObtenerAsync(int? temporadaId, int? tipo, bool incluirAnulados)
    {
        try
        {
            await ExigirAdminAsync();
            var idTemporada = await _temporadaActual.ResolverTemporadaIdAsync(temporadaId);

            var query = ConsultaBase().Where(i => i.TemporadaId == idTemporada && i.IsActive);
            if (tipo.HasValue)
            {
                if (!Enum.IsDefined(typeof(TipoIncobrableNav), tipo.Value))
                    throw new ValidacionException("El tipo de incobrable no es válido");
                var t = (TipoIncobrableNav)tipo.Value;
                query = query.Where(i => i.Tipo == t);
            }
            if (!incluirAnulados)
                query = query.Where(i => !i.Anulado);

            var lista = await query.OrderByDescending(i => i.Fecha).ThenByDescending(i => i.Id).ToListAsync();
            return await ArmarDtosAsync(lista);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener incobrables");
            throw;
        }
    }

    private IQueryable<IncobrableNav> ConsultaBase() => _repo.AsQueryable()
        .AsNoTracking()
        .Include(i => i.Vendedor)
        .Include(i => i.Venta).ThenInclude(v => v!.Cliente)
        .Include(i => i.Venta).ThenInclude(v => v!.Vendedor)
        .Include(i => i.Aplicaciones).ThenInclude(a => a.Venta);

    private async Task<IncobrableNavDto> ObtenerDtoAsync(int id)
    {
        var entidad = await ConsultaBase().FirstOrDefaultAsync(i => i.Id == id)
            ?? throw new EntidadNoEncontradaException("Incobrable", id);
        return (await ArmarDtosAsync(new List<IncobrableNav> { entidad }))[0];
    }

    private async Task<List<IncobrableNavDto>> ArmarDtosAsync(List<IncobrableNav> lista)
    {
        var usuarioIds = lista.Select(i => i.UsuarioId)
            .Concat(lista.Where(i => i.UsuarioAnulacionId.HasValue).Select(i => i.UsuarioAnulacionId!.Value))
            .Concat(lista.Where(i => i.Vendedor != null).Select(i => i.Vendedor!.UsuarioId))
            .Concat(lista.Where(i => i.Venta?.Vendedor != null).Select(i => i.Venta!.Vendedor!.UsuarioId));
        var usuarios = await _ventas.ObtenerNombresUsuarioAsync(usuarioIds);
        string Nombre(int id) => usuarios.TryGetValue(id, out var n) ? n : string.Empty;

        return lista.Select(i =>
        {
            var esCliente = i.Tipo == TipoIncobrableNav.Cliente;
            var vendedor = esCliente ? i.Venta?.Vendedor : i.Vendedor;
            return new IncobrableNavDto
            {
                Id = i.Id,
                TemporadaId = i.TemporadaId,
                Tipo = (int)i.Tipo,
                TipoNombre = esCliente ? "Cliente" : "VendedorRuta",
                VentaId = esCliente ? i.VentaId : null,
                NroVenta = esCliente ? i.Venta?.NroVenta : null,
                ClienteId = esCliente ? i.Venta?.ClienteId : null,
                ClienteNombre = esCliente ? i.Venta?.Cliente?.Nombre : null,
                VendedorId = esCliente ? i.Venta?.VendedorId : i.VendedorId,
                VendedorNombre = vendedor != null ? Nombre(vendedor.UsuarioId) : null,
                Monto = i.Monto,
                Motivo = i.Motivo,
                Fecha = i.Fecha,
                UsuarioId = i.UsuarioId,
                UsuarioNombre = Nombre(i.UsuarioId),
                Anulado = i.Anulado,
                FechaAnulacion = i.FechaAnulacion.HasValue ? BoliviaTimeZone.ToLocal(i.FechaAnulacion.Value) : null,
                UsuarioAnulacionId = i.UsuarioAnulacionId,
                MotivoAnulacion = i.MotivoAnulacion,
                CreatedAt = BoliviaTimeZone.ToLocal(i.CreatedAt),
                Aplicaciones = i.Aplicaciones
                    .OrderBy(a => a.Venta?.NroVenta ?? 0).ThenBy(a => a.Id)
                    .Select(a => new IncobrableNavAplicacionDto { VentaId = a.VentaId, NroVenta = a.Venta?.NroVenta ?? 0, Monto = a.Monto })
                    .ToList()
            };
        }).ToList();
    }

    // ================= Cliente =================

    public async Task<IncobrableNavDto> RegistrarClienteAsync(RegistrarIncobrableClienteNavDto dto)
    {
        try
        {
            await ExigirAdminAsync();
            var motivo = ValidarMotivo(dto.Motivo);

            var venta = await _ventas.ObtenerVentaParaEdicionAsync(dto.VentaId);
            if (venta.Anulada)
                throw new ValidacionException("La venta está anulada");
            await _temporadaActual.VerificarEditableAsync(venta.TemporadaId);

            var elegible = venta.Canal == CanalVentaNav.Tienda
                && (venta.Tipo == TipoVentaNav.Directa
                    || (venta.Tipo == TipoVentaNav.Reserva && venta.EstadoEntrega == EstadoEntregaVentaNav.Entregado));
            if (!elegible)
                throw new ExcepcionDominio("Solo se puede dar por incobrable una venta de tienda a crédito o una reserva ya entregada", 400, "VENTA_NO_ELEGIBLE");
            if (venta.MontoSaldo <= 0)
                throw new ExcepcionDominio("La venta no tiene saldo por cobrar", 409, "VENTA_SIN_SALDO");

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;
            var monto = venta.MontoSaldo;

            var incobrable = new IncobrableNav
            {
                TemporadaId = venta.TemporadaId,
                Tipo = TipoIncobrableNav.Cliente,
                VentaId = venta.Id,
                VendedorId = null,
                Monto = monto,
                Motivo = motivo,
                Fecha = BoliviaTimeZone.Now().Date,
                UsuarioId = usuarioId,
                Anulado = false
            };
            incobrable.Aplicaciones.Add(new IncobrableNavAplicacion
            {
                VentaId = venta.Id,
                Monto = monto,
                CreatedByUsuarioId = usuarioId,
                CreatedAt = ahora
            });

            await _uow.BeginTransactionAsync();
            try
            {
                venta.MontoIncobrable += monto;
                RecalcularPago(venta);
                Tocar(venta, usuarioId, ahora);
                await _repo.CrearAsync(incobrable);
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Incobrable {Id} de cliente registrado: venta {VentaId}, monto {Monto}", incobrable.Id, venta.Id, monto);
            return await ObtenerDtoAsync(incobrable.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar incobrable de cliente");
            throw;
        }
    }

    // ================= Vendedor de ruta =================

    public async Task<IncobrableNavDto> RegistrarVendedorRutaAsync(RegistrarIncobrableVendedorRutaNavDto dto)
    {
        try
        {
            await ExigirAdminAsync();
            var motivo = ValidarMotivo(dto.Motivo);
            var monto = Redondear(dto.Monto);
            if (monto <= 0)
                throw new ValidacionException("El monto debe ser mayor a 0");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var vendedorOk = await _vendedorRepo.AsQueryable().AsNoTracking()
                .AnyAsync(v => v.Id == dto.VendedorId && v.TemporadaId == temporada.Id && v.IsActive && v.Tipo == TipoVendedor.Ruta);
            if (!vendedorOk)
                throw new ValidacionException("El vendedor no existe, está inactivo o no es de ruta");

            // Ventas de ruta con saldo, de la más antigua a la más nueva (con tracking: se modifican)
            var ventas = await _ventaRepo.AsQueryable()
                .Include(v => v.Cobros)
                .Where(v => v.IsActive && !v.Anulada && v.Canal == CanalVentaNav.Ruta && v.TemporadaId == temporada.Id
                    && v.VendedorId == dto.VendedorId && v.MontoSaldo > 0)
                .OrderBy(v => v.Fecha).ThenBy(v => v.Id)
                .ToListAsync();

            var saldo = ventas.Sum(v => v.MontoSaldo);
            if (saldo <= 0)
                throw new ExcepcionDominio("El vendedor no tiene saldo por rendir", 409, "VENDEDOR_SIN_SALDO");
            if (monto > saldo)
                throw new ValidacionException($"El monto ({monto}) supera el saldo por rendir ({saldo})");

            var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
            var ahora = DateTime.UtcNow;

            var incobrable = new IncobrableNav
            {
                TemporadaId = temporada.Id,
                Tipo = TipoIncobrableNav.VendedorRuta,
                VentaId = null,
                VendedorId = dto.VendedorId,
                Monto = monto,
                Motivo = motivo,
                Fecha = BoliviaTimeZone.Now().Date,
                UsuarioId = usuarioId,
                Anulado = false
            };

            await _uow.BeginTransactionAsync();
            try
            {
                var resto = monto;
                foreach (var venta in ventas)
                {
                    if (resto <= 0) break;
                    var aplica = Math.Min(resto, venta.MontoSaldo);
                    if (aplica <= 0) continue;

                    venta.MontoIncobrable += aplica;
                    RecalcularPago(venta);
                    Tocar(venta, usuarioId, ahora);
                    incobrable.Aplicaciones.Add(new IncobrableNavAplicacion
                    {
                        VentaId = venta.Id,
                        Monto = aplica,
                        CreatedByUsuarioId = usuarioId,
                        CreatedAt = ahora
                    });
                    resto -= aplica;
                }

                await _repo.CrearAsync(incobrable);
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Incobrable {Id} de vendedor de ruta {VendedorId} registrado, monto {Monto}", incobrable.Id, dto.VendedorId, monto);
            return await ObtenerDtoAsync(incobrable.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar incobrable de vendedor de ruta");
            throw;
        }
    }

    // ================= Anular =================

    public async Task<IncobrableNavDto> AnularAsync(int id, string? motivo)
    {
        try
        {
            await ExigirAdminAsync();
            var motivoAnulacion = motivo?.Trim();
            if (motivoAnulacion is { Length: > MaxMotivo })
                throw new ValidacionException($"El motivo no puede superar {MaxMotivo} caracteres");

            var incobrable = await _repo.ObtenerParaEdicionAsync(id);
            if (incobrable == null || !incobrable.IsActive)
                throw new EntidadNoEncontradaException("Incobrable", id);
            if (incobrable.Anulado)
                throw new ExcepcionDominio("El incobrable ya está anulado", 409, "INCOBRABLE_ANULADO");
            await _temporadaActual.VerificarEditableAsync(incobrable.TemporadaId);

            var ventaIds = incobrable.Aplicaciones.Select(a => a.VentaId).Distinct().ToList();
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
                    var aplicado = incobrable.Aplicaciones.Where(a => a.VentaId == venta.Id).Sum(a => a.Monto);
                    venta.MontoIncobrable = Math.Max(0, venta.MontoIncobrable - aplicado);
                    RecalcularPago(venta);
                    Tocar(venta, usuarioId, ahora);
                }
                incobrable.Anulado = true;
                incobrable.FechaAnulacion = ahora;
                incobrable.UsuarioAnulacionId = usuarioId;
                incobrable.MotivoAnulacion = string.IsNullOrEmpty(motivoAnulacion) ? null : motivoAnulacion;
                Tocar(incobrable, usuarioId, ahora);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Incobrable {Id} anulado", id);
            return await ObtenerDtoAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular incobrable {Id}", id);
            throw;
        }
    }
}
