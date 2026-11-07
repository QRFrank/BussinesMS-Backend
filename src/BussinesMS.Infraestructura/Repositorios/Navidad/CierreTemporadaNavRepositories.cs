using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class IncobrableNavRepository : NavidadRepositorioBase<IncobrableNav>, IIncobrableNavRepository
{
    public IncobrableNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<IncobrableNav?> ObtenerParaEdicionAsync(int id)
        => await _dbSet
            .Include(i => i.Aplicaciones)
            .FirstOrDefaultAsync(i => i.Id == id);
}

public class CierreTemporadaNavRepository : NavidadRepositorioBase<CierreTemporadaNav>, ICierreTemporadaNavRepository
{
    public CierreTemporadaNavRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<CierreTemporadaNav?> ObtenerPorTemporadaAsync(int temporadaId)
        => await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.TemporadaId == temporadaId && c.IsActive);
}
