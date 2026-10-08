using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/AportesCapital")]
[Produces("application/json")]
public class AportesCapitalController : BaseController
{
    private readonly IAporteCapitalService _servicio;

    public AportesCapitalController(IAporteCapitalService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] AporteCapitalFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumen([FromQuery] int? temporadaId)
    {
        var resultado = await _servicio.ObtenerResumenAsync(temporadaId);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Aporte de capital no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearAporteCapitalDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Aporte de capital creado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarAporteCapitalDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Aporte de capital actualizado exitosamente.");
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Aporte de capital eliminado" });
    }
}
