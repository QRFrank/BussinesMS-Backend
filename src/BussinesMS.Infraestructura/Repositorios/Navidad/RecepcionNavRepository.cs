using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class RecepcionNavRepository : NavidadRepositorioBase<Recepcion>, IRecepcionNavRepository
{
    public RecepcionNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<Recepcion?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(r => r.Proveedor)
            .Include(r => r.CodigoCliente)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(r => r.Detalles)
                .ThenInclude(d => d.Distribuciones)
            .FirstOrDefaultAsync(r => r.Id == id);
}
