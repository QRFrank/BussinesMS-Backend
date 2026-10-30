using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
[SoloAdmin]
public class ProductoPresentacionesController : BaseController
{
    private readonly IProductoPresentacionService _servicio;

    public ProductoPresentacionesController(IProductoPresentacionService servicio)
    {
        _servicio = servicio;
    }

    [Menu(8, 7, 3, 6)]
    [HttpGet("variante/{varianteId}")]
    public async Task<IActionResult> ObtenerPorVariante(int varianteId)
    {
        var resultado = await _servicio.ObtenerPorVarianteAsync(varianteId);
        return RespuestaOk(resultado);
    }

    [Menu(8, 7, 3, 6)]
    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Presentación no encontrada", 404)
            : RespuestaOk(resultado);
    }

    // GET api/Sistema/ProductoPresentaciones/stock/5?stockEnUnidades=1083
    [Menu(8, 7, 3, 6)]
    [HttpGet("stock/{varianteId}")]
    public async Task<IActionResult> ConvertirStock(int varianteId, [FromQuery] int stockEnUnidades)
    {
        var resultado = await _servicio.ConvertirStockAsync(varianteId, stockEnUnidades);
        return RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearProductoPresentacionDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Presentación creada");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarProductoPresentacionDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Presentación eliminada" });
    }
}