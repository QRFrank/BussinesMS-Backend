using BussinesMS.Aplicacion.Comun;
using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Dominio.Enums;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Sistema;

public class GastoOperativoService : IGastoOperativoService
{
    private const string OrigenCaja = "caja";
    private const string OrigenExterno = "externo";
    private const string OrigenMixto = "mixto";

    private readonly IGastoOperativoRepository _repo;
    private readonly ICategoriaGastoRepository _categoriaRepo;
    private readonly ISesionCajaRepository _sesionRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly ILogger<GastoOperativoService> _logger;

    public GastoOperativoService(
        IGastoOperativoRepository repo,
        ICategoriaGastoRepository categoriaRepo,
        ISesionCajaRepository sesionRepo,
        IUsuarioRepository usuarioRepo,
        ILogger<GastoOperativoService> logger)
    {
        _repo = repo;
        _categoriaRepo = categoriaRepo;
        _sesionRepo = sesionRepo;
        _usuarioRepo = usuarioRepo;
        _logger = logger;
    }

    public async Task<PagedResultDto<GastoOperativoListDto>> ObtenerTodosAsync(GastoOperativoFiltroDto query)
    {
        try
        {
            var activo = query.IsActive ?? true;
            var baseQuery = _repo.AsQueryable().Where(x => x.IsActive == activo);

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

            if (query.CategoriaGastoId.HasValue)
                baseQuery = baseQuery.Where(x => x.CategoriaGastoId == query.CategoriaGastoId.Value);

            if (query.AlmacenId.HasValue)
                baseQuery = baseQuery.Where(x => x.AlmacenId == query.AlmacenId.Value);

            if (query.FechaDesde.HasValue)
            {
                var (inicioUtc, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaDesde.Value));
                baseQuery = baseQuery.Where(x => x.FechaGasto >= inicioUtc);
            }

            if (query.FechaHasta.HasValue)
            {
                var (_, finUtc) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(query.FechaHasta.Value));
                baseQuery = baseQuery.Where(x => x.FechaGasto < finUtc);
            }

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.ToLower();
                baseQuery = baseQuery.Where(x =>
                    (x.Descripcion != null && x.Descripcion.ToLower().Contains(f)) ||
                    (x.CategoriaGasto != null && x.CategoriaGasto.Nombre.ToLower().Contains(f)));
            }

            // Orden por defecto: FechaGasto desc (si el cliente no manda SortBy)
            var sinOrdenCliente = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrdenCliente)
                baseQuery = baseQuery.OrderByDescending(x => x.FechaGasto).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrdenCliente);
            var entidades = await filteredQuery
                .Include(x => x.CategoriaGasto)
                .ToListAsync();

            var nombres = await ObtenerNombresUsuariosAsync(entidades.Select(e => e.CreatedByUsuarioId));

            return new PagedResultDto<GastoOperativoListDto>
            {
                Items = entidades.Select(e => MapearDto(e, nombres)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener gastos operativos");
            throw;
        }
    }

    public async Task<GastoOperativoListDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            // Devuelve también gastos anulados (IsActive=false) para poder ver su detalle
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null) return null;

            var nombres = await ObtenerNombresUsuariosAsync(new[] { entidad.CreatedByUsuarioId });
            return MapearDto(entidad, nombres);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener gasto operativo {Id}", id);
            throw;
        }
    }

    public async Task<GastoOperativoListDto> CrearAsync(CrearGastoOperativoDto dto)
    {
        try
        {
            ValidarMontos(dto.MontoCaja, dto.MontoExterno);

            var categoria = await ObtenerCategoriaValidaAsync(dto.CategoriaGastoId);

            var entidad = new GastoOperativo
            {
                CategoriaGastoId = dto.CategoriaGastoId,
                MontoCaja = dto.MontoCaja,
                MontoExterno = dto.MontoExterno,
                Monto = dto.MontoCaja + dto.MontoExterno,
                Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim()
            };

            if (dto.MontoCaja == 0 && dto.SesionCajaId.HasValue)
                throw new ValidacionException("Si no se paga con caja no debe indicarse sesión de caja");

            if (dto.MontoCaja > 0)
            {
                if (!dto.SesionCajaId.HasValue)
                    throw new ValidacionException("Para pagar con caja se requiere una sesión de caja");

                var sesion = await _sesionRepo.ObtenerPorIdAsync(dto.SesionCajaId.Value);
                if (sesion == null || !sesion.IsActive)
                    throw new EntidadNoEncontradaException("Sesión de caja", dto.SesionCajaId.Value);
                if (sesion.Estado != EstadoSesionCaja.Abierta)
                    throw new ValidacionException("La sesión de caja no está abierta");

                await ValidarEfectivoDisponibleAsync(sesion, dto.MontoCaja, montoCajaPropio: 0);

                // Gasto de caja: se ignoran FechaGasto/AlmacenId del body
                entidad.SesionCajaId = sesion.Id;
                entidad.AlmacenId = sesion.AlmacenId;
                entidad.FechaGasto = DateTime.UtcNow;
            }
            else
            {
                // Gasto externo: la fecha viene en hora local Bolivia
                entidad.SesionCajaId = null;
                entidad.AlmacenId = dto.AlmacenId;
                entidad.FechaGasto = dto.FechaGasto.HasValue
                    ? BoliviaTimeZone.ToUtc(dto.FechaGasto.Value)
                    : DateTime.UtcNow;
            }

            var creado = await _repo.CrearAsync(entidad);
            creado.CategoriaGasto = categoria;

            _logger.LogInformation("Gasto operativo creado: {Id} ({Monto})", creado.Id, creado.Monto);

            var nombres = await ObtenerNombresUsuariosAsync(new[] { creado.CreatedByUsuarioId });
            return MapearDto(creado, nombres);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear gasto operativo");
            throw;
        }
    }

    public async Task<GastoOperativoListDto> ActualizarAsync(ActualizarGastoOperativoDto dto)
    {
        try
        {
            var existente = await _repo.ObtenerConDetallesAsync(dto.Id);
            if (existente == null)
                throw new EntidadNoEncontradaException("Gasto operativo", dto.Id);

            if (!existente.IsActive)
                throw new ValidacionException("El gasto está anulado");

            if (existente.SesionCajaId.HasValue &&
                (existente.SesionCaja == null || existente.SesionCaja.Estado != EstadoSesionCaja.Abierta))
                throw new ValidacionException("No se puede modificar un gasto de una caja cerrada");

            ValidarMontos(dto.MontoCaja, dto.MontoExterno);

            var categoria = await ObtenerCategoriaValidaAsync(dto.CategoriaGastoId);

            if (dto.MontoCaja > 0)
            {
                // SesionCajaId no se cambia en el PUT
                if (!existente.SesionCajaId.HasValue)
                    throw new ValidacionException("Un registro externo no puede tener monto de caja");

                var sesion = existente.SesionCaja!;
                if (!sesion.IsActive)
                    throw new EntidadNoEncontradaException("Sesión de caja", sesion.Id);

                // El gasto está activo y en esta misma sesión: su MontoCaja actual se devuelve al disponible
                await ValidarEfectivoDisponibleAsync(sesion, dto.MontoCaja, montoCajaPropio: existente.MontoCaja);
            }

            existente.CategoriaGastoId = categoria.Id;
            existente.CategoriaGasto = categoria;
            existente.MontoCaja = dto.MontoCaja;
            existente.MontoExterno = dto.MontoExterno;
            existente.Monto = dto.MontoCaja + dto.MontoExterno;
            existente.Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim();

            // FechaGasto solo editable en gastos externos (hora local Bolivia); en gastos de caja se ignora
            if (!existente.SesionCajaId.HasValue && dto.FechaGasto.HasValue)
                existente.FechaGasto = BoliviaTimeZone.ToUtc(dto.FechaGasto.Value);

            var actualizado = await _repo.ActualizarAsync(existente);

            _logger.LogInformation("Gasto operativo actualizado: {Id} ({Monto})", actualizado.Id, actualizado.Monto);

            var nombres = await ObtenerNombresUsuariosAsync(new[] { actualizado.CreatedByUsuarioId });
            return MapearDto(actualizado, nombres);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al actualizar gasto operativo {Id}", dto.Id);
            throw;
        }
    }

    public async Task AnularAsync(int id)
    {
        try
        {
            var existente = await _repo.ObtenerConDetallesAsync(id);
            if (existente == null)
                throw new EntidadNoEncontradaException("Gasto operativo", id);

            if (!existente.IsActive)
                throw new ValidacionException("El gasto ya está anulado");

            if (existente.SesionCajaId.HasValue &&
                (existente.SesionCaja == null || existente.SesionCaja.Estado != EstadoSesionCaja.Abierta))
                throw new ValidacionException("No se puede anular un gasto de una caja cerrada");

            await _repo.EliminarAsync(id);
            _logger.LogInformation("Gasto operativo anulado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular gasto operativo {Id}", id);
            throw;
        }
    }

    private static void ValidarMontos(decimal montoCaja, decimal montoExterno)
    {
        if (montoCaja < 0 || montoExterno < 0)
            throw new ValidacionException("Los montos no pueden ser negativos");
        if (montoCaja + montoExterno <= 0)
            throw new ValidacionException("El monto debe ser mayor a 0");
    }

    private async Task<CategoriaGasto> ObtenerCategoriaValidaAsync(int categoriaGastoId)
    {
        var categoria = await _categoriaRepo.ObtenerPorIdAsync(categoriaGastoId);
        if (categoria == null)
            throw new EntidadNoEncontradaException("Categoría de gasto", categoriaGastoId);
        if (!categoria.IsActive)
            throw new ValidacionException("La categoría de gasto no está activa");
        return categoria;
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

    private static GastoOperativoListDto MapearDto(GastoOperativo e, IReadOnlyDictionary<int, string> nombres)
        => new()
        {
            Id = e.Id,
            SesionCajaId = e.SesionCajaId,
            CategoriaGastoId = e.CategoriaGastoId,
            CategoriaGastoNombre = e.CategoriaGasto?.Nombre ?? string.Empty,
            Monto = e.Monto,
            MontoCaja = e.MontoCaja,
            MontoExterno = e.MontoExterno,
            Descripcion = e.Descripcion,
            FechaGasto = BoliviaTimeZone.ToLocal(e.FechaGasto),
            AlmacenId = e.AlmacenId,
            Origen = e.MontoCaja > 0 && e.MontoExterno > 0 ? OrigenMixto
                : e.MontoCaja > 0 ? OrigenCaja
                : OrigenExterno,
            CreatedByUsuarioId = e.CreatedByUsuarioId,
            UsuarioNombre = nombres.TryGetValue(e.CreatedByUsuarioId, out var nombre) ? nombre : null,
            IsActive = e.IsActive,
            CreatedAt = BoliviaTimeZone.ToLocal(e.CreatedAt)
        };
}
