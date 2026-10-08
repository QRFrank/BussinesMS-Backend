using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class ProductoPresentacionNavRepository : NavidadRepositorioBase<ProductoPresentacion>, IProductoPresentacionNavRepository
{
    public ProductoPresentacionNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<List<ProductoPresentacion>> ObtenerActivasPorProductoAsync(int productoId)
        => await _dbSet
            .Where(x => x.ProductoId == productoId && x.IsActive)
            .OrderBy(x => x.Unidades)
            .ToListAsync();
}
