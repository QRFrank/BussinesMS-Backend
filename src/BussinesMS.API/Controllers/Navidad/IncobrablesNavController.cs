using BussinesMS.API.Autorizacion;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Incobrables")]
[Produces("application/json")]
[SoloAdmin]
public class IncobrablesNavController : BaseController
{
    private readonly IIncobrableNavService _servicio;

    public IncobrablesNavController(IIncobrableNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] int? temporadaId, [FromQuery] int? tipo, [FromQuery] bool incluirAnulados = false)
        => RespuestaOk(await _servicio.ObtenerAsync(temporadaId, tipo, incluirAnulados));

    [HttpPost("cliente")]
    public async Task<IActionResult> RegistrarCliente([FromBody] RegistrarIncobrableClienteNavDto dto)
        => RespuestaCreado(await _servicio.RegistrarClienteAsync(dto), "Incobrable registrado");

    [HttpPost("vendedor-ruta")]
    public async Task<IActionResult> RegistrarVendedorRuta([FromBody] RegistrarIncobrableVendedorRutaNavDto dto)
        => RespuestaCreado(await _servicio.RegistrarVendedorRutaAsync(dto), "Incobrable registrado");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Anular(int id, [FromQuery] string? motivo)
        => RespuestaOk(await _servicio.AnularAsync(id, motivo), "Incobrable anulado");
}
