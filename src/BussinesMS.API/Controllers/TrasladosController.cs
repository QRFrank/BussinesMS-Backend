using BussinesMS.Aplicacion.DTOs.Sistema;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
public class TrasladosController : BaseController
{
    private readonly ITrasladoService _servicio;

    public TrasladosController(ITrasladoService servicio)
    {
        _servicio = servicio;
    }

    [HttpPost("por-lote")]
    public async Task<IActionResult> CrearPorLote([FromBody] CrearTrasladoPorLoteDto dto)
    {
        var resultado = await _servicio.CrearPorLoteAsync(dto);
        return RespuestaCreado(resultado, "Traslado por lote creado");
    }
}
