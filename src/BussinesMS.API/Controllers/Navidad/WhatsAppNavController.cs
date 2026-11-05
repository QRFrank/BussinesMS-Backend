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
    private readonly IWhatsAppBandejaAdminService _bandeja;

    public WhatsAppNavController(IWhatsAppAdminService servicio, IWhatsAppBandejaAdminService bandeja)
    {
        _servicio = servicio;
        _bandeja = bandeja;
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

    // ===== Fase 2: bandeja =====

    /// <summary>Estado liviano para el banner (lee lo que dejó el monitor; no consulta a Evolution).</summary>
    [HttpGet("estado")]
    public async Task<IActionResult> ObtenerEstado()
        => RespuestaOk(await _bandeja.ObtenerEstadoBannerAsync());

    [HttpGet("envios")]
    public async Task<IActionResult> ObtenerBitacora([FromQuery] WhatsAppBitacoraFiltroDto filtro)
        => RespuestaOk(await _bandeja.ObtenerBitacoraAsync(filtro));

    [HttpGet("envios/{id:int}")]
    public async Task<IActionResult> ObtenerEnvio(int id)
        => RespuestaOk(await _bandeja.ObtenerEnvioAsync(id));

    [HttpPost("envios/{id:int}/reintentar")]
    public async Task<IActionResult> Reintentar(int id)
        => RespuestaOk(await _bandeja.ReintentarAsync(id));

    [HttpPost("prueba-bandeja")]
    public async Task<IActionResult> EnviarPruebaPorBandeja([FromBody] EnviarPruebaBandejaDto? dto)
        => RespuestaOk(await _bandeja.EnviarPruebaPorBandejaAsync(dto ?? new EnviarPruebaBandejaDto()));
}
