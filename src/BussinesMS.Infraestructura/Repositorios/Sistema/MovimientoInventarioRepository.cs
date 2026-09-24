using BussinesMS.Aplicacion.Common;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class MovimientoInventarioRepository : IMovimientoInventarioRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public MovimientoInventarioRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<MovimientoInventario> CrearAsync(MovimientoInventario entidad)
    {
        var rawId = _currentUser.GetUsuarioId();
        var usuarioId = rawId is int id && id > 0 ? id : 1;
        entidad.UsuarioId = usuarioId;
        entidad.FechaMovimiento = DateTime.UtcNow;
        _context.MovimientosInventario.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<MovimientoInventario> CrearSinGuardarAsync(MovimientoInventario entidad)
    {
        var rawId = _currentUser.GetUsuarioId();
        var usuarioId = rawId is int id && id > 0 ? id : 1;
        entidad.UsuarioId = usuarioId;
        entidad.FechaMovimiento = DateTime.UtcNow;
        _context.MovimientosInventario.Add(entidad);
        return entidad;
    }

    public async Task<List<MovimientoInventario>> ObtenerTodosAsync()
        => await _context.MovimientosInventario
            .Include(x => x.Variante)
            .OrderByDescending(x => x.FechaMovimiento)
            .ToListAsync();

    public async Task<List<MovimientoInventario>> ObtenerFiltradosAsync(
        int? loteId, int? varianteId, int? almacenOrigenId,
        int? almacenDestinoId, int? tipoMovimiento,
        DateTime? fechaDesde, DateTime? fechaHasta)
    {
        var query = _context.MovimientosInventario.AsQueryable();

        if (loteId.HasValue)
            query = query.Where(x => x.LoteAlmacenId == loteId.Value);
        if (varianteId.HasValue)
            query = query.Where(x => x.VarianteId == varianteId.Value);
        if (almacenOrigenId.HasValue)
            query = query.Where(x => x.AlmacenOrigenId == almacenOrigenId.Value);
        if (almacenDestinoId.HasValue)
            query = query.Where(x => x.AlmacenDestinoId == almacenDestinoId.Value);
        if (tipoMovimiento.HasValue)
            query = query.Where(x => (int)x.TipoMovimiento == tipoMovimiento.Value);
        if (fechaDesde.HasValue)
        {
            var (inicioUtc, _) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(fechaDesde.Value));
            query = query.Where(x => x.FechaMovimiento >= inicioUtc);
        }
        if (fechaHasta.HasValue)
        {
            var (_, finUtc) = BoliviaTimeZone.RangoDiaUtc(DateOnly.FromDateTime(fechaHasta.Value));
            query = query.Where(x => x.FechaMovimiento < finUtc);
        }

        return await query
            .Include(x => x.Variante)
            .OrderByDescending(x => x.FechaMovimiento)
            .ToListAsync();
    }

    public async Task<MovimientoInventario?> ObtenerPorIdAsync(int id)
        => await _context.MovimientosInventario
            .Include(x => x.Variante)
            .FirstOrDefaultAsync(x => x.Id == id);
}
