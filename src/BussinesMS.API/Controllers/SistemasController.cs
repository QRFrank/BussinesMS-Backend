using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[SoloAdmin]
public class SistemasController : BaseController
{
    private readonly ISistemaService _servicio;

    public SistemasController(ISistemaService servicio)
    {
        _servicio = servicio;
    }

    [AllowAnonymous] // la pantalla de login lo carga antes de tener sesion (solo id y nombre)
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
        return resultado == null ? RespuestaError("Sistema no encontrado", 404) : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearSistemaDto sistema)
    {
        var resultado = await _servicio.CrearAsync(sistema);
        return RespuestaOk(resultado, "Sistema creado");
    }
}