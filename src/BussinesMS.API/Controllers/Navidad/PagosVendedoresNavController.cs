using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/PagosVendedores")]
[Produces("application/json")]
[Menu(161, 162)]
public class PagosVendedoresNavController : BaseController
{
    private readonly IPagoVendedorNavService _servicio;

    public PagosVendedoresNavController(IPagoVendedorNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("Contexto")]
    public async Task<IActionResult> ObtenerContexto()
    {
        var contexto = await _servicio.ObtenerContextoAsync();
        return !contexto.EsAdmin && contexto.VendedorId == null
            ? RespuestaOk(contexto, SinVendedorMensaje)
            : RespuestaOk(contexto);
    }

    // Pantalla "Ventas por vendedor" (161): no-admin solo ve lo suyo; sin vendedor vinculado, lista vacía con mensaje
    [HttpGet("Reporte")]
    public async Task<IActionResult> ObtenerReporte([FromQuery] ReporteVentasVendedorFiltroDto filtro)
    {
        var (items, tieneAcceso) = await _servicio.ObtenerReporteConEstadoAsync(filtro);
        return tieneAcceso ? RespuestaOk(items) : RespuestaOk(items, SinVendedorMensaje);
    }

    private const string SinVendedorMensaje = "Tu usuario no tiene un vendedor vinculado en la temporada: no hay ventas propias para mostrar.";

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

    [Menu(162)]
    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarPagoVendedorDto dto)
        => RespuestaCreado(await _servicio.RegistrarAsync(dto), "Pago registrado");

    [Menu(162, Flag = FlagMenu.Eliminar)]
    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
        => RespuestaOk(await _servicio.AnularAsync(id), "Pago anulado");
}
