using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class VentaClienteRutaNavRepository : NavidadRepositorioBase<VentaClienteRuta>, IVentaClienteRutaNavRepository
{
    public VentaClienteRutaNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<VentaClienteRuta?> ObtenerParaEdicionAsync(int id)
        => await _dbSet
            .AsSplitQuery()
            .Include(v => v.Detalles)
            .Include(v => v.Cobros)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<VentaClienteRuta?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(v => v.Cliente)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Cobros)
            .FirstOrDefaultAsync(v => v.Id == id);
}
