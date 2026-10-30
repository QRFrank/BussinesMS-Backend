using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class CategoriaGastoNavRepository : NavidadRepositorioBase<CategoriaGastoNav>, ICategoriaGastoNavRepository
{
    public CategoriaGastoNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    // Sin filtro IsActive: el índice único de Nombre en BD cubre también a los inactivos
    public async Task<bool> ExisteNombreAsync(string nombre, int? excludeId = null)
    {
        var query = _dbSet.Where(x => x.Nombre.ToLower() == nombre.ToLower());
        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);
        return await query.AnyAsync();
    }

    public async Task<CategoriaGastoNav?> ObtenerPorNombreAsync(string nombre)
        => await _dbSet.FirstOrDefaultAsync(x => x.Nombre.ToLower() == nombre.ToLower());

    public IQueryable<CategoriaGastoNav> AsQueryablePorTemporada(int temporadaId)
        => _contexto.Set<CategoriaGastoTemporadaNav>()
            .Where(v => v.TemporadaId == temporadaId && v.IsActive)
            .Select(v => v.CategoriaGasto!);

    public async Task<bool> EstaVinculadaAsync(int categoriaId, int temporadaId)
        => await _contexto.Set<CategoriaGastoTemporadaNav>()
            .AnyAsync(v => v.CategoriaGastoId == categoriaId && v.TemporadaId == temporadaId && v.IsActive);

    public async Task VincularAsync(int categoriaId, int temporadaId)
    {
        var set = _contexto.Set<CategoriaGastoTemporadaNav>();
        var existente = await set.FirstOrDefaultAsync(v => v.CategoriaGastoId == categoriaId && v.TemporadaId == temporadaId);
        if (existente != null)
        {
            if (!existente.IsActive)
            {
                existente.IsActive = true;
                await _contexto.SaveChangesAsync();
            }
            return;
        }

        set.Add(new CategoriaGastoTemporadaNav
        {
            CategoriaGastoId = categoriaId,
            TemporadaId = temporadaId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUsuarioId = _currentUser.ObtenerUsuarioIdRequerido()
        });
        await _contexto.SaveChangesAsync();
    }

    public async Task CopiarVinculosAsync(int temporadaOrigenId, int temporadaDestinoId)
    {
        var set = _contexto.Set<CategoriaGastoTemporadaNav>();
        var yaLigadas = await set.Where(v => v.TemporadaId == temporadaDestinoId)
            .Select(v => v.CategoriaGastoId).ToListAsync();
        var categoriaIds = await set
            .Where(v => v.TemporadaId == temporadaOrigenId && v.IsActive && v.CategoriaGasto!.IsActive)
            .Select(v => v.CategoriaGastoId)
            .ToListAsync();

        var usuarioId = _currentUser.ObtenerUsuarioIdRequerido();
        var ahora = DateTime.UtcNow;
        foreach (var id in categoriaIds.Except(yaLigadas))
        {
            set.Add(new CategoriaGastoTemporadaNav
            {
                CategoriaGastoId = id,
                TemporadaId = temporadaDestinoId,
                CreatedAt = ahora,
                CreatedByUsuarioId = usuarioId
            });
        }
        await _contexto.SaveChangesAsync();
    }
}
