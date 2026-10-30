using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Stock")]
[Produces("application/json")]
[Menu(136, 142, 153, 155)]
public class StockNavController : BaseController
{
    private readonly IStockNavService _servicio;

    public StockNavController(IStockNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerStock([FromQuery] StockNavFiltroDto filtro)
    {
        var resultado = await _servicio.ObtenerStockAsync(filtro);
        return RespuestaOk(resultado);
    }
}
