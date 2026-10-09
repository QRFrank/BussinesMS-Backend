using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class PedidoNavRepository : NavidadRepositorioBase<Pedido>, IPedidoNavRepository
{
    public PedidoNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<Pedido?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(p => p.Proveedor)
            .Include(p => p.CodigoCliente)
            .Include(p => p.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task ReemplazarDetallesAsync(int pedidoId, IEnumerable<PedidoDetalle> nuevos)
    {
        var detalles = _contexto.Set<PedidoDetalle>();

        // Borrado físico primero (índice único PedidoId+ProductoId)
        await detalles.Where(d => d.PedidoId == pedidoId).ExecuteDeleteAsync();

        foreach (var detalle in nuevos)
        {
            detalle.PedidoId = pedidoId;
            await detalles.AddAsync(detalle);
        }
        await _contexto.SaveChangesAsync();
    }
}
