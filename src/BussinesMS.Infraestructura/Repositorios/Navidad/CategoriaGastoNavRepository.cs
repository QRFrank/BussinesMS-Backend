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
}
