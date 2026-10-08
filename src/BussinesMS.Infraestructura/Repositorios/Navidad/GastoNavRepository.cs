using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class GastoNavRepository : NavidadRepositorioBase<GastoNav>, IGastoNavRepository
{
    public GastoNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<GastoNav?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .Include(g => g.Categoria)
            .FirstOrDefaultAsync(g => g.Id == id);

    public async Task<bool> TieneGastosActivosPorCategoriaAsync(int categoriaId)
        => await _dbSet.AnyAsync(g => g.CategoriaId == categoriaId && g.IsActive);
}
