using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/CodigosCliente")]
[Produces("application/json")]
public class CodigosClienteController : BaseController
{
    private readonly ICodigoClienteService _servicio;

    public CodigosClienteController(ICodigoClienteService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] CodigoClienteFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Código de cliente no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCodigoClienteDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Código de cliente creado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarCodigoClienteDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Código de cliente actualizado exitosamente.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Código de cliente eliminado" });
    }
}
