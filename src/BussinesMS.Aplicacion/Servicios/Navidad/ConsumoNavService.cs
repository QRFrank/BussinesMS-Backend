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
using ProductoNav = BussinesMS.Dominio.Entidades.Navidad.Producto;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Consumo propio / invitación / otro: baja stock (Consumo) sin cliente ni cobro; cuenta como pérdida al costo promedio.
// DTOs armados a mano (CreatedAt → hora de Bolivia aquí; nombres desde AuthDB).
public class ConsumoNavService : IConsumoNavService
{
    private const int SistemaNavidadId = 2;
    private const string ReferenciaConsumo = "Consumo";

    private readonly IConsumoNavRepository _repo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IStockNavService _stock;
    private readonly IConteoNavService _conteo;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ILogger<ConsumoNavService> _logger;

    public ConsumoNavService(
        IConsumoNavRepository repo,
        IProductoNavRepository productoRepo,
        IAlmacenRepository almacenRepo,
        IUsuarioRepository usuarioRepo,
        IStockNavService stock,
        IConteoNavService conteo,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        IAlmacenAccesoNavService acceso,
        ILogger<ConsumoNavService> logger)
    {
        _repo = repo;
        _productoRepo = productoRepo;
        _almacenRepo = almacenRepo;
        _usuarioRepo = usuarioRepo;
        _stock = stock;
        _conteo = conteo;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _acceso = acceso;
        _logger = logger;
    }

