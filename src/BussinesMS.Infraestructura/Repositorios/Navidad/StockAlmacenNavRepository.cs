using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class StockAlmacenNavRepository : NavidadRepositorioBase<StockAlmacen>, IStockAlmacenNavRepository
{
    public StockAlmacenNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<StockAlmacen?> ObtenerAsync(int productoId, int almacenId)
        => await _dbSet.FirstOrDefaultAsync(x => x.ProductoId == productoId && x.AlmacenId == almacenId);
}
