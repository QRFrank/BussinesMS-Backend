using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Temporadas")]
[Produces("application/json")]
public class TemporadasController : BaseController
{
    private readonly ITemporadaService _servicio;
    private readonly ICatalogoNavService _catalogoServicio;

    public TemporadasController(ITemporadaService servicio, ICatalogoNavService catalogoServicio)
    {
        _servicio = servicio;
        _catalogoServicio = catalogoServicio;
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
            ? RespuestaError("Temporada no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpGet("actual")]
    public async Task<IActionResult> ObtenerActual()
    {
        var resultado = await _servicio.ObtenerActualAsync();
        return RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearTemporadaDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Temporada creada");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarTemporadaDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Temporada actualizada exitosamente.");
    }

    [HttpPost("{id:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int id)
    {
        var resultado = await _servicio.CerrarAsync(id);
        return RespuestaOk(resultado, "Temporada cerrada");
    }

    // Copia proveedores, códigos, productos, presentaciones y vendedores desde otra temporada
    [HttpPost("{destinoId:int}/copiar-catalogo")]
    public async Task<IActionResult> CopiarCatalogo(int destinoId, [FromBody] CopiarCatalogoDto dto)
    {
        var resultado = await _catalogoServicio.CopiarAsync(destinoId, dto.TemporadaOrigenId);
        return RespuestaOk(resultado, "Catálogo copiado");
    }
}
