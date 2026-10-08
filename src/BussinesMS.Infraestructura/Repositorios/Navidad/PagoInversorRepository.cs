using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class PagoInversorRepository : NavidadRepositorioBase<PagoInversor>, IPagoInversorRepository
{
    public PagoInversorRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<PagoInversor?> ObtenerConDetallesAsync(int id)
        => await _dbSet
            .Include(p => p.AporteCapital)
                .ThenInclude(a => a!.Inversor)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<decimal> SumarPagosActivosAsync(int aporteCapitalId, TipoPagoInversor tipo)
        => await _dbSet
            .Where(p => p.AporteCapitalId == aporteCapitalId && p.Tipo == tipo && p.IsActive)
            .SumAsync(p => (decimal?)p.Monto) ?? 0m;

    public async Task<bool> TienePagosActivosAsync(int aporteCapitalId)
        => await _dbSet.AnyAsync(p => p.AporteCapitalId == aporteCapitalId && p.IsActive);
}
