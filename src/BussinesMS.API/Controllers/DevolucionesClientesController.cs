using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
[Menu(4, 3)]
public class DevolucionesClientesController : BaseController
{
    private readonly IDevolucionClienteService _servicio;

    public DevolucionesClientesController(IDevolucionClienteService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Devolución no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearDevolucionClienteDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Devolución registrada");
    }

    [HttpPost("cambio")]
    public async Task<IActionResult> RealizarCambio([FromBody] RealizarCambioDto dto)
    {
        await _servicio.RealizarCambioAsync(dto);
        return RespuestaOk("Cambio realizado");
    }

    [Menu(4, 3, Flag = FlagMenu.Eliminar)]
    [HttpPost("baja")]
    public async Task<IActionResult> DarDeBaja([FromBody] DarDeBajaDevolucionDto dto)
    {
        await _servicio.DarDeBajaAsync(dto);
        return RespuestaOk("Devolución dada de baja");
    }
}
