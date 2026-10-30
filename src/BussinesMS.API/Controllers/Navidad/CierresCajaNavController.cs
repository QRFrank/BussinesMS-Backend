using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/CierresCaja")]
[Produces("application/json")]
[Menu(143, 151)]
public class CierresCajaNavController : BaseController
{
    private readonly ICierreCajaNavService _servicio;

    public CierresCajaNavController(ICierreCajaNavService servicio)
    {
        _servicio = servicio;
    }

    // Historial de cierres de caja registrados
    [HttpGet]
    public async Task<IActionResult> ObtenerHistorial([FromQuery] CierreCajaNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerHistorialAsync(query);
        return RespuestaOk(resultado);
    }

    // Resumen del turno cerrado sin entrega, o del turno actual (previsualización)
    [HttpGet("resumen")]
    public async Task<IActionResult> ObtenerResumen([FromQuery] int almacenId)
    {
        var resultado = await _servicio.ObtenerResumenAsync(almacenId);
        return RespuestaOk(resultado);
    }

    // Resumen (ventas y cobros) de un cierre ya registrado
    [HttpGet("{id:int}/resumen")]
    public async Task<IActionResult> ObtenerResumenCierre(int id)
    {
        var resultado = await _servicio.ObtenerResumenPorIdAsync(id);
        return RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarCierreCajaNavDto dto)
    {
        var resultado = await _servicio.RegistrarAsync(dto);
        return RespuestaCreado(resultado, "Cierre de caja registrado");
    }
}
