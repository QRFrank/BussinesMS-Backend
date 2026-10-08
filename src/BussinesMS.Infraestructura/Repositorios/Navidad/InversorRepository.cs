using BussinesMS.Aplicacion.Interfaces.Navidad;
using BussinesMS.Aplicacion.Seguridad;
using BussinesMS.Dominio.Entidades.Navidad;
using BussinesMS.Dominio.Enums.Navidad;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Navidad;

public class InversorRepository : NavidadRepositorioBase<Inversor>, IInversorRepository
{
    public InversorRepository(NavidadDbContext contexto, ICurrentUserService currentUser) : base(contexto, currentUser)
    {
    }

    public async Task<bool> TieneAportesEnTemporadaAbiertaAsync(int inversorId)
        => await _contexto.Set<AporteCapital>()
            .AnyAsync(a => a.InversorId == inversorId
                && a.IsActive
                && a.Temporada != null
                && a.Temporada.Estado == EstadoTemporada.Abierta
                && a.Temporada.IsActive);
}
