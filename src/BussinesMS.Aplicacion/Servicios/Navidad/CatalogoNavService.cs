using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Dominio.Excepciones;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using CodigoClienteNav = BussinesMS.Dominio.Entidades.Navidad.CodigoCliente;
using ProductoNav = BussinesMS.Dominio.Entidades.Navidad.Producto;
using ProductoPresentacionNav = BussinesMS.Dominio.Entidades.Navidad.ProductoPresentacion;
using ProveedorNav = BussinesMS.Dominio.Entidades.Navidad.Proveedor;
using VendedorNav = BussinesMS.Dominio.Entidades.Navidad.Vendedor;

namespace BussinesMS.Aplicacion.Servicios.Navidad;

// Copia el catálogo (proveedores, códigos, productos, presentaciones y vendedores) de una temporada a la abierta.
// No copia stock, pedidos, deudas ni movimientos.
public class CatalogoNavService : ICatalogoNavService
{
    private readonly ITemporadaRepository _temporadaRepo;
    private readonly IProveedorNavRepository _proveedorRepo;
    private readonly ICodigoClienteRepository _codigoRepo;
    private readonly IProductoNavRepository _productoRepo;
    private readonly IProductoPresentacionNavRepository _presentacionRepo;
    private readonly IVendedorNavRepository _vendedorRepo;
    private readonly IUsuarioRepository _usuarioRepo;
    private readonly INavidadUnitOfWork _uow;
    private readonly ILogger<CatalogoNavService> _logger;

    public CatalogoNavService(
        ITemporadaRepository temporadaRepo,
        IProveedorNavRepository proveedorRepo,
        ICodigoClienteRepository codigoRepo,
        IProductoNavRepository productoRepo,
        IProductoPresentacionNavRepository presentacionRepo,
        IVendedorNavRepository vendedorRepo,
        IUsuarioRepository usuarioRepo,
        INavidadUnitOfWork uow,
        ILogger<CatalogoNavService> logger)
    {
        _temporadaRepo = temporadaRepo;
        _proveedorRepo = proveedorRepo;
        _codigoRepo = codigoRepo;
        _productoRepo = productoRepo;
        _presentacionRepo = presentacionRepo;
        _vendedorRepo = vendedorRepo;
        _usuarioRepo = usuarioRepo;
        _uow = uow;
        _logger = logger;
    }

