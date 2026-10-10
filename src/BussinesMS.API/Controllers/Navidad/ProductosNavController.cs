using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Productos")]
[Produces("application/json")]
public class ProductosNavController : BaseController
{
    private readonly IProductoNavService _servicio;

    public ProductosNavController(IProductoNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] ProductoNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Producto no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearProductoNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Producto creado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarProductoNavDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Producto actualizado exitosamente.");
    }

    [HttpPut("precios")]
    public async Task<IActionResult> ActualizarPrecios([FromBody] List<ActualizarPrecioProductoNavDto> items)
    {
        var resultado = await _servicio.ActualizarPreciosAsync(items);
        return RespuestaOk(resultado, "Precios actualizados");
    }

    [HttpPatch("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, [FromBody] CambiarEstadoProductoNavDto dto)
    {
        var resultado = await _servicio.CambiarEstadoAsync(id, dto.IsActive);
        return RespuestaOk(resultado, dto.IsActive ? "Producto activado" : "Producto desactivado");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Producto eliminado" });
    }
}
