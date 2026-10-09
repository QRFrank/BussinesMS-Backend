using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Abastecimiento")]
[Produces("application/json")]
public class AbastecimientoNavController : BaseController
{
    private readonly IAbastecimientoNavService _servicio;

    public AbastecimientoNavController(IAbastecimientoNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("faltantes")]
    public async Task<IActionResult> ObtenerFaltantes([FromQuery] AbastecimientoNavFiltroDto filtro)
    {
        var resultado = await _servicio.ObtenerFaltantesAsync(filtro);
        return RespuestaOk(resultado);
    }

    [HttpGet("deudas")]
    public async Task<IActionResult> ObtenerDeudas([FromQuery] AbastecimientoNavFiltroDto filtro)
    {
        var resultado = await _servicio.ObtenerDeudasAsync(filtro);
        return RespuestaOk(resultado);
    }
}
