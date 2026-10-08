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
public class AporteCapitalService : IAporteCapitalService
{
    private const string CapitalPropioNombre = "Capital propio";

    private readonly IAporteCapitalRepository _repo;
    private readonly IInversorRepository _inversorRepo;
    private readonly IPagoInversorRepository _pagoRepo;
    private readonly ITemporadaRepository _temporadaRepo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly IMapper _mapper;
    private readonly ILogger<AporteCapitalService> _logger;

    public AporteCapitalService(
        IAporteCapitalRepository repo,
        IInversorRepository inversorRepo,
        IPagoInversorRepository pagoRepo,
        ITemporadaRepository temporadaRepo,
        ITemporadaActualService temporadaActual,
        IMapper mapper,
        ILogger<AporteCapitalService> logger)
    {
        _repo = repo;
        _inversorRepo = inversorRepo;
        _pagoRepo = pagoRepo;
        _temporadaRepo = temporadaRepo;
        _temporadaActual = temporadaActual;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PagedResultDto<AporteCapitalDto>> ObtenerTodosAsync(AporteCapitalFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .Include(x => x.Inversor)
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            if (query.InversorId.HasValue)
                baseQuery = baseQuery.Where(x => x.InversorId == query.InversorId.Value);

            if (query.SoloCapitalPropio == true)
                baseQuery = baseQuery.Where(x => x.InversorId == null);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.Inversor != null && x.Inversor.Nombre.ToLower().Contains(f)) ||
                    (x.Observacion != null && x.Observacion.ToLower().Contains(f)));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            return new PagedResultDto<AporteCapitalDto>
            {
                Items = _mapper.Map<List<AporteCapitalDto>>(entidades),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener aportes de capital");
            throw;
        }
    }

    public async Task<AporteCapitalDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;
            return _mapper.Map<AporteCapitalDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener aporte de capital {Id}", id);
            throw;
        }
    }

    public async Task<ResumenCapitalDto> ObtenerResumenAsync(int? temporadaId)
    {
        try
        {
            var id = await _temporadaActual.ResolverTemporadaIdAsync(temporadaId);
            var temporada = await _temporadaRepo.ObtenerPorIdAsync(id);
            var aportes = await _repo.ObtenerActivosConPagosPorTemporadaAsync(id);

            var detalle = aportes.Select(a =>
            {
                var esPropio = a.InversorId == null;
                var pagosActivos = a.Pagos.Where(p => p.IsActive).ToList();
                var comisionTotal = CalcularComision(a.Monto, a.PorcentajeComision);
                var comisionPagada = pagosActivos.Where(p => p.Tipo == TipoPagoInversor.Comision).Sum(p => p.Monto);
                var capitalDevuelto = esPropio
                    ? 0m
                    : pagosActivos.Where(p => p.Tipo == TipoPagoInversor.DevolucionCapital).Sum(p => p.Monto);

                return new ResumenAporteCapitalDto
                {
                    AporteCapitalId = a.Id,
                    InversorId = a.InversorId,
                    InversorNombre = esPropio ? CapitalPropioNombre : (a.Inversor?.Nombre ?? string.Empty),
                    EsCapitalPropio = esPropio,
                    Fecha = a.Fecha,
                    Monto = a.Monto,
                    PorcentajeComision = a.PorcentajeComision,
                    ComisionTotal = comisionTotal,
                    ComisionPagada = comisionPagada,
                    SaldoComision = comisionTotal - comisionPagada,
                    CapitalDevuelto = capitalDevuelto,
                    SaldoCapital = a.Monto - capitalDevuelto
                };
            })
            // Capital propio primero, luego por nombre de inversor
            .OrderByDescending(x => x.EsCapitalPropio)
            .ThenBy(x => x.InversorNombre)
            .ThenBy(x => x.Fecha)
            .ThenBy(x => x.AporteCapitalId)
            .ToList();

            var deInversores = detalle.Where(x => !x.EsCapitalPropio).ToList();

            return new ResumenCapitalDto
            {
                TemporadaId = id,
                TemporadaNombre = temporada?.Nombre ?? string.Empty,
                CapitalTotal = detalle.Sum(x => x.Monto),
                CapitalPropio = detalle.Where(x => x.EsCapitalPropio).Sum(x => x.Monto),
                CapitalInversores = deInversores.Sum(x => x.Monto),
                ComisionTotal = detalle.Sum(x => x.ComisionTotal),
                ComisionPagada = detalle.Sum(x => x.ComisionPagada),
                ComisionPorPagar = detalle.Sum(x => x.SaldoComision),
                CapitalDevuelto = deInversores.Sum(x => x.CapitalDevuelto),
                CapitalPorDevolver = deInversores.Sum(x => x.SaldoCapital),
                Aportes = detalle
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener resumen de capital");
            throw;
        }
    }

    public async Task<AporteCapitalDto> CrearAsync(CrearAporteCapitalDto dto)
    {
        try
        {
            var temporada = await _temporadaActual.ObtenerAbiertaAsync();

            ValidarReglas(dto.InversorId, dto.Monto, dto.PorcentajeComision, dto.Observacion);
            await ValidarInversorAsync(dto.InversorId);

            var entidad = new AporteCapital
            {
                TemporadaId = temporada.Id,
                InversorId = dto.InversorId,
                Monto = dto.Monto,
                Fecha = dto.Fecha.Date,
                PorcentajeComision = dto.PorcentajeComision,
                Observacion = Limpiar(dto.Observacion)
            };

            var creada = await _repo.CrearAsync(entidad);

            _logger.LogInformation("Aporte de capital creado: {Id} (temporada {TemporadaId})", creada.Id, creada.TemporadaId);

            var conDetalles = await _repo.ObtenerConDetallesAsync(creada.Id) ?? creada;
            return _mapper.Map<AporteCapitalDto>(conDetalles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear aporte de capital");
            throw;
        }
    }

    public async Task<AporteCapitalDto> ActualizarAsync(ActualizarAporteCapitalDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(dto.Id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Aporte de capital", dto.Id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            ValidarReglas(dto.InversorId, dto.Monto, dto.PorcentajeComision, dto.Observacion);
            if (dto.InversorId != existente.InversorId)
                await ValidarInversorAsync(dto.InversorId);

            if (await _pagoRepo.TienePagosActivosAsync(existente.Id))
            {
                if (dto.InversorId != existente.InversorId)
                    throw new ValidacionException("No se puede cambiar el inversor de un aporte con pagos registrados");

                // Los nuevos montos no pueden quedar por debajo de lo ya pagado
                var comisionPagada = await _pagoRepo.SumarPagosActivosAsync(existente.Id, TipoPagoInversor.Comision);
                var nuevaComisionTotal = CalcularComision(dto.Monto, dto.PorcentajeComision);
                if (comisionPagada > nuevaComisionTotal)
                    throw new ValidacionException(
                        $"La nueva comisión total ({nuevaComisionTotal:0.00}) quedaría por debajo de la comisión ya pagada ({comisionPagada:0.00})");

                var capitalDevuelto = await _pagoRepo.SumarPagosActivosAsync(existente.Id, TipoPagoInversor.DevolucionCapital);
                if (capitalDevuelto > dto.Monto)
                    throw new ValidacionException(
                        $"El nuevo monto ({dto.Monto:0.00}) quedaría por debajo del capital ya devuelto ({capitalDevuelto:0.00})");
            }

            existente.InversorId = dto.InversorId;
            existente.Monto = dto.Monto;
            existente.Fecha = dto.Fecha.Date;
            existente.PorcentajeComision = dto.PorcentajeComision;
            existente.Observacion = Limpiar(dto.Observacion);

            var actualizada = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Aporte de capital actualizado: {Id}", actualizada.Id);

            var conDetalles = await _repo.ObtenerConDetallesAsync(actualizada.Id) ?? actualizada;
            return _mapper.Map<AporteCapitalDto>(conDetalles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar aporte de capital {Id}", dto.Id);
            throw;
        }
    }

    public async Task EliminarAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerPorIdAsync(id);
            if (existente == null || !existente.IsActive)
                throw new EntidadNoEncontradaException("Aporte de capital", id);

            await _temporadaActual.VerificarEditableAsync(existente.TemporadaId);

            if (await _pagoRepo.TienePagosActivosAsync(id))
                throw new ValidacionException("El aporte tiene pagos registrados. Anule primero los pagos.");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Aporte de capital eliminado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al eliminar aporte de capital {Id}", id);
            throw;
        }
    }

    private static decimal CalcularComision(decimal monto, decimal porcentaje)
        => Math.Round(monto * porcentaje / 100m, 2);

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarReglas(int? inversorId, decimal monto, decimal porcentaje, string? observacion)
    {
        if (monto <= 0)
            throw new ValidacionException("El monto debe ser mayor a 0");
        if (porcentaje < 0 || porcentaje > 100)
            throw new ValidacionException("El porcentaje de comisión debe estar entre 0 y 100");
        if (inversorId == null && porcentaje != 0)
            throw new ValidacionException("El capital propio va con PorcentajeComision = 0");
        if (observacion != null && observacion.Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
    }

    private async Task ValidarInversorAsync(int? inversorId)
    {
        if (!inversorId.HasValue) return;

        var inversor = await _inversorRepo.ObtenerPorIdAsync(inversorId.Value);
        if (inversor == null || !inversor.IsActive)
            throw new ValidacionException("El inversor no existe o no está activo");
    }

    private static string? Limpiar(string? valor)
        => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
