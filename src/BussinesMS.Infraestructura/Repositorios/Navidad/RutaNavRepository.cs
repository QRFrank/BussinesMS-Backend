using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class SalidaRutaNavRepository : NavidadRepositorioBase<SalidaRuta>, ISalidaRutaNavRepository
{
    public SalidaRutaNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<SalidaRuta?> ObtenerParaEdicionAsync(int id)
        => await _dbSet
            .AsSplitQuery()
            .Include(s => s.Cargas)
                .ThenInclude(c => c.Detalles)
            .Include(s => s.Devoluciones)
                .ThenInclude(d => d.Detalles)
            .FirstOrDefaultAsync(s => s.Id == id);

    public async Task<SalidaRuta?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .AsSplitQuery()
            .Include(s => s.Vendedor)
            .Include(s => s.Cargas)
                .ThenInclude(c => c.Detalles)
                    .ThenInclude(d => d.Producto)
            .Include(s => s.Devoluciones)
                .ThenInclude(d => d.Detalles)
                    .ThenInclude(x => x.Producto)
            .FirstOrDefaultAsync(s => s.Id == id);
}

public class SalidaRutaCargaNavRepository : NavidadRepositorioBase<SalidaRutaCarga>, ISalidaRutaCargaNavRepository
{
    public SalidaRutaCargaNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }
}

public class DevolucionRutaNavRepository : NavidadRepositorioBase<DevolucionRuta>, IDevolucionRutaNavRepository
{
    public DevolucionRutaNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }
}

public class AbonoVendedorNavRepository : NavidadRepositorioBase<AbonoVendedor>, IAbonoVendedorNavRepository
{
    public AbonoVendedorNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<AbonoVendedor?> ObtenerParaEdicionAsync(int id)
        => await _dbSet
            .Include(a => a.Aplicaciones)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<AbonoVendedor?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .AsNoTracking()
            .Include(a => a.Vendedor)
            .Include(a => a.Aplicaciones)
            .FirstOrDefaultAsync(a => a.Id == id);
}
