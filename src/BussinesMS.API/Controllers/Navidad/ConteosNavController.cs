using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Conteos")]
[Produces("application/json")]
public class ConteosNavController : BaseController
{
    private readonly IConteoNavService _servicio;

    public ConteosNavController(IConteoNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] ConteoNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("plantilla")]
    public async Task<IActionResult> ObtenerPlantilla([FromQuery] int almacenId, [FromQuery] int tipo)
    {
        var resultado = await _servicio.ObtenerPlantillaAsync(almacenId, tipo);
        return RespuestaOk(resultado);
    }

    [HttpGet("estado")]
    public async Task<IActionResult> ObtenerEstado([FromQuery] int almacenId, [FromQuery] DateTime? fecha)
    {
        var resultado = await _servicio.ObtenerEstadoAsync(almacenId, fecha);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Conteo no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearConteoNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Conteo registrado");
    }
}
