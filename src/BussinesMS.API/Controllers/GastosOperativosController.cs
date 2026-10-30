using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
[Menu(29)]
public class GastosOperativosController : BaseController
{
    private readonly IGastoOperativoService _servicio;

    public GastosOperativosController(IGastoOperativoService servicio)
    {
        _servicio = servicio;
    }

    [Menu(29, 3)]
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GastoOperativoFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [Menu(29, 3)]
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Gasto operativo no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [Menu(29, 3)]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearGastoOperativoDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Gasto operativo registrado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarGastoOperativoDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Anular(int id)
    {
        await _servicio.AnularAsync(id);
        return RespuestaOk(new { mensaje = "Gasto operativo anulado" });
    }
}
