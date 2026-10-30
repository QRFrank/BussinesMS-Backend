using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Compras")]
[Produces("application/json")]
[Menu(137)]
public class ComprasNavController : BaseController
{
    private readonly ICompraNavService _servicio;

    public ComprasNavController(ICompraNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] CompraNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Compra no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCompraNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Compra registrada");
    }

    [Menu(137, Flag = FlagMenu.Eliminar)]
    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        await _servicio.AnularAsync(id);
        return RespuestaOk(new { mensaje = "Compra anulada" }, "Compra anulada");
    }
}
