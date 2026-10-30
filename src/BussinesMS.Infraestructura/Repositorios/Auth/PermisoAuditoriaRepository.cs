using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Dominio.Entidades.Auth;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BussinesMS.Infraestructura.Repositorios.Auth;

// No hereda RepositorioBase: PermisoAuditoria no es EntidadBase.
public class PermisoAuditoriaRepository : IPermisoAuditoriaRepository
{
    private readonly AuthDbContext _contexto;

    public PermisoAuditoriaRepository(AuthDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task RegistrarAsync(PermisoAuditoria registro)
    {
        var entry = _contexto.PermisoAuditorias.Add(registro);
        try
        {
            await _contexto.SaveChangesAsync();
        }
        catch
        {
            entry.State = EntityState.Detached;
            throw;
        }
    }

    public async Task<List<PermisoAuditoriaResumenDto>> ObtenerResumenAsync(PermisoAuditoriaFiltroDto filtro)
    {
        var query = _contexto.PermisoAuditorias.AsNoTracking().AsQueryable();

        if (filtro.Desde.HasValue)
            query = query.Where(p => p.Fecha >= filtro.Desde.Value);
        if (filtro.Hasta.HasValue)
            query = query.Where(p => p.Fecha <= filtro.Hasta.Value);
        if (filtro.UsuarioId.HasValue)
            query = query.Where(p => p.UsuarioId == filtro.UsuarioId.Value);
        if (!string.IsNullOrWhiteSpace(filtro.Modo))
            query = query.Where(p => p.Modo == filtro.Modo);

        return await query
            .GroupBy(p => new
            {
                p.UsuarioId,
                p.Username,
                p.SistemaId,
                p.Metodo,
                p.Endpoint,
                p.Menus,
                p.FlagRequerido,
                p.Motivo,
                p.Modo
            })
            .Select(g => new PermisoAuditoriaResumenDto
            {
                UsuarioId = g.Key.UsuarioId,
                Username = g.Key.Username,
                SistemaId = g.Key.SistemaId,
                Metodo = g.Key.Metodo,
                Endpoint = g.Key.Endpoint,
                Menus = g.Key.Menus,
                FlagRequerido = g.Key.FlagRequerido,
                Motivo = g.Key.Motivo,
                Modo = g.Key.Modo,
                Cantidad = g.Count(),
                PrimeraVezUtc = g.Min(p => p.Fecha),
                UltimaVezUtc = g.Max(p => p.Fecha),
                RutaEjemplo = g.Max(p => p.Ruta)
            })
            .OrderByDescending(r => r.UltimaVezUtc)
            .ToListAsync();
    }
}
