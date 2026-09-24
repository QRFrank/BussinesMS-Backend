using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
public class SesionesCajaController : BaseController
{
    private readonly ISesionCajaService _servicio;

    public SesionesCajaController(ISesionCajaService servicio)
    {
        _servicio = servicio;
    }

    [HttpPost("abrir")]
    public async Task<IActionResult> AbrirCaja([FromBody] CrearSesionCajaDto dto)
    {
        var resultado = await _servicio.AbrirCajaAsync(dto);
        return RespuestaCreado(resultado, "Sesión de caja abierta");
    }

    [HttpPost("{id}/cerrar")]
    public async Task<IActionResult> CerrarCaja(int id, [FromBody] CerrarSesionCajaDto dto)
    {
        var resultado = await _servicio.CerrarCajaAsync(id, dto);
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
