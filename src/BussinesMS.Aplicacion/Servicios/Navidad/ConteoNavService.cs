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

// Apertura y cierre diario (conteos de inventario por tienda). DTOs armados a mano
// (CreatedAt → hora de Bolivia aquí; nombres de almacén y usuario desde AuthDB).
public class ConteoNavService : IConteoNavService
{
    private const int SistemaNavidadId = 2;
    private const string ReferenciaConteo = "ConteoInventario";

    private readonly IConteoInventarioNavRepository _repo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IStockAlmacenNavRepository _stockRepo;
    private readonly IAlmacenRepository _almacenRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly IStockNavService _stock;
    private readonly ITemporadaActualService _temporadaActual;
    private readonly INavidadUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IAlmacenAccesoNavService _acceso;
    private readonly ILogger<ConteoNavService> _logger;

    public ConteoNavService(
        IConteoInventarioNavRepository repo,
        IProductoNavRepository productoRepo,
        IStockAlmacenNavRepository stockRepo,
        IAlmacenRepository almacenRepo,
        IUsuarioRepository usuarioRepo,
        IStockNavService stock,
        ITemporadaActualService temporadaActual,
        INavidadUnitOfWork uow,
        ICurrentUserService currentUser,
        IAlmacenAccesoNavService acceso,
        ILogger<ConteoNavService> logger)
    {
        _repo = repo;
        _productoRepo = productoRepo;
        _stockRepo = stockRepo;
        _almacenRepo = almacenRepo;
        _usuarioRepo = usuarioRepo;
        _stock = stock;
        _temporadaActual = temporadaActual;
        _uow = uow;
        _currentUser = currentUser;
        _acceso = acceso;
        _logger = logger;
    }

