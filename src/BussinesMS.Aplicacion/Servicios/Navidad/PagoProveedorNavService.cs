using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProveedorNav = BussinesMS.Dominio.Entidades.Navidad.Proveedor;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// DTOs armados a mano (CreatedAt → hora de Bolivia aquí). Anulado = IsActive false.
public class PagoProveedorNavService : IPagoProveedorNavService
{
    private readonly IPagoProveedorNavRepository _repo;
    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly ICodigoClienteRepository _codigoRepo;
    private readonly ICompraNavRepository _compraRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly ILogger<PagoProveedorNavService> _logger;

    public PagoProveedorNavService(
        IPagoProveedorNavRepository repo,
        IProveedorNavRepository proveedorRepo,
        ICodigoClienteRepository codigoRepo,
        ICompraNavRepository compraRepo,
        ITemporadaActualService temporadaActual,
        ILogger<PagoProveedorNavService> logger)
    {
        _repo = repo;
        _proveedorRepo = proveedorRepo;
        _codigoRepo = codigoRepo;
        _compraRepo = compraRepo;
        _temporadaActual = temporadaActual;
        _logger = logger;
    }

    public async Task<PagedResultDto<PagoProveedorNavDto>> ObtenerTodosAsync(PagoProveedorNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            // null/true → activos; false → solo anulados
            var activos = query.IsActive ?? true;

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(x => x.Proveedor)
                .Include(x => x.CodigoCliente)
                .Where(x => x.TemporadaId == temporadaId && x.IsActive == activos);

            if (query.ProveedorId.HasValue)
                baseQuery = baseQuery.Where(x => x.ProveedorId == query.ProveedorId.Value);

            if (query.CodigoClienteId.HasValue)
                baseQuery = baseQuery.Where(x => x.CodigoClienteId == query.CodigoClienteId.Value);

            if (query.Medio.HasValue)
                baseQuery = baseQuery.Where(x => x.Medio == query.Medio.Value);

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
                    (x.Comprobante != null && x.Comprobante.ToLower().Contains(f)) ||
                    (x.Observacion != null && x.Observacion.ToLower().Contains(f)) ||
                    (x.Proveedor != null && x.Proveedor.Nombre.ToLower().Contains(f)) ||
                    (x.CodigoCliente != null && x.CodigoCliente.Codigo.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();
            var compras = await ObtenerComprasDePagosAsync(entidades.Select(e => e.Id));

            return new PagedResultDto<PagoProveedorNavDto>
            {
                Items = entidades.Select(e => Mapear(e, compras.GetValueOrDefault(e.Id))).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pagos a proveedores");
            throw;
        }
    }

    // Devuelve también pagos anulados (isActive false) para poder consultarlos
    public async Task<PagoProveedorNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null) return null;
            var compras = await ObtenerComprasDePagosAsync(new[] { entidad.Id });
            return Mapear(entidad, compras.GetValueOrDefault(entidad.Id));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pago a proveedor {Id}", id);
            throw;
        }
    }

    public async Task<PagoProveedorNavDto> CrearAsync(CrearPagoProveedorNavDto dto)
    {
        try
        {
            ValidarEstructura(dto);

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var proveedor = await ObtenerProveedorValidoAsync(dto.ProveedorId, temporada.Id);

            if (dto.CompraId.HasValue)
            {
                // Pago de una compra: sin código (las compras no llevan), misma compra vigente, proveedor y temporada abierta
                if (dto.CodigoClienteId.HasValue)
                    throw new ValidacionException("El pago de una compra no lleva código de cliente");
                var (compra, _, saldo) = await ObtenerSaldoCompraAsync(dto.CompraId.Value, null);
                if (!compra.IsActive || compra.Anulada)
                    throw new ValidacionException("La compra está anulada");
                if (compra.ProveedorId != proveedor.Id)
                    throw new ValidacionException("La compra no es de ese proveedor");
                if (compra.TemporadaId != temporada.Id)
                    throw new ValidacionException("La compra no es de la temporada abierta");
                if (dto.Monto > saldo)
                    throw new ValidacionException($"El monto ({dto.Monto}) no puede superar el saldo de la compra #{compra.Id} ({saldo})");
            }
            else
            {
                await ValidarCodigoClienteAsync(proveedor, dto.CodigoClienteId);
            }

            var entidad = new PagoProveedor
            {
                TemporadaId = temporada.Id,
                ProveedorId = proveedor.Id,
                CodigoClienteId = dto.CodigoClienteId,
                CompraId = dto.CompraId,
                Fecha = dto.Fecha.Date,
                Monto = dto.Monto,
                Medio = dto.Medio,
                Comprobante = NormalizarTexto(dto.Comprobante),
                Observacion = NormalizarTexto(dto.Observacion)
            };

            var creado = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Pago a proveedor creado: {Id} (proveedor {ProveedorId}, {Monto})", creado.Id, creado.ProveedorId, creado.Monto);

            return await ObtenerPorIdAsync(creado.Id)
                ?? throw new EntidadNoEncontradaException("Pago a proveedor", creado.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear pago a proveedor");
            throw;
        }
    }

    public async Task<PagoProveedorNavDto> ActualizarAsync(ActualizarPagoProveedorNavDto dto)
    {
        try
        {
            var pago = await _repo.ObtenerPorIdAsync(dto.Id);
            if (pago == null)
                throw new EntidadNoEncontradaException("Pago a proveedor", dto.Id);
            if (!pago.IsActive)
                throw new ValidacionException("El pago está anulado: no se puede editar");

            await _temporadaActual.VerificarEditableAsync(pago.TemporadaId);

            if (dto.Fecha == default)
                throw new ValidacionException("La fecha es obligatoria");
            if (dto.Monto <= 0)
                throw new ValidacionException("El monto debe ser mayor a 0");
            if (!Enum.IsDefined(typeof(MedioPagoNav), dto.Medio))
                throw new ValidacionException("El medio de pago es inválido");
            if (dto.Comprobante != null && dto.Comprobante.Trim().Length > 100)
                throw new ValidacionException("El comprobante no puede superar 100 caracteres");
            if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
                throw new ValidacionException("La observación no puede superar 500 caracteres");

            // Pago de una compra: no puede superar el total de la compra
            if (pago.CompraId.HasValue)
            {
                var (compra, total, saldoSinEste) = await ObtenerSaldoCompraAsync(pago.CompraId.Value, pago.Id);
                if (dto.Monto > saldoSinEste)
                    throw new ValidacionException($"El monto ({dto.Monto}) no puede superar lo que admite la compra #{compra.Id}: {saldoSinEste} (total {total} menos los otros pagos)");
            }

            // No cambia proveedor, código ni compra vinculada
            pago.Fecha = dto.Fecha.Date;
            pago.Monto = dto.Monto;
            pago.Medio = dto.Medio;
            pago.Comprobante = NormalizarTexto(dto.Comprobante);
            pago.Observacion = NormalizarTexto(dto.Observacion);
            await _repo.ActualizarAsync(pago);

            _logger.LogInformation("Pago a proveedor actualizado: {Id}", pago.Id);

            return await ObtenerPorIdAsync(pago.Id)
                ?? throw new EntidadNoEncontradaException("Pago a proveedor", pago.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar pago a proveedor {Id}", dto.Id);
            throw;
        }
    }

    public async Task AnularAsync(int id)
    {
        try
        {
            var pago = await _repo.ObtenerPorIdAsync(id);
            if (pago == null)
                throw new EntidadNoEncontradaException("Pago a proveedor", id);

            if (!pago.IsActive)
                throw new ValidacionException("El pago ya está anulado");

            await _temporadaActual.VerificarEditableAsync(pago.TemporadaId);

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Pago a proveedor anulado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular pago a proveedor {Id}", id);
            throw;
        }
    }

    // ---------- Validaciones ----------

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarEstructura(CrearPagoProveedorNavDto dto)
    {
        if (dto.ProveedorId <= 0)
            throw new ValidacionException("El proveedor es obligatorio");
        if (dto.Fecha == default)
            throw new ValidacionException("La fecha es obligatoria");
        if (dto.Monto <= 0)
            throw new ValidacionException("El monto debe ser mayor a 0");
        if (!Enum.IsDefined(typeof(MedioPagoNav), dto.Medio))
            throw new ValidacionException("El medio de pago es inválido");
        if (dto.Comprobante != null && dto.Comprobante.Trim().Length > 100)
            throw new ValidacionException("El comprobante no puede superar 100 caracteres");
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
    }

    // Proveedor activo y de la temporada abierta (sin tracking)
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

    // Pago → compra vinculada (solo los que tienen compra)
    private async Task<Dictionary<int, CompraRef?>> ObtenerComprasDePagosAsync(IEnumerable<int> pagoIds)
    {
        var ids = pagoIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, CompraRef?>();
        var filas = await _repo.AsQueryable()
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && p.CompraId.HasValue)
            .Select(p => new { p.Id, CompraId = p.CompraId!.Value, p.Compra!.NroNota })
            .ToListAsync();
        return filas.ToDictionary(f => f.Id, f => (CompraRef?)new CompraRef(f.CompraId, f.NroNota));
    }

    private sealed record CompraRef(int Id, string? NroNota);

    // Saldo de una compra = total - pagos activos vinculados (sin contar excluirPagoId)
    private async Task<(Compra Compra, decimal Total, decimal Saldo)> ObtenerSaldoCompraAsync(int compraId, int? excluirPagoId)
    {
        var compra = await _compraRepo.AsQueryable()
            .AsNoTracking()
            .Include(c => c.Detalles)
            .Include(c => c.Pagos)
            .FirstOrDefaultAsync(c => c.Id == compraId)
            ?? throw new ValidacionException("La compra no existe");
        var total = Math.Round(compra.Detalles.Sum(d => d.CantidadUnidades * d.PrecioCompraUnidad), 2, MidpointRounding.AwayFromZero);
        var pagado = compra.Pagos.Where(p => p.IsActive && p.Id != excluirPagoId).Sum(p => p.Monto);
        return (compra, total, Math.Round(total - pagado, 2, MidpointRounding.AwayFromZero));
    }

    // ---------- Mapeo ----------

    private static PagoProveedorNavDto Mapear(PagoProveedor e, CompraRef? compra) => new()
    {
        CompraId = compra?.Id,
        CompraNroNota = compra?.NroNota,
        Id = e.Id,
        TemporadaId = e.TemporadaId,
        ProveedorId = e.ProveedorId,
        ProveedorNombre = e.Proveedor?.Nombre ?? string.Empty,
        CodigoClienteId = e.CodigoClienteId,
        Codigo = e.CodigoCliente?.Codigo,
        CodigoTitular = e.CodigoCliente?.Titular,
        Fecha = e.Fecha,
        Monto = e.Monto,
        Medio = e.Medio,
        MedioNombre = e.Medio.ToString(),
        Comprobante = e.Comprobante,
        Observacion = e.Observacion,
        IsActive = e.IsActive,
        CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt)
    };

    private static string? NormalizarTexto(string? texto)
    {
        var t = texto?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
