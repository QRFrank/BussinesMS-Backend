using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class TemporadaRepository : NavidadRepositorioBase<Temporada>, ITemporadaRepository
{
    public TemporadaRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<Temporada?> ObtenerAbiertaAsync()
        => await _dbSet.FirstOrDefaultAsync(x => x.Estado == EstadoTemporada.Abierta && x.IsActive);

    public async Task<bool> ExisteAbiertaAsync(int? excludeId = null)
    {
        var query = _dbSet.Where(x => x.Estado == EstadoTemporada.Abierta && x.IsActive);
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<List<TemporadaAlmacenConteo>> ObtenerAlmacenesConteoAsync(IEnumerable<int> temporadaIds)
    {
        var ids = temporadaIds.Distinct().ToList();
        if (ids.Count == 0) return new List<TemporadaAlmacenConteo>();

        return await _contexto.Set<TemporadaAlmacenConteo>()
            .AsNoTracking()
            .Where(x => ids.Contains(x.TemporadaId))
            .ToListAsync();
    }

    public async Task ReemplazarAlmacenesConteoAsync(int temporadaId, IEnumerable<int> almacenIds)
    {
        var set = _contexto.Set<TemporadaAlmacenConteo>();
        var actuales = await set.Where(x => x.TemporadaId == temporadaId).ToListAsync();
        set.RemoveRange(actuales);

        var usuarioId = _currentUser.GetUsuarioId() ?? 1;
        var ahora = DateTime.UtcNow;
        foreach (var almacenId in almacenIds.Distinct())
        {
            set.Add(new TemporadaAlmacenConteo
            {
                TemporadaId = temporadaId,
                AlmacenId = almacenId,
                CreatedAt = ahora,
                CreatedByUsuarioId = usuarioId
            });
        }

        await _contexto.SaveChangesAsync();
    }

    public async Task<bool> ExisteAlmacenConteoAsync(int temporadaId, int almacenId)
        => await _contexto.Set<TemporadaAlmacenConteo>()
            .AnyAsync(x => x.TemporadaId == temporadaId && x.AlmacenId == almacenId);
}
