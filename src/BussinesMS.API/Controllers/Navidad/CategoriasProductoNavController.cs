using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/CategoriasProducto")]
[Produces("application/json")]
public class CategoriasProductoNavController : BaseController
{
    private readonly ICategoriaProductoNavService _servicio;

    public CategoriasProductoNavController(ICategoriaProductoNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GenericPaginationQueryDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Categoría de producto no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearCategoriaProductoNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Categoría de producto creada");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarCategoriaProductoNavDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Categoría de producto actualizada exitosamente.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Categoría de producto eliminada" });
    }
}
