using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Sistema;

public class ProductoPresentacionRepository : IProductoPresentacionRepository
{
    private readonly SistemaDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public ProductoPresentacionRepository(SistemaDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public IQueryable<ProductoPresentacion> AsQueryable()
        => _context.ProductoPresentaciones.AsQueryable();

    public async Task<List<ProductoPresentacion>> ObtenerPorVarianteAsync(int varianteId)
        => await _context.ProductoPresentaciones
            .Where(x => x.VarianteId == varianteId && x.IsActive)
            .Include(x => x.TipoPresentacion)
            .Include(x => x.PresentacionPadre)
            .OrderBy(x => x.TipoPresentacion!.Orden)
            .ToListAsync();

    public async Task<ProductoPresentacion?> ObtenerPorIdAsync(int id)
        => await _context.ProductoPresentaciones
            .Include(x => x.TipoPresentacion)
            .Include(x => x.PresentacionPadre)
            .FirstOrDefaultAsync(x => x.Id == id);

    public async Task<bool> ExisteCombinacionAsync(int varianteId, int tipoPresentacionId, int? excludeId = null)
    {
        var query = _context.ProductoPresentaciones
            .Where(x => x.VarianteId == varianteId
                     && x.TipoPresentacionId == tipoPresentacionId
                     && x.IsActive);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<bool> ExisteDefaultReporteAsync(int varianteId, int? excludeId = null)
    {
        var query = _context.ProductoPresentaciones
            .Where(x => x.VarianteId == varianteId
                     && x.EsDefaultReporte
                     && x.IsActive);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<ProductoPresentacion> CrearAsync(ProductoPresentacion entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.CreatedByUsuarioId = usuarioId;
        entidad.CreatedAt = DateTime.UtcNow;
        entidad.IsActive = true;
        _context.ProductoPresentaciones.Add(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task<ProductoPresentacion> ActualizarAsync(ProductoPresentacion entidad)
    {
        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.UpdatedByUsuarioId = usuarioId;
        entidad.UpdatedAt = DateTime.UtcNow;
        _context.ProductoPresentaciones.Update(entidad);
        await _context.SaveChangesAsync();
        return entidad;
    }

    public async Task EliminarAsync(int id)
    {
        var entidad = await _context.ProductoPresentaciones.FindAsync(id);
        if (entidad == null) return;

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        entidad.DeletedByUsuarioId = usuarioId;
        entidad.DeletedAt = DateTime.UtcNow;
        entidad.IsActive = false;
        _context.ProductoPresentaciones.Update(entidad);
        await _context.SaveChangesAsync();
    }

    public async Task EliminarPorVarianteAsync(int varianteId)
    {
        var presentaciones = await _context.ProductoPresentaciones
            .Where(x => x.VarianteId == varianteId && x.IsActive)
            .ToListAsync();

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        foreach (var p in presentaciones)
        {
            p.IsActive = false;
            p.DeletedAt = DateTime.UtcNow;
            p.DeletedByUsuarioId = usuarioId;
        }

        await _context.SaveChangesAsync();
    }
}