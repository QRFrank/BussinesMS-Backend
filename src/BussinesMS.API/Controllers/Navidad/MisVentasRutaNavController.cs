using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

// Registro personal y privado de ventas del vendedor de ruta (el admin recibe 403 en todo)
[ApiController]
[Route("api/Navidad/Ruta/MisVentas")]
[Produces("application/json")]
public class MisVentasRutaNavController : BaseController
{
    private readonly IVentaClienteRutaNavService _servicio;

    public MisVentasRutaNavController(IVentaClienteRutaNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet]
    public async Task<IActionResult> Obtener([FromQuery] MisVentasRutaFiltroDto query)
        => RespuestaOk(await _servicio.ObtenerAsync(query));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
        => RespuestaOk(await _servicio.ObtenerPorIdAsync(id));

    [HttpGet("me-deben")]
    public async Task<IActionResult> ObtenerMeDeben([FromQuery] int? temporadaId)
        => RespuestaOk(await _servicio.ObtenerMeDebenAsync(temporadaId));

    // Ayuda del cierre: total registrado de ventas a clientes de una salida propia
    [HttpGet("salidas/{salidaId:int}/total-registrado")]
    public async Task<IActionResult> ObtenerTotalRegistradoSalida(int salidaId)
        => RespuestaOk(await _servicio.ObtenerTotalRegistradoSalidaAsync(salidaId));

    [HttpPost]
    public async Task<IActionResult> Registrar([FromBody] RegistrarVentaClienteRutaDto dto)
        => RespuestaCreado(await _servicio.RegistrarAsync(dto), "Venta registrada");

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarVentaClienteRutaDto dto)
        => RespuestaOk(await _servicio.EditarAsync(id, dto), "Venta actualizada");

    [HttpPost("{id:int}/cobros")]
    public async Task<IActionResult> RegistrarCobro(int id, [FromBody] RegistrarCobroClienteRutaDto dto)
        => RespuestaOk(await _servicio.RegistrarCobroAsync(id, dto), "Cobro registrado");

    [HttpPost("{id:int}/cobros/{cobroId:int}/anular")]
    public async Task<IActionResult> AnularCobro(int id, int cobroId)
        => RespuestaOk(await _servicio.AnularCobroAsync(id, cobroId), "Cobro anulado");

    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
        => RespuestaOk(await _servicio.AnularAsync(id), "Venta anulada");
}
