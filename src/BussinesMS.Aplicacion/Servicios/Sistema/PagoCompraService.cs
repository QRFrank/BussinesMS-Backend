using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class PagoCompraService : IPagoCompraService
{
    private const string OrigenCaja = "caja";
    private const string OrigenExterno = "externo";
    private const string OrigenMixto = "mixto";

    private readonly IPagoCompraRepository _repo;
    private readonly ICompraRepository _compraRepo;
    private readonly ISesionCajaRepository _sesionRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly ISistemaUnitOfWork _uow;
    private readonly ILogger<PagoCompraService> _logger;

    public PagoCompraService(
        IPagoCompraRepository repo,
        ICompraRepository compraRepo,
        ISesionCajaRepository sesionRepo,
        IUsuarioRepository usuarioRepo,
        ICurrentUserService currentUser,
        ISistemaUnitOfWork uow,
        ILogger<PagoCompraService> logger)
    {
        _repo = repo;
        _compraRepo = compraRepo;
        _sesionRepo = sesionRepo;
        _usuarioRepo = usuarioRepo;
        _currentUser = currentUser;
        _uow = uow;
        _logger = logger;
    }

    public async Task<PagedResultDto<PagoCompraListDto>> ObtenerTodosAsync(PagoCompraFiltroDto query)
    {
        try
        {
            var activo = query.IsActive ?? true;
            var baseQuery = _repo.AsQueryable().Where(x => x.IsActive == activo);

            if (query.CompraId.HasValue)
                baseQuery = baseQuery.Where(x => x.CompraId == query.CompraId.Value);

            if (query.ProveedorId.HasValue)
                baseQuery = baseQuery.Where(x => x.Compra!.ProveedorId == query.ProveedorId.Value);

            if (query.SesionCajaId.HasValue)
                baseQuery = baseQuery.Where(x => x.SesionCajaId == query.SesionCajaId.Value);

            if (!string.IsNullOrWhiteSpace(query.Origen))
            {
                var origen = query.Origen.Trim().ToLower();
                if (origen == OrigenCaja)
                    baseQuery = baseQuery.Where(x => x.MontoCaja > 0 && x.MontoExterno == 0);
                else if (origen == OrigenExterno)
                    baseQuery = baseQuery.Where(x => x.MontoCaja == 0);
                else if (origen == OrigenMixto)
                    baseQuery = baseQuery.Where(x => x.MontoCaja > 0 && x.MontoExterno > 0);
            }

            if (query.PagadoPorUsuarioId.HasValue)
                baseQuery = baseQuery.Where(x => x.PagadoPorUsuarioId == query.PagadoPorUsuarioId.Value);

            if (query.FechaDesde.HasValue)
            {
                var (inicioUtc, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaDesde.Value));
                baseQuery = baseQuery.Where(x => x.FechaPago >= inicioUtc);
            }

            if (query.FechaHasta.HasValue)
            {
                var (_, finUtc) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaHasta.Value));
                baseQuery = baseQuery.Where(x => x.FechaPago < finUtc);
            }

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.Observacion != null && x.Observacion.ToLower().Contains(f)) ||
                    (x.Compra != null && x.Compra.NumeroFactura != null && x.Compra.NumeroFactura.ToLower().Contains(f)) ||
                    (x.Compra != null && x.Compra.Proveedor != null && x.Compra.Proveedor.Nombre.ToLower().Contains(f)));
            }

            // Orden por defecto: FechaPago desc, luego Id desc (si el cliente no manda SortBy)
            var sinOrdenCliente = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrdenCliente)
                baseQuery = baseQuery.OrderByDescending(x => x.FechaPago).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrdenCliente);
            var filas = await Proyectar(filteredQuery).ToListAsync();

            var nombres = await ObtenerNombresUsuariosAsync(filas.Select(f => f.PagadoPorUsuarioId));

            return new PagedResultDto<PagoCompraListDto>
            {
                Items = filas.Select(f => CompletarDto(f, nombres)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pagos de compra");
            throw;
        }
    }

    public async Task<PagoCompraListDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            // Devuelve también pagos anulados (IsActive=false) para poder ver su detalle
            return await ObtenerDtoAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pago de compra {Id}", id);
            throw;
        }
    }

    public async Task<PagoCompraListDto> CrearAsync(CrearPagoCompraDto dto)
    {
        try
        {
            var compra = await _compraRepo.ObtenerPorIdAsync(dto.CompraId);
            if (compra == null || !compra.IsActive)
                throw new EntidadNoEncontradaException("Compra", dto.CompraId);

            if (compra.EstaLiquidada)
                throw new ValidacionException("La compra ya está liquidada");

            ValidarMontos(dto.MontoCaja, dto.MontoExterno);
            var monto = dto.MontoCaja + dto.MontoExterno;

            if (dto.MontoCaja == 0 && dto.SesionCajaId.HasValue)
                throw new ValidacionException("Si no se paga con caja no debe indicarse sesión de caja");

            if (dto.MontoCaja > 0)
            {
                if (!dto.SesionCajaId.HasValue)
                    throw new ValidacionException("Para pagar con caja se requiere una sesión de caja");

                var sesion = await ObtenerSesionValidaAsync(dto.SesionCajaId.Value, compra.AlmacenId);
                await ValidarEfectivoDisponibleAsync(sesion, dto.MontoCaja, montoCajaPropio: 0);
            }

            var totalPagadoActivo = await SumarPagosActivosAsync(compra.Id);
            if (totalPagadoActivo + monto > compra.TotalCompra)
                throw new ValidacionException("El monto excede el saldo pendiente de la compra");

            var pago = new PagoCompra
            {
                CompraId = compra.Id,
                Monto = monto,
                MontoCaja = dto.MontoCaja,
                MontoExterno = dto.MontoExterno,
                FechaPago = DateTime.UtcNow,
                SesionCajaId = dto.SesionCajaId,
                PagadoPorUsuarioId = _currentUser.GetUsuarioId() ?? 1,
                Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim()
            };

            await _uow.BeginTransactionAsync();
            try
            {
                await _repo.CrearSinGuardarAsync(pago);
                await _uow.SaveChangesAsync();

                await RecalcularEstadoCompraAsync(compra.Id);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Pago registrado para compra {CompraId}: {Monto}", compra.Id, pago.Monto);

            return (await ObtenerDtoAsync(pago.Id))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar pago de compra {CompraId}", dto.CompraId);
            throw;
        }
    }

    public async Task<PagoCompraListDto> ActualizarAsync(ActualizarPagoCompraDto dto)
    {
        try
        {
            var pago = await _repo.ObtenerPorIdAsync(dto.Id);
            if (pago == null)
                throw new EntidadNoEncontradaException("Pago de compra", dto.Id);

            if (!pago.IsActive)
                throw new ValidacionException("El pago está anulado");

            await VerificarCajaAbiertaAsync(pago, "No se puede modificar un pago de una caja cerrada");

            ValidarMontos(dto.MontoCaja, dto.MontoExterno);
            var monto = dto.MontoCaja + dto.MontoExterno;

            var compra = await _compraRepo.ObtenerPorIdAsync(pago.CompraId);
            if (compra == null)
                throw new EntidadNoEncontradaException("Compra", pago.CompraId);

            if (dto.MontoCaja > 0)
            {
                // SesionCajaId no se cambia en el PUT
                if (!pago.SesionCajaId.HasValue)
                    throw new ValidacionException("Un registro externo no puede tener monto de caja");

                var sesion = await ObtenerSesionValidaAsync(pago.SesionCajaId.Value, compra.AlmacenId);
                // El pago está activo y en esta misma sesión: su MontoCaja actual se devuelve al disponible
                await ValidarEfectivoDisponibleAsync(sesion, dto.MontoCaja, montoCajaPropio: pago.MontoCaja);
            }

            // Se permite aunque la compra esté liquidada: bajar el monto la devuelve a parcial
            var otrosPagosActivos = await SumarPagosActivosAsync(pago.CompraId, excluirPagoId: pago.Id);
            if (otrosPagosActivos + monto > compra.TotalCompra)
                throw new ValidacionException("El monto excede el saldo pendiente de la compra");

            await _uow.BeginTransactionAsync();
            try
            {
                pago.Monto = monto;
                pago.MontoCaja = dto.MontoCaja;
                pago.MontoExterno = dto.MontoExterno;
                pago.Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim();
                await _repo.ActualizarAsync(pago);

                await RecalcularEstadoCompraAsync(pago.CompraId);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Pago de compra actualizado: {Id} ({Monto})", pago.Id, pago.Monto);

            return (await ObtenerDtoAsync(pago.Id))!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar pago de compra {Id}", dto.Id);
            throw;
        }
    }

    public async Task AnularAsync(int id)
    {
        try
        {
            var pago = await _repo.ObtenerPorIdAsync(id);
            if (pago == null)
                throw new EntidadNoEncontradaException("Pago de compra", id);

            if (!pago.IsActive)
                throw new ValidacionException("El pago ya está anulado");

            await VerificarCajaAbiertaAsync(pago, "No se puede anular un pago de una caja cerrada");

            await _uow.BeginTransactionAsync();
            try
            {
                // Soft delete (IsActive=false, DeletedAt, DeletedByUsuarioId). Su SaveChanges
                // corre sobre el mismo DbContext, dentro de la transacción abierta por _uow.
                await _repo.EliminarAsync(id);

                await RecalcularEstadoCompraAsync(pago.CompraId);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Pago de compra anulado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular pago de compra {Id}", id);
            throw;
        }
    }

    // Recalcula EstaLiquidada/EstadoPago de la compra a partir de los pagos ACTIVOS ya guardados
    // (se llama después de persistir el cambio del pago, así el pago editado se cuenta una sola vez):
    //   totalPagado >= TotalCompra      → liquidada, Contado
    //   0 < totalPagado < TotalCompra   → no liquidada, ParcialmentePagado
    //   totalPagado == 0                → no liquidada, Credito. Queda como deuda sin pagos, aunque la
    //                                     compra se haya registrado originalmente al contado.
    private async Task RecalcularEstadoCompraAsync(int compraId)
    {
        var compra = await _compraRepo.ObtenerPorIdAsync(compraId)
            ?? throw new EntidadNoEncontradaException("Compra", compraId);

        var totalPagado = await SumarPagosActivosAsync(compraId);

        if (totalPagado >= compra.TotalCompra)
        {
            compra.EstaLiquidada = true;
            compra.EstadoPago = EstadoPago.Contado;
        }
        else if (totalPagado > 0)
        {
            compra.EstaLiquidada = false;
            compra.EstadoPago = EstadoPago.ParcialmentePagado;
        }
        else
        {
            compra.EstaLiquidada = false;
            compra.EstadoPago = EstadoPago.Credito;
        }

        await _compraRepo.ActualizarAsync(compra);
    }

    private async Task<decimal> SumarPagosActivosAsync(int compraId, int? excluirPagoId = null)
    {
        var q = _repo.AsQueryable().Where(p => p.CompraId == compraId && p.IsActive);
        if (excluirPagoId.HasValue)
            q = q.Where(p => p.Id != excluirPagoId.Value);
        return await q.SumAsync(p => p.Monto);
    }

    private static void ValidarMontos(decimal montoCaja, decimal montoExterno)
    {
        if (montoCaja < 0 || montoExterno < 0)
            throw new ValidacionException("Los montos no pueden ser negativos");
        if (montoCaja + montoExterno <= 0)
            throw new ValidacionException("El monto debe ser mayor a 0");
    }

    private async Task<SesionCaja> ObtenerSesionValidaAsync(int sesionCajaId, int almacenCompraId)
    {
        var sesion = await _sesionRepo.ObtenerPorIdAsync(sesionCajaId);
        if (sesion == null || !sesion.IsActive)
            throw new EntidadNoEncontradaException("SesionCaja", sesionCajaId);

        if (sesion.Estado != EstadoSesionCaja.Abierta)
            throw new ValidacionException("La sesión de caja no está abierta");

        if (sesion.AlmacenId != almacenCompraId)
            throw new ValidacionException("La sesión de caja no pertenece al almacén de la compra");

        return sesion;
    }

    // disponible = MontoInicial + IngresosEfectivo − egresos de caja (gastos + pagos activos, MontoCaja)
    // + montoCajaPropio (MontoCaja actual del registro que se edita, para excluirlo del cálculo)
    private async Task ValidarEfectivoDisponibleAsync(SesionCaja sesion, decimal montoCaja, decimal montoCajaPropio)
    {
        var egresos = await _sesionRepo.CalcularEgresosAsync([sesion.Id]);
        var (egresosGastos, egresosPagoProveedor) = egresos.GetValueOrDefault(sesion.Id);
        var disponible = sesion.MontoInicial + sesion.IngresosEfectivo - egresosGastos - egresosPagoProveedor + montoCajaPropio;
        if (montoCaja > disponible)
            throw new ValidacionException($"En caja solo hay Bs {disponible:0.00}");
    }

    private async Task VerificarCajaAbiertaAsync(PagoCompra pago, string mensaje)
    {
        if (!pago.SesionCajaId.HasValue) return; // pago externo: siempre editable

        var sesion = await _sesionRepo.ObtenerPorIdAsync(pago.SesionCajaId.Value);
        if (sesion == null || sesion.Estado != EstadoSesionCaja.Abierta)
            throw new ValidacionException(mensaje);
    }

    private async Task<PagoCompraListDto?> ObtenerDtoAsync(int id)
    {
        var fila = await Proyectar(_repo.AsQueryable().Where(x => x.Id == id)).FirstOrDefaultAsync();
        if (fila == null) return null;

        var nombres = await ObtenerNombresUsuariosAsync(new[] { fila.PagadoPorUsuarioId });
        return CompletarDto(fila, nombres);
    }

    // Proyección en EF: saldo pendiente como subquery sobre Compra.Pagos (sin N+1).
    // FechaPago/CreatedAt quedan en UTC acá y se convierten a Bolivia en CompletarDto.
    private static IQueryable<PagoCompraListDto> Proyectar(IQueryable<PagoCompra> query)
        => query.Select(x => new PagoCompraListDto
        {
            Id = x.Id,
            CompraId = x.CompraId,
            CompraNumeroFactura = x.Compra!.NumeroFactura,
            ProveedorId = x.Compra.ProveedorId,
            ProveedorNombre = x.Compra.Proveedor!.Nombre,
            TotalCompra = x.Compra.TotalCompra,
            SaldoPendiente = x.Compra.TotalCompra - x.Compra.Pagos.Where(p => p.IsActive).Sum(p => p.Monto),
            Monto = x.Monto,
            MontoCaja = x.MontoCaja,
            MontoExterno = x.MontoExterno,
            FechaPago = x.FechaPago,
            SesionCajaId = x.SesionCajaId,
            Origen = x.MontoCaja > 0 && x.MontoExterno > 0 ? OrigenMixto
                : x.MontoCaja > 0 ? OrigenCaja
                : OrigenExterno,
            PagadoPorUsuarioId = x.PagadoPorUsuarioId,
            Observacion = x.Observacion,
            IsActive = x.IsActive,
            CreatedAt = x.CreatedAt
        });

    private static PagoCompraListDto CompletarDto(PagoCompraListDto dto, IReadOnlyDictionary<int, string> nombres)
    {
        dto.FechaPago = BoliviaTimeZone.ToLocal(dto.FechaPago);
        dto.CreatedAt = BoliviaTimeZone.ToLocal(dto.CreatedAt);
        dto.UsuarioNombre = nombres.TryGetValue(dto.PagadoPorUsuarioId, out var nombre) ? nombre : null;
        return dto;
    }

    // Una sola consulta a la BD Auth para todos los usuarios de la página (sin N+1)
    private async Task<Dictionary<int, string>> ObtenerNombresUsuariosAsync(IEnumerable<int> usuarioIds)
    {
        var ids = usuarioIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();

        var usuarios = await _usuarioRepo.AsQueryable()
            .Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Nombre, u.Apellido })
            .ToListAsync();

        return usuarios.ToDictionary(u => u.Id, u => $"{u.Nombre} {u.Apellido}".Trim());
    }
}
