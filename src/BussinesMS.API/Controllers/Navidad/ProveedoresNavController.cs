using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Proveedores")]
[Produces("application/json")]
public class ProveedoresNavController : BaseController
{
    private readonly IProveedorNavService _servicio;

    public ProveedoresNavController(IProveedorNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] ProveedorNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Proveedor no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearProveedorNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Proveedor creado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarProveedorNavDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Proveedor actualizado exitosamente.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Proveedor eliminado" });
    }
}
