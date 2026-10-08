using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class CodigoClienteRepository : NavidadRepositorioBase<CodigoCliente>, ICodigoClienteRepository
{
    public CodigoClienteRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<CodigoCliente?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .Include(c => c.Proveedor)
            .FirstOrDefaultAsync(c => c.Id == id);

    public async Task<bool> ExisteCodigoAsync(int proveedorId, string codigo, int? excluirId = null)
    {
        var cod = codigo.Trim().ToLower();
        return await _dbSet.AnyAsync(c => c.ProveedorId == proveedorId
            && c.IsActive
            && c.Codigo.ToLower() == cod
            && (!excluirId.HasValue || c.Id != excluirId.Value));
    }
}
