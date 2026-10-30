using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
[Menu(6)]
public class MovimientosInventarioController : BaseController
{
    private readonly IMovimientoInventarioService _servicio;

    public MovimientosInventarioController(IMovimientoInventarioService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GenericPaginationQueryDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("filtrar")]
    public async Task<IActionResult> ObtenerFiltrados(
        [FromQuery] MovimientoInventarioFiltroDto filtro,
        [FromQuery] GenericPaginationQueryDto query)
    {
        var resultado = await _servicio.ObtenerFiltradosAsync(filtro, query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Movimiento de inventario no encontrado", 404)
            : RespuestaOk(resultado);
    }
}
