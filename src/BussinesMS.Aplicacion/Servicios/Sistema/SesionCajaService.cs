using AutoMapper;
using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Helpers;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class SesionCajaService : ISesionCajaService
{
    private readonly ISesionCajaRepository _repo;
    private readonly IMapper _mapper;
    private readonly ILogger<SesionCajaService> _logger;
    private readonly ICurrentUserService _currentUser;

    public SesionCajaService(
        ISesionCajaRepository repo,
        IMapper mapper,
        ILogger<SesionCajaService> logger,
        ICurrentUserService currentUser)
    {
        _repo = repo;
        _mapper = mapper;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task<SesionCajaDto> AbrirCajaAsync(CrearSesionCajaDto dto)
    {
        try
        {
            var usuarioId = _currentUser.GetUsuarioId() ?? 1;

            var existente = await _repo.ObtenerAbiertaPorUsuarioAsync(usuarioId, dto.AlmacenId);
            if (existente != null)
                throw new ValidacionException("Ya existe una sesión de caja abierta para este usuario en este almacén");

            var entidad = new SesionCaja
            {
                AlmacenId = dto.AlmacenId,
                MontoInicial = dto.MontoInicial,
                Estado = EstadoSesionCaja.Abierta
            };

            var resultado = await _repo.CrearAsync(entidad);
            return _mapper.Map<SesionCajaDto>(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al abrir sesión de caja");
            throw;
        }
    }

    public async Task<SesionCajaDto> CerrarCajaAsync(int id, CerrarSesionCajaDto dto)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            if (entidad == null)
                throw new EntidadNoEncontradaException("SesionCaja", id);

            ValidacionEntidad.VerificarActivo(entidad, "Sesión de caja");

            if (entidad.Estado != EstadoSesionCaja.Abierta)
                throw new ValidacionException("Solo se pueden cerrar sesiones abiertas");

            var montoEsperado = entidad.MontoInicial
                + entidad.IngresosEfectivo
                - entidad.EgresosGastos
                - entidad.EgresosPagoProveedor;

            entidad.FechaCierre = DateTime.UtcNow;
            entidad.MontoEsperadoEfectivo = montoEsperado;
            entidad.MontoRealEntregado = dto.MontoRealEntregado;
            entidad.Diferencia = dto.MontoRealEntregado - montoEsperado;
            entidad.Estado = EstadoSesionCaja.Cerrada;

            var resultado = await _repo.ActualizarAsync(entidad);
            return _mapper.Map<SesionCajaDto>(resultado);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al cerrar sesión de caja {Id}", id);
            throw;
        }
    }

    public async Task<SesionCajaDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerPorIdAsync(id);
            return entidad == null ? null : _mapper.Map<SesionCajaDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sesión de caja {Id}", id);
            throw;
        }
    }

    public async Task<SesionCajaDto?> ObtenerAbiertaAsync(int usuarioId, int almacenId)
    {
        try
        {
            var entidad = await _repo.ObtenerAbiertaPorUsuarioAsync(usuarioId, almacenId);
            return entidad == null ? null : _mapper.Map<SesionCajaDto>(entidad);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sesión abierta del usuario {UsuarioId}", usuarioId);
            throw;
        }
    }

    public async Task<PagedResultDto<SesionCajaListDto>> ObtenerTodosAsync(SesionCajaFiltroDto query)
    {
        try
        {
            var baseQuery = _repo.AsQueryable().Where(x => x.IsActive);

            if (query.AlmacenId.HasValue)
                baseQuery = baseQuery.Where(x => x.AlmacenId == query.AlmacenId.Value);

            if (query.UsuarioId.HasValue)
                baseQuery = baseQuery.Where(x => x.UsuarioId == query.UsuarioId.Value);

            if (query.Estado.HasValue)
                baseQuery = baseQuery.Where(x => x.Estado == query.Estado.Value);

            if (query.FechaDesde.HasValue)
            {
                var (inicioUtc, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaDesde.Value));
                baseQuery = baseQuery.Where(x => x.FechaApertura >= inicioUtc);
            }

            if (query.FechaHasta.HasValue)
            {
                var (_, finUtc) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaHasta.Value));
                baseQuery = baseQuery.Where(x => x.FechaApertura < finUtc);
            }

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query);
            var entidades = await filteredQuery
                .Include(x => x.Ventas)
                .ToListAsync();

            var items = entidades.Select(e => new SesionCajaListDto
            {
                Id = e.Id,
                UsuarioId = e.UsuarioId,
                AlmacenId = e.AlmacenId,
                FechaApertura = BoliviaTimeZone.ToLocal(e.FechaApertura),
                FechaCierre = e.FechaCierre.HasValue ? BoliviaTimeZone.ToLocal(e.FechaCierre.Value) : null,
                MontoInicial = e.MontoInicial,
                IngresosEfectivo = e.IngresosEfectivo,
                IngresosDigitales = e.IngresosDigitales,
                EgresosGastos = e.EgresosGastos,
                EgresosPagoProveedor = e.EgresosPagoProveedor,
                MontoEsperadoEfectivo = e.MontoEsperadoEfectivo,
                MontoRealEntregado = e.MontoRealEntregado,
                Diferencia = e.Diferencia,
                Estado = e.Estado,
                IsActive = e.IsActive,
                CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt),
                CantidadVentas = e.Ventas.Count,
                VentaIds = e.Ventas.Select(v => v.Id).ToList()
            }).ToList();

            return new PagedResultDto<SesionCajaListDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener sesiones de caja");
            throw;
        }
    }
}
