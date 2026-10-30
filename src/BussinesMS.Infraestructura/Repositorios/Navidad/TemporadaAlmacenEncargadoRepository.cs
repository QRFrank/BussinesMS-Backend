using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class TemporadaAlmacenEncargadoRepository : NavidadRepositorioBase<TemporadaAlmacenEncargado>, ITemporadaAlmacenEncargadoRepository
{
    public TemporadaAlmacenEncargadoRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<List<TemporadaAlmacenEncargado>> ObtenerPorTemporadaAsync(int temporadaId)
        => await _dbSet
            .AsNoTracking()
            .Where(x => x.TemporadaId == temporadaId)
            .ToListAsync();

    public async Task<List<int>> ObtenerAlmacenIdsDeUsuarioAsync(int temporadaId, int usuarioId)
        => await _dbSet
            .AsNoTracking()
            .Where(x => x.TemporadaId == temporadaId && x.UsuarioId == usuarioId)
            .Select(x => x.AlmacenId)
            .ToListAsync();

    public async Task ReemplazarAsync(int temporadaId, int almacenId, IEnumerable<int> usuarioIds)
    {
        var actuales = await _dbSet.Where(x => x.TemporadaId == temporadaId && x.AlmacenId == almacenId).ToListAsync();
        _dbSet.RemoveRange(actuales);
        // Se guarda el borrado antes de insertar para no chocar con el índice único
        await _contexto.SaveChangesAsync();

        var adminId = _currentUser.ObtenerUsuarioIdRequerido();
        var ahora = DateTime.UtcNow;
        foreach (var usuarioId in usuarioIds.Distinct())
        {
            _dbSet.Add(new TemporadaAlmacenEncargado
            {
                TemporadaId = temporadaId,
                AlmacenId = almacenId,
                UsuarioId = usuarioId,
                CreatedAt = ahora,
                CreatedByUsuarioId = adminId
            });
        }

        await _contexto.SaveChangesAsync();
    }
}
