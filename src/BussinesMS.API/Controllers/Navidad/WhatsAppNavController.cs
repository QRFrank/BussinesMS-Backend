using BussinesMS.API.Autorizacion;
using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/WhatsApp")]
[Produces("application/json")]
[SoloAdmin]
public class WhatsAppNavController : BaseController
{
    private readonly IWhatsAppAdminService _servicio;

    public WhatsAppNavController(IWhatsAppAdminService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("numero")]
    public async Task<IActionResult> ObtenerNumero()
        => RespuestaOk(await _servicio.ObtenerNumeroAsync());

    [HttpPost("numero/qr")]
    public async Task<IActionResult> ObtenerQr()
        => RespuestaOk(await _servicio.ObtenerQrAsync());

    [HttpPost("numero/desconectar")]
    public async Task<IActionResult> Desconectar()
        => RespuestaOk(await _servicio.DesconectarAsync());

    [HttpPost("numero/cambiar")]
    public async Task<IActionResult> CambiarNumero()
        => RespuestaOk(await _servicio.CambiarNumeroAsync());

    [HttpPost("prueba")]
    public async Task<IActionResult> EnviarPrueba([FromBody] EnviarPruebaWhatsAppDto dto)
        => RespuestaOk(await _servicio.EnviarPruebaAsync(dto));
}
