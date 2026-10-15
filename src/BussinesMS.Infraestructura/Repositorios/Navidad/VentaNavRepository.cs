using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class VentaNavRepository : NavidadRepositorioBase<VentaNav>, IVentaNavRepository
{
    public VentaNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<VentaNav?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(v => v.Cliente)
            .Include(v => v.Vendedor)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Entregas)
                .ThenInclude(e => e.Detalles)
                    .ThenInclude(d => d.Producto)
            .Include(v => v.Cobros)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<VentaNav?> ObtenerParaEdicionAsync(int id)
        => await _dbSet
            .AsSplitQuery()
            .Include(v => v.Cliente)
            .Include(v => v.Detalles)
            .Include(v => v.Entregas)
                .ThenInclude(e => e.Detalles)
            .Include(v => v.Cobros)
            .FirstOrDefaultAsync(v => v.Id == id);

    public async Task<int> ObtenerSiguienteNroAsync(int temporadaId)
        => (await _dbSet
            .Where(v => v.TemporadaId == temporadaId)
            .Select(v => (int?)v.NroVenta)
            .MaxAsync() ?? 0) + 1;
}

public class EntregaVentaNavRepository : NavidadRepositorioBase<EntregaVenta>, IEntregaVentaNavRepository
{
    public EntregaVentaNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }
}

public class CobroVentaNavRepository : NavidadRepositorioBase<CobroVenta>, ICobroVentaNavRepository
{
    public CobroVentaNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }
}

public class ConsumoNavRepository : NavidadRepositorioBase<ConsumoNav>, IConsumoNavRepository
{
    public ConsumoNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<ConsumoNav?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(c => c.Detalles)
                .ThenInclude(d => d.Producto)
            .FirstOrDefaultAsync(c => c.Id == id);
}
