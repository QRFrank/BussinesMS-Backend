using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

// Reservas del Sistema Navideño (replanteo 2026-10-15). Cobros y anular siguen en api/Navidad/Ventas.
[ApiController]
[Route("api/Navidad/Reservas")]
[Produces("application/json")]
public class ReservasNavController : BaseController
{
    private readonly IReservaNavService _servicio;
    private readonly IReservaCoberturaNavService _cobertura;

    public ReservasNavController(IReservaNavService servicio, IReservaCoberturaNavService cobertura)
    {
        _servicio = servicio;
        _cobertura = cobertura;
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
            ? RespuestaError("Reserva no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpGet("productos")]
    public async Task<IActionResult> ObtenerProductos([FromQuery] int? reservaId)
    {
        var resultado = await _servicio.ObtenerProductosAsync(reservaId);
        return RespuestaOk(resultado);
    }

    [HttpGet("cobertura")]
    public async Task<IActionResult> ObtenerCobertura([FromQuery] ReservaCoberturaNavFiltroDto filtro)
    {
        var resultado = await _cobertura.ObtenerCoberturaAsync(filtro);
        return RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearReservaNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Reserva registrada");
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Editar(int id, [FromBody] EditarReservaNavDto dto)
    {
        var resultado = await _servicio.EditarAsync(id, dto);
        return RespuestaOk(resultado, "Reserva actualizada");
    }

    [HttpPost("{id:int}/entregas")]
    public async Task<IActionResult> RegistrarEntrega(int id, [FromBody] RegistrarEntregaVentaDto dto)
    {
        var resultado = await _servicio.RegistrarEntregaAsync(id, dto);
        return RespuestaCreado(resultado, "Entrega registrada");
    }

    [HttpPost("{id:int}/bloqueos/liberar")]
    public async Task<IActionResult> LiberarBloqueo(int id, [FromBody] LiberarBloqueoReservaDto dto)
    {
        var resultado = await _servicio.LiberarBloqueoAsync(id, dto);
        return RespuestaOk(resultado, "Bloqueo liberado");
    }
}
