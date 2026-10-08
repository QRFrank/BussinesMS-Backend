using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Nota: CreatedAt se convierte a hora de Bolivia en NavidadMappingProfile (no reconvertir aquí).
public class PagoInversorService : IPagoInversorService
{
    private readonly IPagoInversorRepository _repo;
    private readonly IAporteCapitalRepository _aporteRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly IMapper _mapper;
    private readonly ILogger<PagoInversorService> _logger;

    public PagoInversorService(
        IPagoInversorRepository repo,
        IAporteCapitalRepository aporteRepo,
        ITemporadaActualService temporadaActual,
        IMapper mapper,
        ILogger<PagoInversorService> logger)
    {
        _repo = repo;
        _aporteRepo = aporteRepo;
        _temporadaActual = temporadaActual;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<PagoInversorDto>> ObtenerTodosAsync(PagoInversorFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .Include(x => x.AporteCapital)
                    .ThenInclude(a => a!.Inversor)
                .Where(x => x.TemporadaId == temporadaId);

            // null/true → activos; false → solo anulados
            var activos = query.IsActive ?? true;
            baseQuery = baseQuery.Where(x => x.IsActive == activos);

            if (query.AporteCapitalId.HasValue)
                baseQuery = baseQuery.Where(x => x.AporteCapitalId == query.AporteCapitalId.Value);

            if (query.InversorId.HasValue)
                baseQuery = baseQuery.Where(x => x.AporteCapital != null && x.AporteCapital.InversorId == query.InversorId.Value);

            if (query.Tipo.HasValue)
                baseQuery = baseQuery.Where(x => x.Tipo == query.Tipo.Value);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.Observacion != null && x.Observacion.ToLower().Contains(f)) ||
                    (x.AporteCapital != null && x.AporteCapital.Inversor != null &&
                     x.AporteCapital.Inversor.Nombre.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<PagoInversorDto>
            {
                Items = _mapper.Map<List<PagoInversorDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener pagos a inversores");
            throw;
        }
    }

    public async Task<PagoInversorDto> CrearAsync(CrearPagoInversorDto dto)
    {
        try
        {
            if (dto.Monto <= 0)
                throw new ValidacionException("El monto debe ser mayor a 0");
            if (!Enum.IsDefined(typeof(TipoPagoInversor), dto.Tipo))
                throw new ValidacionException("El tipo de pago es inválido");
            if (dto.Observacion != null && dto.Observacion.Length > 500)
                throw new ValidacionException("La observación no puede superar 500 caracteres");

            var aporte = await _aporteRepo.ObtenerPorIdAsync(dto.AporteCapitalId);
            if (aporte == null || !aporte.IsActive)
                throw new EntidadNoEncontradaException("Aporte de capital", dto.AporteCapitalId);

            // Solo hay una temporada abierta: si es editable, el aporte es de la abierta
            await _temporadaActual.VerificarEditableAsync(aporte.TemporadaId);

            if (aporte.InversorId == null)
                throw new ValidacionException("El capital propio no tiene pagos a inversor");

            var pagado = await _repo.SumarPagosActivosAsync(aporte.Id, dto.Tipo);
            if (dto.Tipo == TipoPagoInversor.Comision)
            {
                var comisionTotal = Math.Round(aporte.Monto * aporte.PorcentajeComision / 100m, 2);
                var saldo = comisionTotal - pagado;
                if (pagado + dto.Monto > comisionTotal)
                    throw new ValidacionException($"El pago supera el saldo de comisión (saldo: {saldo:0.00})");
            }
            else
            {
                var saldo = aporte.Monto - pagado;
                if (pagado + dto.Monto > aporte.Monto)
                    throw new ValidacionException($"El pago supera el saldo de capital (saldo: {saldo:0.00})");
            }

            var entidad = new PagoInversor
            {
                TemporadaId = aporte.TemporadaId,
                AporteCapitalId = aporte.Id,
                Monto = dto.Monto,
                Fecha = dto.Fecha.Date,
                Tipo = dto.Tipo,
                Observacion = string.IsNullOrWhiteSpace(dto.Observacion) ? null : dto.Observacion.Trim()
            };

            var creado = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Pago a inversor creado: {Id} (aporte {AporteId}, {Tipo})", creado.Id, creado.AporteCapitalId, creado.Tipo);

            var conDetalles = await _repo.ObtenerConDetallesAsync(creado.Id) ?? creado;
            return _mapper.Map<PagoInversorDto>(conDetalles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear pago a inversor");
            throw;
        }
    }

    public async Task AnularAsync(int id)
    {
        try
        {
            var pago = await _repo.ObtenerPorIdAsync(id);
            if (pago == null)
                throw new EntidadNoEncontradaException("Pago a inversor", id);

            if (!pago.IsActive)
                throw new ValidacionException("El pago ya está anulado");

            await _temporadaActual.VerificarEditableAsync(pago.TemporadaId);

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Pago a inversor anulado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular pago a inversor {Id}", id);
            throw;
        }
    }
}
