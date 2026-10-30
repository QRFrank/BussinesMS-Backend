using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
[Menu(4, 3)]
public class VentasController : BaseController
{
    private readonly IVentaService _servicio;

    public VentasController(IVentaService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] VentaFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Venta no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [Menu(3)]
    [HttpGet("ProductoVariantes/{id}")]
    public async Task<IActionResult> ObtenerVarianteVenta(int id)
    {
        if (id <= 0)
            return RespuestaError("El id de la variante es requerido.", 400);

        var resultado = await _servicio.ObtenerVarianteVentaAsync(id);
        return resultado == null
            ? RespuestaError("Variante de producto no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [Menu(3)]
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearVentaDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Venta registrada");
    }

    [Menu(4)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Venta anulada y stock devuelto" });
    }
}
