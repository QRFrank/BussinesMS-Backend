using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class PagoProveedorNavRepository : NavidadRepositorioBase<PagoProveedor>, IPagoProveedorNavRepository
{
    public PagoProveedorNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<PagoProveedor?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .Include(p => p.Proveedor)
            .Include(p => p.CodigoCliente)
            .FirstOrDefaultAsync(p => p.Id == id);
}
