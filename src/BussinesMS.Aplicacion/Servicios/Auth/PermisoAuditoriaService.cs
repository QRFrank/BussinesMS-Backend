using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.Interfaces.Auth;
using Microsoft.Extensions.Logging;

namespace BussinesMS.Aplicacion.Servicios.Auth;

public class PermisoAuditoriaService : IPermisoAuditoriaService
{
    private readonly IPermisoAuditoriaRepository _repositorio;
    private readonly ILogger<PermisoAuditoriaService> _logger;

    public PermisoAuditoriaService(IPermisoAuditoriaRepository repositorio, ILogger<PermisoAuditoriaService> logger)
    {
        _repositorio = repositorio;
        _logger = logger;
    }

    public async Task<List<PermisoAuditoriaResumenDto>> ObtenerResumenAsync(PermisoAuditoriaFiltroDto filtro)
    {
        try
        {
            return await _repositorio.ObtenerResumenAsync(filtro);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el resumen de auditoría de permisos");
            throw;
        }
    }
}
