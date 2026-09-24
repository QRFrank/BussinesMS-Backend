using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
public class ClientesController : BaseController
{
    private readonly IClienteService _servicio;

    public ClientesController(IClienteService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GenericPaginationQueryDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Cliente no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearClienteDto dto)
    {
        var (entidad, fueReactivada) = await _servicio.CrearAsync(dto);

        return fueReactivada
            ? RespuestaOk(new { mensaje = $"El cliente '{entidad.Nombre}' estaba desactivado y fue reactivado.", data = entidad })
            : RespuestaCreado(entidad, "Cliente creado exitosamente.");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarClienteDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Cliente actualizado exitosamente.");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Cliente eliminado" });
    }
}
