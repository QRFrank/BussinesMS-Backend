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

// Traslados entre almacenes navideños. DTOs armados a mano (CreatedAt → hora de Bolivia aquí;
// nombres de almacén y usuario desde AuthDB).
public class TrasladoNavService : ITrasladoNavService
{
    private const int SistemaNavidadId = 2;
    private const string ReferenciaTraslado = "Traslado";

    private readonly ITrasladoNavRepository _repo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IStockNavService _stock;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<TrasladoNavService> _logger;

    public TrasladoNavService(
        ITrasladoNavRepository repo,
        IProductoNavRepository productoRepo,
        IAlmacenRepository almacenRepo,
        IUsuarioRepository usuarioRepo,
        IStockNavService stock,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        ILogger<TrasladoNavService> logger)
    {
        _repo = repo;
        _productoRepo = productoRepo;
        _almacenRepo = almacenRepo;
        _usuarioRepo = usuarioRepo;
        _stock = stock;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<PagedResultDto<TrasladoNavDto>> ObtenerTodosAsync(TrasladoNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(x => x.Detalles)
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            if (query.AlmacenId.HasValue)
            {
                var a = query.AlmacenId.Value;
                baseQuery = baseQuery.Where(x => x.AlmacenOrigenId == a || x.AlmacenDestinoId == a);
            }

            if (query.AlmacenOrigenId.HasValue)
                baseQuery = baseQuery.Where(x => x.AlmacenOrigenId == query.AlmacenOrigenId.Value);

            if (query.AlmacenDestinoId.HasValue)
                baseQuery = baseQuery.Where(x => x.AlmacenDestinoId == query.AlmacenDestinoId.Value);

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

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var almacenes = await ObtenerNombresAlmacenAsync(
                entidades.SelectMany(e => new[] { e.AlmacenOrigenId, e.AlmacenDestinoId }));
            var usuarios = await ObtenerNombresUsuarioAsync(entidades.Select(e => e.UsuarioId));

            return new PagedResultDto<TrasladoNavDto>
            {
                Items = entidades.Select(e => MapearCabecera(e, almacenes, usuarios)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener traslados");
            throw;
        }
    }

    public async Task<TrasladoNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            var almacenes = await ObtenerNombresAlmacenAsync(new[] { entidad.AlmacenOrigenId, entidad.AlmacenDestinoId });
            var usuarios = await ObtenerNombresUsuarioAsync(new[] { entidad.UsuarioId });
            var dto = MapearCabecera(entidad, almacenes, usuarios);
            dto.Detalles = entidad.Detalles
                .OrderBy(d => d.Id)
                .Select(d => new TrasladoDetalleNavDto
                {
                    Id = d.Id,
                    ProductoId = d.ProductoId,
                    ProductoNombreMostrar = d.Producto != null ? NombreMostrar(d.Producto) : string.Empty,
                    UnidadesPorEmpaque = d.Producto?.UnidadesPorEmpaque,
                    NombreEmpaque = d.Producto?.NombreEmpaque,
                    Color = d.Producto?.Color,
                    CantidadUnidades = d.CantidadUnidades
                }).ToList();
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener traslado {Id}", id);
            throw;
        }
    }

