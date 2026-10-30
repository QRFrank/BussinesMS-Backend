using Microsoft.AspNetCore.Mvc;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[SoloAdmin]
public class RolesController : BaseController
{
    private readonly IRolService _servicio;

    public RolesController(IRolService servicio)
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
        return resultado == null ? RespuestaError("Rol no encontrado", 404) : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearRolDto rol)
    {
        var resultado = await _servicio.CrearAsync(rol);
        return RespuestaOk(resultado, "Rol creado");
    }

    [HttpPut("{id}/menuIds")]
    public async Task<IActionResult> ActualizarMenuIds(int id, [FromBody] List<int> menuIds)
    {
        var resultado = await _servicio.ActualizarMenuIdsAsync(id, menuIds);
        return RespuestaOk(resultado, "MenuIds actualizados");
    }
}