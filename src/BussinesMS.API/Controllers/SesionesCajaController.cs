using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
[Menu(16, 17, 30, 3)]
public class SesionesCajaController : BaseController
{
    private readonly ISesionCajaService _servicio;

    public SesionesCajaController(ISesionCajaService servicio)
    {
        _servicio = servicio;
    }

    [Menu(17, 3)]
    [HttpPost("abrir")]
    public async Task<IActionResult> AbrirCaja([FromBody] CrearSesionCajaDto dto)
    {
        var resultado = await _servicio.AbrirCajaAsync(dto);
        return RespuestaCreado(resultado, "Sesión de caja abierta");
    }

    [Menu(17, 3)]
    [HttpPost("{id}/cerrar")]
    public async Task<IActionResult> CerrarCaja(int id, [FromBody] CerrarSesionCajaDto dto)
    {
        var resultado = await _servicio.CerrarCajaAsync(id, dto);
        return RespuestaOk(resultado);
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] SesionCajaFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Sesión de caja no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpGet("abierta")]
    public async Task<IActionResult> ObtenerAbierta([FromQuery] int usuarioId, [FromQuery] int almacenId)
    {
        var resultado = await _servicio.ObtenerAbiertaAsync(usuarioId, almacenId);
        return resultado == null
            ? RespuestaError("No hay sesión abierta para este usuario en este almacén", 404)
            : RespuestaOk(resultado);
    }
}