    public async Task<PagedResultDto<ConteoNavDto>> ObtenerTodosAsync(ConteoNavFiltroDto query)
    {
        try
        {
            var temporadaId = await _temporadaActual.ResolverTemporadaIdAsync(query.TemporadaId);

            var baseQuery = _repo.AsQueryable()
                .AsNoTracking()
                .Include(x => x.Detalles)
                .Where(x => x.IsActive && x.TemporadaId == temporadaId);

            // EncargadoAlmacen: solo los conteos de sus tiendas
            var aCargo = await _acceso.ObtenerRestriccionAsync(temporadaId);
            if (aCargo != null)
                baseQuery = baseQuery.Where(x => aCargo.Contains(x.AlmacenId));

            if (query.AlmacenId.HasValue)
                baseQuery = baseQuery.Where(x => x.AlmacenId == query.AlmacenId.Value);

            if (query.Tipo.HasValue)
            {
                var tipo = (TipoConteoInventario)query.Tipo.Value;
                baseQuery = baseQuery.Where(x => x.Tipo == tipo);
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

            var sinOrden = string.IsNullOrWhiteSpace(query.SortBy);
            if (sinOrden)
                baseQuery = baseQuery.OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id);

            (var filteredQuery, var totalCount) = baseQuery.ApplyFilters(query, skipSorting: sinOrden);
            var entidades = await filteredQuery.ToListAsync();

            var almacenes = await ObtenerNombresAlmacenAsync(entidades.Select(e => e.AlmacenId));
            var usuarios = await ObtenerNombresUsuarioAsync(entidades.Select(e => e.UsuarioId));

            return new PagedResultDto<ConteoNavDto>
            {
                Items = entidades.Select(e => MapearCabecera(e, almacenes, usuarios)).ToList(),
                TotalCount = totalCount,
                Page = query.GetPageValue(),
                PageSize = query.GetPageSizeValue()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener conteos");
            throw;
        }
    }

    public async Task<ConteoNavDto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            var entidad = await _repo.ObtenerConDetallesAsync(id);
            if (entidad == null || !entidad.IsActive) return null;

            await _acceso.VerificarAlmacenAsync(entidad.TemporadaId, entidad.AlmacenId);

            var almacenes = await ObtenerNombresAlmacenAsync(new[] { entidad.AlmacenId });
            var usuarios = await ObtenerNombresUsuarioAsync(new[] { entidad.UsuarioId });
            var dto = MapearCabecera(entidad, almacenes, usuarios);
            dto.Detalles = entidad.Detalles
                .OrderBy(d => d.Id)
                .Select(d => new ConteoDetalleNavDto
                {
                    Id = d.Id,
                    ProductoId = d.ProductoId,
                    ProductoNombreMostrar = d.Producto != null ? NombreMostrar(d.Producto) : string.Empty,
                    UnidadesPorEmpaque = d.Producto?.UnidadesPorEmpaque,
                    NombreEmpaque = d.Producto?.NombreEmpaque,
                    Color = d.Producto?.Color,
                    StockSistema = d.StockSistema,
                    Contado = d.Contado,
                    Diferencia = d.Diferencia,
                    Observacion = d.Observacion,
                    MotivoDiferencia = d.MotivoDiferencia.HasValue ? (int)d.MotivoDiferencia.Value : null,
                    MotivoDiferenciaNombre = d.MotivoDiferencia.HasValue ? NombreMotivo(d.MotivoDiferencia.Value) : null
                }).ToList();
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener conteo {Id}", id);
            throw;
        }
    }

    public async Task<List<ConteoPlantillaNavDto>> ObtenerPlantillaAsync(int almacenId, int tipo)
    {
        try
        {
            if (almacenId <= 0)
                throw new ValidacionException("El almacén es obligatorio");
            if (!Enum.IsDefined(typeof(TipoConteoInventario), tipo))
                throw new ValidacionException("El tipo de conteo es inválido");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await _acceso.VerificarAlmacenAsync(temporada.Id, almacenId);
            var almacenes = await ValidarAlmacenesAsync(new[] { almacenId });
            await ValidarRequiereConteoAsync(temporada.Id, almacenId, almacenes[almacenId]);

            var productos = await _productoRepo.AsQueryable()
                .AsNoTracking()
                .Where(p => p.TemporadaId == temporada.Id && p.IsActive)
                .ToListAsync();

            var stocks = await _stockRepo.AsQueryable()
                .AsNoTracking()
                .Where(s => s.AlmacenId == almacenId)
                .ToDictionaryAsync(s => s.ProductoId, s => s.Cantidad);

            return productos
                .Select(p => new ConteoPlantillaNavDto
                {
                    ProductoId = p.Id,
                    ProductoNombreMostrar = NombreMostrar(p),
                    UnidadesPorEmpaque = p.UnidadesPorEmpaque,
                    NombreEmpaque = p.NombreEmpaque,
                    Color = p.Color,
                    StockSistema = stocks.TryGetValue(p.Id, out var c) ? c : 0
                })
                .OrderBy(x => x.ProductoNombreMostrar, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener plantilla de conteo");
            throw;
        }
    }

    public async Task<ConteoEstadoNavDto> ObtenerEstadoAsync(int almacenId, DateTime? fecha)
    {
        try
        {
            var dia = (fecha ?? BoliviaTimeZone.Now()).Date;
            var estado = new ConteoEstadoNavDto { AlmacenId = almacenId, Fecha = dia };

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await _acceso.VerificarAlmacenAsync(temporada.Id, almacenId);
            if (!await _temporadaActual.RequiereConteoAsync(temporada.Id, almacenId))
                return estado;

            estado.RequiereConteo = true;

            var delDia = await _repo.AsQueryable()
                .AsNoTracking()
                .Where(c => c.IsActive && c.TemporadaId == temporada.Id && c.AlmacenId == almacenId && c.Fecha == dia)
                .OrderBy(c => c.Id)
                .Select(c => new { c.Id, c.Tipo })
                .ToListAsync();

            // Criterio del último turno del día: aperturaHecha = hubo alguna apertura;
            // cierreHecho = el último conteo del día es un cierre (turno cerrado)
            var apertura = delDia.LastOrDefault(c => c.Tipo == TipoConteoInventario.Apertura);
            var ultimoDia = delDia.LastOrDefault();
            var cierreHecho = ultimoDia != null && ultimoDia.Tipo == TipoConteoInventario.Cierre;
            estado.AperturaHecha = apertura != null;
            estado.AperturaId = apertura?.Id;
            estado.CierreHecho = cierreHecho;
            estado.CierreId = cierreHecho ? ultimoDia!.Id : null;
            estado.TurnosHoy = delDia.Count(c => c.Tipo == TipoConteoInventario.Apertura);
            estado.CierrePendienteFecha = await ObtenerCierrePendienteAsync(temporada.Id, almacenId, dia);

            var ultimo = await ObtenerUltimoConteoAsync(temporada.Id, almacenId);
            if (ultimo != null)
            {
                estado.UltimoTipo = (int)ultimo.Tipo;
                estado.UltimaFecha = ultimo.Fecha;
                estado.UltimaHora = BoliviaTimeZone.ToLocal(ultimo.CreatedAt);
                estado.Abierta = ultimo.Tipo == TipoConteoInventario.Apertura && ultimo.Fecha == BoliviaTimeZone.Now().Date;
            }

            return estado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener estado de conteo");
            throw;
        }
    }

    public async Task VerificarAperturaParaVentaAsync(int almacenId)
    {
        var temporada = await _temporadaActual.ObtenerAbiertaAsync();
        if (!await _temporadaActual.RequiereConteoAsync(temporada.Id, almacenId))
            return;

        var hoy = BoliviaTimeZone.Now().Date;
        // Turnos secuenciales: abierta = el último conteo es una Apertura de hoy
        var ultimo = await ObtenerUltimoConteoAsync(temporada.Id, almacenId);
        if (ultimo != null && ultimo.Tipo == TipoConteoInventario.Apertura && ultimo.Fecha == hoy)
            return;

        if (ultimo != null && ultimo.Tipo == TipoConteoInventario.Apertura && ultimo.Fecha < hoy)
            throw new ExcepcionDominio($"Falta el cierre del día {ultimo.Fecha:dd/MM/yyyy}", 409, "CIERRE_PENDIENTE");

        var nombres = await ObtenerNombresAlmacenAsync(new[] { almacenId });
        var nombre = nombres.TryGetValue(almacenId, out var n) ? n : $"#{almacenId}";
        throw new ExcepcionDominio($"La tienda {nombre} no está abierta: hacé la apertura", 409, "SIN_APERTURA");
    }

    public async Task<ConteoNavDto> CrearAsync(CrearConteoNavDto dto)
    {
        try
        {
            ValidarEstructura(dto);

            var tipo = (TipoConteoInventario)dto.Tipo;
            var hoy = BoliviaTimeZone.Now().Date;

            if (dto.Fecha.HasValue && tipo == TipoConteoInventario.Apertura)
                throw new ValidacionException("La apertura siempre es de hoy: no envíe fecha");
            // Cierre atrasado: solo si viene fecha (día pendiente anterior a hoy)
            var atrasado = tipo == TipoConteoInventario.Cierre && dto.Fecha.HasValue;
            var dia = atrasado ? dto.Fecha!.Value.Date : hoy;
            if (atrasado && dia >= hoy)
                throw new ValidacionException("El cierre atrasado debe ser de un día anterior a hoy; para cerrar hoy no envíe fecha");

            var temporada = await _temporadaActual.ObtenerAbiertaAsync();
            await _acceso.VerificarAlmacenAsync(temporada.Id, dto.AlmacenId);
            var almacenes = await ValidarAlmacenesAsync(new[] { dto.AlmacenId });
            await ValidarRequiereConteoAsync(temporada.Id, dto.AlmacenId, almacenes[dto.AlmacenId]);

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

            // Turnos secuenciales: manda el último conteo activo de la tienda en la temporada
            var ultimo = await ObtenerUltimoConteoAsync(temporada.Id, dto.AlmacenId);
            var ultimaEsApertura = ultimo != null && ultimo.Tipo == TipoConteoInventario.Apertura;

            if (atrasado)
            {
                if (!ultimaEsApertura || ultimo!.Fecha != dia)
                    throw new ValidacionException($"El día {dia:dd/MM/yyyy} no es un día pendiente de cierre: no tiene apertura o ya tiene cierre");
            }
            else if (tipo == TipoConteoInventario.Cierre)
            {
                if (ultimaEsApertura && ultimo!.Fecha < hoy)
                    throw new ValidacionException(
                        $"Falta el cierre del día {ultimo.Fecha:dd/MM/yyyy}: use el cierre atrasado de ese día");
                if (!ultimaEsApertura)
                    throw new ExcepcionDominio(
                        "No hay apertura de hoy en este almacén: haga la apertura antes del cierre",
                        409, "CIERRE_SIN_APERTURA");
            }
            else if (ultimaEsApertura)
            {
                if (ultimo!.Fecha < hoy)
                    throw new ExcepcionDominio(
                        $"Falta el cierre del día {ultimo.Fecha:dd/MM/yyyy}", 409, "CIERRE_PENDIENTE");
                var nombreTienda = almacenes[dto.AlmacenId];
                throw new ExcepcionDominio($"La tienda {nombreTienda} ya está abierta", 409, "TIENDA_YA_ABIERTA");
            }

            var usuarioId = _currentUser.GetUsuarioId() ?? 1;
            var ahora = DateTime.UtcNow;

            var conteo = new ConteoInventario
            {
                TemporadaId = temporada.Id,
                AlmacenId = dto.AlmacenId,
                Fecha = dia,
                Tipo = tipo,
                UsuarioId = usuarioId,
                Observacion = NormalizarTexto(dto.Observacion)
            };

            await _uow.BeginTransactionAsync();
            try
            {
                // Stock del sistema tomado dentro de la transacción, antes de ajustar
                foreach (var item in dto.Detalles)
                {
                    var stockSistema = await _stock.ObtenerDisponibleAsync(dto.AlmacenId, item.ProductoId);
                    var diferencia = item.Contado - stockSistema;
                    var obs = NormalizarTexto(item.Observacion);
                    if (diferencia != 0 && obs == null)
                        throw new ValidacionException(
                            $"El producto {NombreMostrar(productos[item.ProductoId])} tiene diferencia ({diferencia}) y requiere observación");

                    // Motivo: obligatorio si falta stock (diferencia < 0), opcional si sobra, ignorado si no hay diferencia
                    MotivoDiferenciaConteo? motivo = null;
                    if (diferencia < 0 && !item.MotivoDiferencia.HasValue)
                        throw new ValidacionException(
                            $"El producto {NombreMostrar(productos[item.ProductoId])} tiene un faltante ({diferencia}) y requiere el motivo de la diferencia");
                    if (diferencia != 0 && item.MotivoDiferencia.HasValue)
                    {
                        if (!Enum.IsDefined(typeof(MotivoDiferenciaConteo), item.MotivoDiferencia.Value))
                            throw new ValidacionException("El motivo de la diferencia es inválido");
                        motivo = (MotivoDiferenciaConteo)item.MotivoDiferencia.Value;
                    }

                    // RepositorioBase solo pone auditoría en la raíz
                    conteo.Detalles.Add(new ConteoInventarioDetalle
                    {
                        ProductoId = item.ProductoId,
                        StockSistema = stockSistema,
                        Contado = item.Contado,
                        Diferencia = diferencia,
                        Observacion = obs,
                        MotivoDiferencia = motivo,
                        CreatedByUsuarioId = usuarioId,
                        CreatedAt = ahora
                    });
                }

                var creado = await _repo.CrearAsync(conteo);

                foreach (var d in creado.Detalles.Where(x => x.Diferencia != 0))
                {
                    if (d.Diferencia > 0)
                        await _stock.RegistrarEntradaAsync(temporada.Id, creado.AlmacenId, d.ProductoId, d.Diferencia,
                            TipoMovimientoNav.AjusteConteo, ReferenciaConteo, creado.Id, d.Observacion);
                    else
                        // El conteo refleja lo físico: no respeta el bloqueo de reservas
                        await _stock.RegistrarSalidaAsync(temporada.Id, creado.AlmacenId, d.ProductoId, -d.Diferencia,
                            TipoMovimientoNav.AjusteConteo, ReferenciaConteo, creado.Id, d.Observacion, respetarBloqueo: false);
                }

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation("Conteo creado: {Id} ({Tipo}, temporada {TemporadaId})", conteo.Id, tipo, temporada.Id);

            return await ObtenerPorIdAsync(conteo.Id)
                ?? throw new EntidadNoEncontradaException("Conteo", conteo.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al crear conteo");
            throw;
        }
    }

    // ---------- Validaciones ----------

    // Reglas repetidas del validador por si FluentValidation no corre
    private static void ValidarEstructura(CrearConteoNavDto dto)
    {
        if (dto.AlmacenId <= 0)
            throw new ValidacionException("El almacén es obligatorio");
        if (!Enum.IsDefined(typeof(TipoConteoInventario), dto.Tipo))
            throw new ValidacionException("El tipo de conteo es inválido");
        if (dto.Observacion != null && dto.Observacion.Trim().Length > 500)
            throw new ValidacionException("La observación no puede superar 500 caracteres");
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            throw new ValidacionException("Debe registrar al menos un producto");
        if (dto.Detalles.Any(d => d.ProductoId <= 0))
            throw new ValidacionException("El producto es obligatorio");
        if (dto.Detalles.Select(d => d.ProductoId).Distinct().Count() != dto.Detalles.Count)
            throw new ValidacionException("Hay productos repetidos en el conteo");
        if (dto.Detalles.Any(d => d.Contado < 0))
            throw new ValidacionException("La cantidad contada no puede ser negativa");
        if (dto.Detalles.Any(d => d.Observacion != null && d.Observacion.Trim().Length > 500))
            throw new ValidacionException("La observación no puede superar 500 caracteres");
    }

    private async Task ValidarRequiereConteoAsync(int temporadaId, int almacenId, string almacenNombre)
    {
        if (!await _temporadaActual.RequiereConteoAsync(temporadaId, almacenId))
            throw new ValidacionException($"El almacén {almacenNombre} no requiere apertura y cierre diario");
    }

    // Fecha más antigua anterior a `dia` con apertura sin cierre (null si no hay)
    private async Task<DateTime?> ObtenerCierrePendienteAsync(int temporadaId, int almacenId, DateTime dia)
    {
        var pendientes = await ObtenerPendientesAsync(temporadaId, almacenId, dia);
        return pendientes.Count > 0 ? pendientes[0] : null;
    }

    // Con turnos secuenciales hay a lo sumo un día pendiente: el último conteo es una Apertura anterior a `dia`
    private async Task<List<DateTime>> ObtenerPendientesAsync(int temporadaId, int almacenId, DateTime dia)
    {
        var ultimo = await ObtenerUltimoConteoAsync(temporadaId, almacenId);
        if (ultimo != null && ultimo.Tipo == TipoConteoInventario.Apertura && ultimo.Fecha.Date < dia)
            return new List<DateTime> { ultimo.Fecha.Date };
        return new List<DateTime>();
    }

    // Último conteo activo de la tienda en la temporada (por Fecha y luego Id)
    private async Task<UltimoConteo?> ObtenerUltimoConteoAsync(int temporadaId, int almacenId)
        => await _repo.AsQueryable()
            .AsNoTracking()
            .Where(c => c.IsActive && c.TemporadaId == temporadaId && c.AlmacenId == almacenId)
            .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
            .Select(c => new UltimoConteo { Id = c.Id, Fecha = c.Fecha, Tipo = c.Tipo, CreatedAt = c.CreatedAt })
            .FirstOrDefaultAsync();

    private sealed class UltimoConteo
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public TipoConteoInventario Tipo { get; set; }
        public DateTime CreatedAt { get; set; }
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

    private static ConteoNavDto MapearCabecera(ConteoInventario e, Dictionary<int, string> almacenes, Dictionary<int, string> usuarios) => new()
    {
        Id = e.Id,
        TemporadaId = e.TemporadaId,
        AlmacenId = e.AlmacenId,
        AlmacenNombre = almacenes.TryGetValue(e.AlmacenId, out var a) ? a : string.Empty,
        Fecha = e.Fecha,
        Tipo = (int)e.Tipo,
        TipoNombre = e.Tipo == TipoConteoInventario.Apertura ? "Apertura" : "Cierre",
        UsuarioId = e.UsuarioId,
        UsuarioNombre = usuarios.TryGetValue(e.UsuarioId, out var u) ? u : string.Empty,
        Observacion = e.Observacion,
        CantidadProductos = e.Detalles.Count,
        CantidadDiferencias = e.Detalles.Count(d => d.Diferencia != 0),
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

    private static string NombreMotivo(MotivoDiferenciaConteo m) => m switch
    {
        MotivoDiferenciaConteo.VentaNoRegistrada => "Venta no registrada",
        MotivoDiferenciaConteo.Consumo => "Consumo",
        MotivoDiferenciaConteo.MermaRobo => "Merma o robo",
        MotivoDiferenciaConteo.ErrorConteo => "Error de conteo",
        _ => m.ToString()
    };

    private static string? NormalizarTexto(string? texto)
    {
        var t = texto?.Trim();
        return string.IsNullOrEmpty(t) ? null : t;
    }
}
