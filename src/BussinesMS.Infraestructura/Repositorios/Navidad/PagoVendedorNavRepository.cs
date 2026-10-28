using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class PagoVendedorNavRepository : NavidadRepositorioBase<PagoVendedor>, IPagoVendedorNavRepository
{
    public PagoVendedorNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<PagoVendedor?> ObtenerParaEdicionAsync(int id)
        => await _dbSet
            .Include(p => p.Detalles)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<PagoVendedor?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .Include(p => p.Vendedor)
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(p => p.Id == id);
}
