using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/PagosInversor")]
[Produces("application/json")]
public class PagosInversorController : BaseController
{
    private readonly IPagoInversorService _servicio;

    public PagosInversorController(IPagoInversorService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] PagoInversorFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearPagoInversorDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Pago a inversor registrado");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Anular(int id)
    {
        await _servicio.AnularAsync(id);
        return RespuestaOk(new { mensaje = "Pago anulado" });
    }
}