    public async Task<TrasladoNavDto> CrearAsync(CrearTrasladoNavDto dto)
    {
        try
        {
            ValidarEstructura(dto);

            var hoy = BoliviaTimeZone.Now().Date;
            var fecha = dto.Fecha?.Date ?? hoy;
            if (fecha > hoy)
                throw new ValidacionException("La fecha del traslado no puede ser futura");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            var almacenes = await ValidarAlmacenesAsync(new[] { dto.AlmacenOrigenId, dto.AlmacenDestinoId });

            var productoIds = dto.Detalles.Select(d => d.ProductoId).ToList();
            var productos = await _productoRepo.AsQueryable()
                .AsNoTracking()
                .Where(p => productoIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            foreach (var item in dto.Detalles)
            {
                if (!productos.TryGetValue(item.ProductoId, out var producto) || producto.TemporadaId != temporada.Id)
                {
                    var nombre = producto != null ? NombreMostrar(producto) : item.ProductoId.ToString();
                    throw new ValidacionException($"El producto {nombre} no existe o no es de la temporada abierta");
                }
                if (!producto.IsActive)
                    throw new ValidacionException($"El producto {NombreMostrar(producto)} está inactivo");
            }

            // Stock suficiente en el origen (antes de abrir la transacción): se listan todos los que faltan
            var faltantes = new List<string>();
            foreach (var item in dto.Detalles)
            {
                var disponible = await _stock.ObtenerDisponibleAsync(dto.AlmacenOrigenId, item.ProductoId);
                if (disponible < item.CantidadUnidades)
                    faltantes.Add($"'{NombreMostrar(productos[item.ProductoId])}' disponible {disponible}, solicitado {item.CantidadUnidades}");
            }
            if (faltantes.Count > 0)
                throw new ExcepcionDominio(
                    $"Stock insuficiente en '{almacenes[dto.AlmacenOrigenId]}': {string.Join("; ", faltantes)}",
                    409, "STOCK_INSUFICIENTE");

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;
            var motivo = NormalizarTexto(dto.Motivo);

            var traslado = new Traslado
            {
                TemporadaId = temporada.Id,
                AlmacenOrigenId = dto.AlmacenOrigenId,
                AlmacenDestinoId = dto.AlmacenDestinoId,
                Fecha = fecha,
                Motivo = motivo,
                Anulado = false,
                UsuarioId = usuarioId
            };

            // RepositorioBase solo pone auditoría en la raíz
            foreach (var item in dto.Detalles)
                traslado.Detalles.Add(new TrasladoDetalle
                {
                    ProductoId = item.ProductoId,
                    CantidadUnidades = item.CantidadUnidades,
                    CreatedByUsuarioId = usuarioId,
                    CreatedAt = ahora
                });

            await _uow.BeginTransactionAsync();
            try
            {
                var creado = await _repo.CrearAsync(traslado);

                foreach (var detalle in creado.Detalles)
                {
                    await _stock.RegistrarSalidaAsync(temporada.Id, creado.AlmacenOrigenId, detalle.ProductoId,
                        detalle.CantidadUnidades, TipoMovimientoNav.TrasladoSalida, ReferenciaTraslado, creado.Id, motivo);
                    await _stock.RegistrarEntradaAsync(temporada.Id, creado.AlmacenDestinoId, detalle.ProductoId,
                        detalle.CantidadUnidades, TipoMovimientoNav.TrasladoEntrada, ReferenciaTraslado, creado.Id, motivo);
                }

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Traslado creado: {Id} (temporada {TemporadaId})", traslado.Id, temporada.Id);

            return await ObtenerPorIdAsync(traslado.Id)
                ?? throw new EntidadNoEncontradaException("Traslado", traslado.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear traslado");
            throw;
        }
    }

    public async Task AnularAsync(int id)
    {
        try
        {
            var traslado = await _repo.ObtenerConDetallesAsync(id);
            if (traslado == null || !traslado.IsActive)
                throw new EntidadNoEncontradaException("Traslado", id);
            if (traslado.Anulado)
                throw new ValidacionException("El traslado ya está anulado");

            await _temporadaActual.VerificarEditableAsync(traslado.TemporadaId);

            // El stock actual del destino tiene que alcanzar para devolver lo trasladado
            var nombresAlmacen = await ObtenerNombresAlmacenAsync(new[] { traslado.AlmacenOrigenId, traslado.AlmacenDestinoId });
            var almacenDestino = nombresAlmacen.TryGetValue(traslado.AlmacenDestinoId, out var nd) ? nd : $"#{traslado.AlmacenDestinoId}";
            foreach (var d in traslado.Detalles)
            {
                var disponible = await _stock.ObtenerDisponibleAsync(traslado.AlmacenDestinoId, d.ProductoId);
                if (disponible < d.CantidadUnidades)
                {
                    var producto = d.Producto != null ? NombreMostrar(d.Producto) : $"#{d.ProductoId}";
                    throw new ExcepcionDominio(
                        $"No se puede anular: el stock de '{producto}' en '{almacenDestino}' es {disponible} y el traslado movió {d.CantidadUnidades}",
                        409, "STOCK_INSUFICIENTE");
                }
            }

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var d in traslado.Detalles)
                {
                    await _stock.RegistrarSalidaAsync(traslado.TemporadaId, traslado.AlmacenDestinoId, d.ProductoId,
                        d.CantidadUnidades, TipoMovimientoNav.AnulacionTraslado, ReferenciaTraslado, traslado.Id);
                    await _stock.RegistrarEntradaAsync(traslado.TemporadaId, traslado.AlmacenOrigenId, d.ProductoId,
                        d.CantidadUnidades, TipoMovimientoNav.AnulacionTraslado, ReferenciaTraslado, traslado.Id);
                }

                // Cabecera con tracking (la consulta anterior es sin tracking).
                var entidad = await _repo.ObtenerPorIdAsync(id)
                    ?? throw new EntidadNoEncontradaException("Traslado", id);
                entidad.Anulado = true;
                await _repo.ActualizarAsync(entidad);

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Traslado anulado: {Id}", id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al anular traslado {Id}", id);
            throw;
        }
    }

    // ---------- Validaciones ----------

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarEstructura(CrearTrasladoNavDto dto)
    {
        if (dto.AlmacenOrigenId <= 0)
            throw new ValidacionException("El almacén de origen es obligatorio");
        if (dto.AlmacenDestinoId <= 0)
            throw new ValidacionException("El almacén de destino es obligatorio");
        if (dto.AlmacenOrigenId == dto.AlmacenDestinoId)
            throw new ValidacionException("El almacén de origen y el de destino deben ser distintos");
        if (dto.Motivo != null && dto.Motivo.Trim().Length > 500)
            throw new ValidacionException("El motivo no puede superar 500 caracteres");
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            throw new ValidacionException("Debe registrar al menos un producto");
        if (dto.Detalles.Any(d => d.ProductoId <= 0))
            throw new ValidacionException("El producto es obligatorio");
        if (dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count)
            throw new ValidacionException("Hay productos repetidos en el traslado");
        if (dto.Detalles.Any(d => d.CantidadUnidades <= 0))
            throw new ValidacionException("La cantidad debe ser mayor a 0");
    }

    // AuthDB: existen, son del sistema navideño y están activos. Devuelve id → nombre
    private async Task<Dictionary<int, string>> ValidarAlmacenesAsync(IEnumerable<int> almacenIds)
    {
        var ids = almacenIds.Distinct().ToList();
        var almacenes = await _almacenRepo.AsQueryable()
            .AsNoTracking()
            .Where(a => ids.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id);

        foreach (var id in ids)
        {
            if (!almacenes.TryGetValue(id, out var almacen))
                throw new ValidacionException($"El almacén {id} no es un almacén navideño activo");
            if (!almacen.IsActive || almacen.SistemaId != SistemaNavidadId)
                throw new ValidacionException($"El almacén {almacen.Nombre} no es un almacén navideño activo");
        }

        return almacenes.ToDictionary(x => x.Key, x => x.Value.Nombre);
    }

    // ---------- Mapeo ----------

    private static TrasladoNavDto MapearCabecera(Traslado e, Dictionary<int, string> almacenes, Dictionary<int, string> usuarios) => new()
    {
        Id = e.Id,
        TemporadaId = e.TemporadaId,
        AlmacenOrigenId = e.AlmacenOrigenId,
        AlmacenOrigenNombre = almacenes.TryGetValue(e.AlmacenOrigenId, out var o) ? o : string.Empty,
        AlmacenDestinoId = e.AlmacenDestinoId,
        AlmacenDestinoNombre = almacenes.TryGetValue(e.AlmacenDestinoId, out var d) ? d : string.Empty,
        Fecha = e.Fecha,
        Motivo = e.Motivo,
        Anulado = e.Anulado,
        UsuarioId = e.UsuarioId,
        UsuarioNombre = usuarios.TryGetValue(e.UsuarioId, out var u) ? u : string.Empty,
        CantidadProductos = e.Detalles.Count,
        TotalUnidades = e.Detalles.Sum(x => x.CantidadUnidades),
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

    private static string? NormalizarTexto(string? texto)
    {
        var t = texto?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
