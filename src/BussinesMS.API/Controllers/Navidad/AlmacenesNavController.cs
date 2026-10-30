using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Almacenes")]
[Produces("application/json")]
[Menu(103)]
public class AlmacenesNavController : BaseController
{
    private readonly IAlmacenNavService _servicio;

    public AlmacenesNavController(IAlmacenNavService servicio)
    {
        _servicio = servicio;
    }

    // Almacenes navideños con encargados de la temporada y el indicador aCargo del usuario actual
    [SoloSesion]
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] AlmacenNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    // Ruta literal: no choca con {almacenId:int}
    [HttpGet("encargados-disponibles")]
    public async Task<IActionResult> ObtenerCandidatos()
    {
        var resultado = await _servicio.ObtenerCandidatosAsync();
        return RespuestaOk(resultado);
    }

    [HttpGet("{almacenId:int}/encargados")]
    public async Task<IActionResult> ObtenerEncargados(int almacenId)
    {
        var resultado = await _servicio.ObtenerEncargadosAsync(almacenId);
        return RespuestaOk(resultado);
    }

    [SoloAdmin]
    [HttpPut("{almacenId:int}/encargados")]
    public async Task<IActionResult> ReemplazarEncargados(int almacenId, [FromBody] AsignarEncargadosNavDto dto)
    {
        var resultado = await _servicio.ReemplazarEncargadosAsync(almacenId, dto);
        return RespuestaOk(resultado, "Encargados actualizados");
    }
}
