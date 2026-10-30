using BussinesMS.API.Autorizacion;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.Interfaces.Auth;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers;

// Permisos fase 2: decisiones que habrían sido (o fueron) 403, agrupadas por usuario y endpoint. Solo admin.
[ApiController]
[Route("api/Permisos/Auditoria")]
[Produces("application/json")]
[SoloAdmin]
public class PermisosAuditoriaController : BaseController
{
    private readonly IPermisoAuditoriaService _servicio;

    public PermisosAuditoriaController(IPermisoAuditoriaService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerResumen([FromQuery] PermisoAuditoriaFiltroDto filtro)
    {
        var resultado = await _servicio.ObtenerResumenAsync(filtro);
        return RespuestaOk(resultado);
    }
}