    public async Task<PagedResultDto<ConsumoNavDto>> ObtenerTodosAsync(ConsumoNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(x => x.Detalles)
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            // EncargadoAlmacen: solo los consumos de sus almacenes
            var aCargo = await _acceso.ObtenerRestriccionAsync(temporadaId);
            if (aCargo != null)
                baseQuery = baseQuery.Where(x => aCargo.Contains(x.AlmacenId));

            if (query.AlmacenId.HasValue)
                baseQuery = baseQuery.Where(x => x.AlmacenId == query.AlmacenId.Value);
            if (query.Motivo.HasValue)
            {
                var motivo = (MotivoConsumoNav)query.Motivo.Value;
                baseQuery = baseQuery.Where(x => x.Motivo == motivo);
            }
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
            if (query.Anulados.HasValue)
                baseQuery = baseQuery.Where(x => x.Anulado == query.Anulados.Value);

            if (!string.IsNullOrWhiteSpace(query.Filter))
            {
                var f = query.Filter.Trim().ToLower();
                baseQuery = baseQuery.Where(x => x.Observacion != null && x.Observacion.ToLower().Contains(f));
            }

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var almacenes = await ObtenerNombresAlmacenAsync(entidades.Select(e => e.AlmacenId));
            var usuarios = await ObtenerNombresUsuarioAsync(entidades.Select(e => e.UsuarioId));

            return new PagedResultDto<ConsumoNavDto>
            {
                Items = entidades.Select(e => MapearCabecera(e, almacenes, usuarios)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener consumos");
            throw;
        }
    }

    public async Task<ConsumoNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            await _acceso.VerificarAlmacenAsync(entidad.TemporadaId, entidad.AlmacenId);

            var almacenes = await ObtenerNombresAlmacenAsync(new[] { entidad.AlmacenId });
            var usuarios = await ObtenerNombresUsuarioAsync(new[] { entidad.UsuarioId });
            var costos = await _stock.ObtenerCostosPromedioAsync(entidad.TemporadaId, entidad.Detalles.Select(d => d.ProductoId));

            var dto = MapearCabecera(entidad, almacenes, usuarios);
            dto.Detalles = entidad.Detalles
                .OrderBy(d => d.Id)
                .Select(d =>
                {
                    var costo = costos.GetValueOrDefault(d.ProductoId);
                    return new ConsumoDetalleNavDto
                    {
                        Id = d.Id,
                        ProductoId = d.ProductoId,
                        ProductoNombreMostrar = d.Producto != null ? NombreMostrar(d.Producto) : string.Empty,
                        UnidadesPorEmpaque = d.Producto?.UnidadesPorEmpaque,
                        NombreEmpaque = d.Producto?.NombreEmpaque,
                        Color = d.Producto?.Color,
                        Unidades = d.Unidades,
                        CostoPromedio = Redondear(costo),
                        CostoEstimado = Redondear(d.Unidades * costo)
                    };
                }).ToList();
            // Informativo: Σ de unidades × costo promedio sin redondear cada línea
            dto.CostoEstimado = Redondear(entidad.Detalles.Sum(d => d.Unidades * costos.GetValueOrDefault(d.ProductoId)));
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener consumo {Id}", id);
            throw;
        }
    }

    public async Task<ConsumoNavDto> CrearAsync(CrearConsumoNavDto dto)
    {
        try
        {
            ValidarEstructura(dto);

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await _acceso.VerificarAlmacenAsync(temporada.Id, dto.AlmacenId);
            await ValidarAlmacenAsync(dto.AlmacenId);

            var productoIds = dto.Detalles.Select(d => d.ProductoId).ToList();
            var productos = await _productoRepo.AsQueryable()
                .AsNoTracking()
                .Where(p => productoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var item in dto.Detalles)
            {
                if (!productos.TryGetValue(item.ProductoId, out var producto) || producto.TemporadaId != temporada.Id)
                    throw new ValidacionException($"El producto {item.ProductoId} no existe o no es de la temporada abierta");
                if (!producto.IsActive)
                    throw new ValidacionException($"El producto {NombreMostrar(producto)} está inactivo");
            }

            // Baja stock de ese almacén: exige apertura de hoy si es tienda con conteo
            await _conteo.VerificarAperturaParaVentaAsync(dto.AlmacenId);

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            var consumo = new ConsumoNav
            {
                TemporadaId = temporada.Id,
                Fecha = BoliviaTimeZone.Now().Date,
                AlmacenId = dto.AlmacenId,
                UsuarioId = usuarioId,
                Motivo = (MotivoConsumoNav)dto.Motivo,
                Observacion = NormalizarTexto(dto.Observacion),
                Anulado = false
            };

            // RepositorioBase solo pone auditoría en la raíz
            foreach (var item in dto.Detalles)
                consumo.Detalles.Add(new ConsumoNavDetalle
                {
                    ProductoId = item.ProductoId,
                    Unidades = item.Unidades,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });

            await _uow.BeginTransactionAsync();
            try
            {
                var creado = await _repo.CrearAsync(consumo);

                // Respeta el bloqueo de reservas (409 STOCK_BLOQUEADO)
                foreach (var d in creado.Detalles)
                    await _stock.RegistrarSalidaAsync(temporada.Id, creado.AlmacenId, d.ProductoId, d.Unidades,
                        TipoMovimientoNav.Consumo, ReferenciaConsumo, creado.Id, creado.Observacion);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Consumo creado: {Id} (temporada {TemporadaId})", consumo.Id, temporada.Id);

            return await ObtenerPorIdAsync(consumo.Id)
                ?? throw new EntidadNoEncontradaException("Consumo", consumo.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear consumo");
            throw;
        }
    }

    public async Task AnularAsync(int id)
    {
        try
        {
            var consumo = await _repo.ObtenerConDetallesAsync(id);
            if (consumo == null || !consumo.IsActive)
                throw new EntidadNoEncontradaException("Consumo", id);
            if (consumo.Anulado)
                throw new ValidacionException("El consumo ya está anulado");

            await _temporadaActual.VerificarEditableAsync(consumo.TemporadaId);
            await _acceso.VerificarAlmacenAsync(consumo.TemporadaId, consumo.AlmacenId);

            await _uow.BeginTransactionAsync();
            try
            {
                // El stock vuelve al almacén del consumo
                foreach (var d in consumo.Detalles)
                    await _stock.RegistrarEntradaAsync(consumo.TemporadaId, consumo.AlmacenId, d.ProductoId, d.Unidades,
                        TipoMovimientoNav.AnulacionConsumo, ReferenciaConsumo, consumo.Id);

                // Cabecera con tracking (la consulta anterior es sin tracking).
                var entidad = await _repo.ObtenerPorIdAsync(id)
                    ?? throw new EntidadNoEncontradaException("Consumo", id);
                entidad.Anulado = true;
                await _repo.ActualizarAsync(entidad);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Consumo anulado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular consumo {Id}", id);
            throw;
        }
    }

    // ---------- Validaciones ----------

    private static void ValidarEstructura(CrearConsumoNavDto dto)
    {
        if (dto.AlmacenId <= 0)
            throw new ValidacionException("El almacén es obligatorio");
        if (!Enum.IsDefined(typeof(MotivoConsumoNav), dto.Motivo))
            throw new ValidacionException("El motivo del consumo es inválido");
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            throw new ValidacionException("Debe registrar al menos un producto");
        if (dto.Detalles.Any(d => d.ProductoId <= 0))
            throw new ValidacionException("El producto es obligatorio");
        if (dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count)
            throw new ValidacionException("Hay productos repetidos en el consumo");
        if (dto.Detalles.Any(d => d.Unidades <= 0))
            throw new ValidacionException("Las unidades deben ser mayores a 0");
    }

    // AuthDB: existe, es del sistema navideño y está activo
    private async Task ValidarAlmacenAsync(int almacenId)
    {
        var almacen = await _almacenRepo.AsQueryable().AsNoTracking().FirstOrDefaultAsync(a => a.Id == almacenId);
        if (almacen == null || !almacen.IsActive || almacen.SistemaId != SistemaNavidadId)
            throw new ValidacionException($"El almacén {almacenId} no es un almacén navideño activo");
    }

    // ---------- Mapeo ----------

    private static ConsumoNavDto MapearCabecera(ConsumoNav e, Dictionary<int, string> almacenes, Dictionary<int, string> usuarios) => new()
    {
        Id = e.Id,
        TemporadaId = e.TemporadaId,
        Fecha = e.Fecha,
        AlmacenId = e.AlmacenId,
        AlmacenNombre = almacenes.TryGetValue(e.AlmacenId, out var a) ? a : string.Empty,
        UsuarioId = e.UsuarioId,
        UsuarioNombre = usuarios.TryGetValue(e.UsuarioId, out var u) ? u : string.Empty,
        Motivo = (int)e.Motivo,
        MotivoNombre = e.Motivo switch
        {
            MotivoConsumoNav.Propio => "Consumo propio",
            MotivoConsumoNav.Invitacion => "Invitación",
            _ => "Otro"
        },
        Observacion = e.Observacion,
        Anulado = e.Anulado,
        CantidadProductos = e.Detalles.Count,
        TotalUnidades = e.Detalles.Sum(d => d.Unidades),
        IsActive = e.IsActive,
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

    private static string NombreMostrar(ProductoNav p) => p.Nombre ?? p.Descripcion;

    private static decimal Redondear(decimal valor) => Math.Round(valor, 2, MidpointRounding.AwayFromZero);

    private static string? NormalizarTexto(string? texto)
    {
        var t = texto?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