    public async Task<CopiaCatalogoResultadoDto> CopiarAsync(int destinoId, int temporadaOrigenId)
    {
        try
        {
            if (destinoId == temporadaOrigenId)
                throw new ValidacionException("La temporada origen y la destino no pueden ser la misma");

            var destino = await _temporadaRepo.ObtenerPorIdAsync(destinoId);
            if (destino == null || !destino.IsActive)
                throw new EntidadNoEncontradaException("Temporada", destinoId);

            var origen = await _temporadaRepo.ObtenerPorIdAsync(temporadaOrigenId);
            if (origen == null || !origen.IsActive)
                throw new EntidadNoEncontradaException("Temporada", temporadaOrigenId);

            if (destino.Estado != EstadoTemporada.Abierta)
                throw new ExcepcionDominio("Solo se puede copiar el catálogo hacia la temporada abierta", 409, "TEMPORADA_CERRADA");

            if (await _proveedorRepo.TemporadaTieneProveedoresActivosAsync(destinoId))
                throw new ExcepcionDominio("La temporada destino ya tiene proveedores", 409, "CATALOGO_NO_VACIO");

            // Origen cargado sin tracking: se crean entidades nuevas, nunca se reutilizan estas instancias
            var proveedores = await _proveedorRepo.ObtenerActivosConCodigosPorTemporadaAsync(temporadaOrigenId);
            var productos = await _productoRepo.ObtenerActivosConPresentacionesPorTemporadaAsync(temporadaOrigenId);
            var vendedores = await _vendedorRepo.ObtenerActivosPorTemporadaAsync(temporadaOrigenId);

            // Usuarios que siguen siendo válidos (AuthDB, solo lectura) y los que ya son vendedores en destino
            var usuarioIds = vendedores.Select(v => v.UsuarioId).Distinct().ToList();
            var usuariosValidos = usuarioIds.Count == 0
                ? new HashSet<int>()
                : (await _usuarioRepo.AsQueryable()
                    .AsNoTracking()
                    .ValidosNavidad()
                    .Where(u => usuarioIds.Contains(u.Id))
                    .Select(u => u.Id)
                    .ToListAsync()).ToHashSet();
            var usuariosEnDestino = (await _vendedorRepo.ObtenerUsuarioIdsActivosAsync(destinoId)).ToHashSet();

            var resultado = new CopiaCatalogoResultadoDto
            {
                TemporadaOrigenId = temporadaOrigenId,
                TemporadaDestinoId = destinoId
            };

            await _uow.BeginTransactionAsync();
            try
            {
                // Proveedores y sus códigos; mapa idViejo -> idNuevo
                var mapaProveedores = new Dictionary<int, int>();
                foreach (var p in proveedores)
                {
                    var nuevo = await _proveedorRepo.CrearAsync(new ProveedorNav
                    {
                        TemporadaId = destinoId,
                        Nombre = p.Nombre,
                        UsaCodigosCliente = p.UsaCodigosCliente,
                        Telefono = p.Telefono,
                        Observacion = p.Observacion
                    });
                    mapaProveedores[p.Id] = nuevo.Id;
                    resultado.Proveedores++;

                    foreach (var c in p.Codigos.Where(c => c.IsActive))
                    {
                        await _codigoRepo.CrearAsync(new CodigoClienteNav
                        {
                            TemporadaId = destinoId,
                            ProveedorId = nuevo.Id,
                            Codigo = c.Codigo,
                            Titular = c.Titular
                        });
                        resultado.CodigosCliente++;
                    }
                }

                // Productos (solo de proveedores copiados) y sus presentaciones activas
                foreach (var prod in productos)
                {
                    if (!mapaProveedores.TryGetValue(prod.ProveedorId, out var nuevoProveedorId))
                        continue;

                    var nuevoProducto = await _productoRepo.CrearAsync(new ProductoNav
                    {
                        TemporadaId = destinoId,
                        ProveedorId = nuevoProveedorId,
                        // Categoría global: se reutiliza la misma
                        CategoriaProductoId = prod.CategoriaProductoId,
                        Nombre = prod.Nombre,
                        PrecioCompraUnidad = prod.PrecioCompraUnidad,
                        PrecioCatalogo = prod.PrecioCatalogo
                    });
                    resultado.Productos++;

                    foreach (var pres in prod.Presentaciones.Where(x => x.IsActive))
                    {
                        await _presentacionRepo.CrearAsync(new ProductoPresentacionNav
                        {
                            ProductoId = nuevoProducto.Id,
                            Nombre = pres.Nombre,
                            Unidades = pres.Unidades,
                            PrecioUnitario = pres.PrecioUnitario,
                            EsPrincipal = pres.EsPrincipal
                        });
                        resultado.Presentaciones++;
                    }
                }

                // Vendedores: solo usuarios aún válidos y que no sean ya vendedores activos en destino
                foreach (var v in vendedores)
                {
                    if (!usuariosValidos.Contains(v.UsuarioId) || usuariosEnDestino.Contains(v.UsuarioId))
                    {
                        resultado.VendedoresOmitidos++;
                        continue;
                    }

                    await _vendedorRepo.CrearAsync(new VendedorNav
                    {
                        TemporadaId = destinoId,
                        UsuarioId = v.UsuarioId,
                        Tipo = v.Tipo,
                        SueldoMensual = v.SueldoMensual
                    });
                    usuariosEnDestino.Add(v.UsuarioId);
                    resultado.Vendedores++;
                }

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            _logger.LogInformation(
                "Catálogo copiado de temporada {OrigenId} a {DestinoId}: {Proveedores} proveedores, {Productos} productos, {Vendedores} vendedores ({Omitidos} omitidos)",
                temporadaOrigenId, destinoId, resultado.Proveedores, resultado.Productos, resultado.Vendedores, resultado.VendedoresOmitidos);

            return resultado;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al copiar catálogo de temporada {OrigenId} a {DestinoId}", temporadaOrigenId, destinoId);
            throw;
        }
    }
}
