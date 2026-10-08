using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Gastos")]
[Produces("application/json")]
public class GastosNavController : BaseController
{
    private readonly IGastoNavService _servicio;

    public GastosNavController(IGastoNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GastoNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Gasto no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearGastoNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Gasto creado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarGastoNavDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Gasto actualizado exitosamente.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Gasto eliminado" });
    }
}
