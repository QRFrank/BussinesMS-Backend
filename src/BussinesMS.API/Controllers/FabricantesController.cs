using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
[Menu(10)]
public class FabricantesController : BaseController
{
    private readonly IFabricanteService _servicio;

    public FabricantesController(IFabricanteService servicio)
    {
        _servicio = servicio;
    }

    [Menu(10, 8)]
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GenericPaginationQueryDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [Menu(10, 8)]
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null ? RespuestaError("Fabricante no encontrado", 404) : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearFabricanteDto dto)
    {
        var (entidad, fueReactivada) = await _servicio.CrearAsync(dto);

        return fueReactivada
            ? RespuestaOk(new { mensaje = $"El fabricante '{entidad.Nombre}' estaba desactivado y fue reactivado.", data = entidad })
            : StatusCode(201, new
            {
                Success = true,
                Message = "Fabricante creado",
                Data = entidad
            });
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarFabricanteDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Fabricante eliminado" });
    }
}