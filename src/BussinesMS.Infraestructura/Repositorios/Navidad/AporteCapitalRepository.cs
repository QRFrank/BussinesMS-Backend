using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class AporteCapitalRepository : NavidadRepositorioBase<AporteCapital>, IAporteCapitalRepository
{
    public AporteCapitalRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<AporteCapital?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .Include(a => a.Inversor)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<List<AporteCapital>> ObtenerActivosConPagosPorTemporadaAsync(int temporadaId)
        => await _dbSet
            .AsNoTracking()
            .Include(a => a.Inversor)
            .Include(a => a.Pagos)
            .Where(a => a.TemporadaId == temporadaId && a.IsActive)
            .ToListAsync();
}
