using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Temporadas")]
[Produces("application/json")]
[Menu(102)]
public class TemporadasController : BaseController
{
    private readonly ITemporadaService _servicio;
    private readonly ICatalogoNavService _catalogoServicio;
    private readonly ICierreTemporadaNavService _cierreServicio;

    public TemporadasController(ITemporadaService servicio, ICatalogoNavService catalogoServicio, ICierreTemporadaNavService cierreServicio)
    {
        _servicio = servicio;
        _catalogoServicio = catalogoServicio;
        _cierreServicio = cierreServicio;
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

    [SoloSesion]
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

    // Cierre de temporada (2026-11-07): pendientes (bloquean o alertan) y resumen del drawer
    [SoloAdmin]
    [HttpGet("{id:int}/cierre/resumen")]
    public async Task<IActionResult> ResumenCierre(int id)
        => RespuestaOk(await _cierreServicio.ObtenerResumenAsync(id));

    // Body opcional { confirmacionNombre }. 409 TEMPORADA_CERRADA si ya está cerrada; 409 TEMPORADA_CON_PENDIENTES con la lista en data.
    // En una transacción: stock sobrante a pérdida (kardex), foto del resultado y paso a Cerrada
    [SoloAdmin]
    [HttpPost("{id:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CerrarTemporadaNavDto? dto)
    {
        try
        {
            var resultado = await _cierreServicio.CerrarAsync(id, dto);
            return RespuestaOk(resultado, "Temporada cerrada");
        }
        catch (TemporadaConPendientesException ex)
        {
            return StatusCode(409, new { Success = false, Message = ex.Message, ErrorCode = ex.CodigoError, Data = ex.Pendientes });
        }
    }

    // Foto del resultado guardada al cerrar. 404 CIERRE_NO_ENCONTRADO si no tiene
    [SoloAdmin]
    [HttpGet("{id:int}/cierre")]
    public async Task<IActionResult> Cierre(int id)
        => RespuestaOk(await _cierreServicio.ObtenerCierreAsync(id));

    // Copia proveedores, códigos, productos, presentaciones y vendedores desde otra temporada
    [SoloAdmin]
    [HttpPost("{destinoId:int}/copiar-catalogo")]
    public async Task<IActionResult> CopiarCatalogo(int destinoId, [FromBody] CopiarCatalogoDto dto)
    {
        var resultado = await _catalogoServicio.CopiarAsync(destinoId, dto.TemporadaOrigenId);
        return RespuestaOk(resultado, "Catálogo copiado");
    }
}
