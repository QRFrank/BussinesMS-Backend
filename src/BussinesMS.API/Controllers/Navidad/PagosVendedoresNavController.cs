using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/PagosVendedores")]
[Produces("application/json")]
public class PagosVendedoresNavController : BaseController
{
    private readonly IPagoVendedorNavService _servicio;

    public PagosVendedoresNavController(IPagoVendedorNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("Contexto")]
    public async Task<IActionResult> ObtenerContexto()
        => RespuestaOk(await _servicio.ObtenerContextoAsync());

    [HttpGet("Reporte")]
    public async Task<IActionResult> ObtenerReporte([FromQuery] ReporteVentasVendedorFiltroDto filtro)
        => RespuestaOk(await _servicio.ObtenerReporteAsync(filtro));

    [HttpGet("Sueldos/Resumen")]
    public async Task<IActionResult> ObtenerResumenSueldos([FromQuery] int? temporadaId, [FromQuery] int? vendedorId)
        => RespuestaOk(await _servicio.ObtenerResumenSueldosAsync(temporadaId, vendedorId));

    [HttpGet]
    public async Task<IActionResult> ObtenerPagos([FromQuery] PagoVendedorFiltroDto query)
        => RespuestaOk(await _servicio.ObtenerPagosAsync(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPago(int id)
    {
        var resultado = await _servicio.ObtenerPagoAsync(id);
        return resultado == null
            ? RespuestaError("Pago a vendedor no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarPagoVendedorDto dto)
        => RespuestaCreado(await _servicio.RegistrarAsync(dto), "Pago registrado");

    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
        => RespuestaOk(await _servicio.AnularAsync(id), "Pago anulado");
}
