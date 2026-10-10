using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class CompraNavRepository : NavidadRepositorioBase<Compra>, ICompraNavRepository
{
    public CompraNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<Compra?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Proveedor)
            .Include(c => c.Pagos)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Distribuciones)
            .FirstOrDefaultAsync(c => c.Id == id);
}
