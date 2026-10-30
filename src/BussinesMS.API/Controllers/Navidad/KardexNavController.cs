using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Kardex")]
[Produces("application/json")]
[Menu(141)]
public class KardexNavController : BaseController
{
    private readonly IKardexNavService _servicio;

    public KardexNavController(IKardexNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] KardexNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerAsync(query);
        return RespuestaOk(resultado);
    }
}
