using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class VendedorNavRepository : NavidadRepositorioBase<Vendedor>, IVendedorNavRepository
{
    public VendedorNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<bool> ExisteUsuarioAsync(int temporadaId, int usuarioId, int? excluirId = null)
        => await _dbSet.AnyAsync(v => v.TemporadaId == temporadaId
            && v.UsuarioId == usuarioId
            && v.IsActive
            && (!excluirId.HasValue || v.Id != excluirId.Value));

    public async Task<List<int>> ObtenerUsuarioIdsActivosAsync(int temporadaId)
        => await _dbSet
            .Where(v => v.TemporadaId == temporadaId && v.IsActive)
            .Select(v => v.UsuarioId)
            .Distinct()
            .ToListAsync();

    public async Task<List<Vendedor>> ObtenerActivosPorTemporadaAsync(int temporadaId)
        => await _dbSet
            .AsNoTracking()
            .Where(v => v.TemporadaId == temporadaId && v.IsActive)
            .OrderBy(v => v.Id)
            .ToListAsync();
}
