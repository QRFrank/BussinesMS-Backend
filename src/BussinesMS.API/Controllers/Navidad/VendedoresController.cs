using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Vendedores")]
[Produces("application/json")]
public class VendedoresController : BaseController
{
    private readonly IVendedorNavService _servicio;

    public VendedoresController(IVendedorNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] VendedorFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    // Ruta literal: no choca con {id:int}
    [HttpGet("usuarios-disponibles")]
    public async Task<IActionResult> ObtenerUsuariosDisponibles()
    {
        var resultado = await _servicio.ObtenerUsuariosDisponiblesAsync();
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Vendedor no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearVendedorNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Vendedor creado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarVendedorNavDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Vendedor actualizado exitosamente.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Vendedor eliminado" });
    }
}
