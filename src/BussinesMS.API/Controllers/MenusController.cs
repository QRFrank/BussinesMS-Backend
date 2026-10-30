using Microsoft.AspNetCore.Mvc;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[SoloAdmin]
public class MenusController : BaseController
{
    private readonly IMenuService _servicio;

    public MenusController(IMenuService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GenericPaginationQueryDto query, [FromQuery] int? sistemaId = null)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query, sistemaId);
        return RespuestaOk(resultado);
    }

    [HttpGet("activos")]
    public async Task<IActionResult> ObtenerActivos([FromQuery] int? sistemaId = null)
    {
        var resultado = await _servicio.ObtenerActivosAsync(sistemaId);
        return RespuestaOk(resultado);
    }

    [HttpGet("arbol")]
    public async Task<IActionResult> ObtenerArbol([FromQuery] int? sistemaId = null)
    {
        var resultado = await _servicio.ObtenerArbolAsync(sistemaId);
        return RespuestaOk(new { menus = resultado });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null ? RespuestaError("Menú no encontrado", 404) : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearMenuDto menu)
    {
        var resultado = await _servicio.CrearAsync(menu);
        return RespuestaOk(resultado, "Menú creado");
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] MenuDto menu)
    {
        if (id != menu.Id)
            return RespuestaError("ID mismatch", 400);

        var resultado = await _servicio.ActualizarAsync(menu);
        return RespuestaOk(resultado, "Menú actualizado");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk("Menú eliminado");
    }
}
