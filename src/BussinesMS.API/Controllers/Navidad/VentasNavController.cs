using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Ventas")]
[Produces("application/json")]
public class VentasNavController : BaseController
{
    private readonly IVentaNavService _servicio;
    private readonly IReservaNavService _reservas;

    public VentasNavController(IVentaNavService servicio, IReservaNavService reservas)
    {
        _servicio = servicio;
        _reservas = reservas;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] VentaNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Venta no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpGet("contexto")]
    public async Task<IActionResult> ObtenerContexto()
    {
        var resultado = await _servicio.ObtenerContextoAsync();
        return RespuestaOk(resultado);
    }

    [HttpGet("productos")]
    public async Task<IActionResult> ObtenerProductos([FromQuery] int almacenId)
    {
        var resultado = await _servicio.ObtenerProductosAsync(almacenId);
        return RespuestaOk(resultado);
    }

    [HttpGet("reservas/cobertura")]
    public async Task<IActionResult> ObtenerCobertura([FromQuery] ReservaCoberturaNavFiltroDto filtro)
    {
        var resultado = await _reservas.ObtenerCoberturaAsync(filtro);
        return RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearVentaNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Venta registrada");
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarVentaNavDto dto)
    {
        var resultado = await _servicio.EditarAsync(id, dto);
        return RespuestaOk(resultado, "Venta actualizada");
    }

    [HttpPost("{id:int}/entregas")]
    public async Task<IActionResult> RegistrarEntrega(int id, [FromBody] RegistrarEntregaVentaDto dto)
    {
        var resultado = await _servicio.RegistrarEntregaAsync(id, dto);
        return RespuestaCreado(resultado, "Entrega registrada");
    }

    [HttpPost("entregas/{entregaId:int}/anular")]
    public async Task<IActionResult> AnularEntrega(int entregaId)
    {
        var resultado = await _servicio.AnularEntregaAsync(entregaId);
        return RespuestaOk(resultado, "Entrega anulada");
    }

    [HttpPost("{id:int}/cobros")]
    public async Task<IActionResult> RegistrarCobro(int id, [FromBody] RegistrarCobroVentaDto dto)
    {
        var resultado = await _servicio.RegistrarCobroAsync(id, dto);
        return RespuestaCreado(resultado, "Cobro registrado");
    }

    [HttpPost("cobros/{cobroId:int}/anular")]
    public async Task<IActionResult> AnularCobro(int cobroId)
    {
        var resultado = await _servicio.AnularCobroAsync(cobroId);
        return RespuestaOk(resultado, "Cobro anulado");
    }

    [HttpPut("{id:int}/bloqueo")]
    public async Task<IActionResult> CambiarBloqueo(int id, [FromBody] BloqueoVentaDto dto)
    {
        var resultado = await _servicio.CambiarBloqueoAsync(id, dto.Bloquear);
        return RespuestaOk(resultado, dto.Bloquear ? "Stock bloqueado" : "Stock desbloqueado");
    }

    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        await _servicio.AnularAsync(id);
        return RespuestaOk(new { mensaje = "Venta anulada" }, "Venta anulada");
    }
}
