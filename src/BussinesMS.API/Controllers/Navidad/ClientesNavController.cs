using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Clientes")]
[Produces("application/json")]
[Menu(114)]
public class ClientesNavController : BaseController
{
    private readonly IClienteNavService _servicio;

    public ClientesNavController(IClienteNavService servicio)
    {
        _servicio = servicio;
    }

    [SoloSesion]
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GenericPaginationQueryDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [SoloSesion]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Cliente no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [Menu(114, 151, 152, 154, 155)]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearClienteNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Cliente creado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarClienteNavDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Cliente actualizado exitosamente.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Cliente eliminado" });
    }
}
