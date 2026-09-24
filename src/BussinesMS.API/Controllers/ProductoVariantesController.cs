using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
public class ProductoVariantesController : BaseController
{
    private readonly IProductoVarianteService _servicio;
    private readonly IVarianteStockService _stockServicio;

    public ProductoVariantesController(IProductoVarianteService servicio, IVarianteStockService stockServicio)
    {
        _servicio = servicio;
        _stockServicio = stockServicio;
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
        return resultado == null
            ? RespuestaError("Variante de producto no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpGet("codigo-barras/{codigoBarras}")]
    public async Task<IActionResult> ObtenerPorCodigoBarras(string codigoBarras)
    {
        var resultado = await _servicio.ObtenerPorCodigoBarrasAsync(codigoBarras);
        return resultado == null
            ? RespuestaError("Variante de producto no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpGet("stock")]
    public async Task<IActionResult> ObtenerStock([FromQuery] GenericPaginationQueryDto query)
    {
        var resultado = await _stockServicio.ObtenerStockAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("stock/{id}")]
    public async Task<IActionResult> ObtenerStockDetalle(int id)
    {
        var resultado = await _stockServicio.ObtenerStockDetalleAsync(id);
        return resultado == null
            ? RespuestaError("Variante de producto no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpGet("stock-pos")]
    public async Task<IActionResult> ObtenerStockPos([FromQuery] GenericPaginationQueryDto query, [FromQuery] int almacenId)
    {
        if (almacenId <= 0)
            return RespuestaError("El almacenId es requerido.", 400);

        var resultado = await _stockServicio.ObtenerStockPosAsync(query, almacenId);
        return RespuestaOk(resultado);
    }

    [HttpGet("CompraInfo/{id}")]
    public async Task<IActionResult> ObtenerCompraInfo(int id)
    {
        var resultado = await _servicio.ObtenerCompraInfoAsync(id);
        return resultado == null
            ? RespuestaError("Variante no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearProductoVarianteDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Variante de producto creada");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarProductoVarianteDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Variante de producto eliminada" });
    }
}