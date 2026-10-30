using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Ventas")]
[Produces("application/json")]
[Menu(151, 152, 154, 143)]
public class VentasNavController : BaseController
{
    private readonly IVentaNavService _servicio;
    private readonly IReservaCoberturaNavService _cobertura;

    public VentasNavController(IVentaNavService servicio, IReservaCoberturaNavService cobertura)
    {
        _servicio = servicio;
        _cobertura = cobertura;
    }

    [Menu(154, 172)]
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] VentaNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    // "Mis ventas" del vendedor de tienda: solo las suyas (el cierre de caja usa GET Ventas)
    [Menu(151)]
    [HttpGet("mias")]
    public async Task<IActionResult> ObtenerMias([FromQuery] VentaNavFiltroDto query)
    {
        var (resultado, tieneVendedor) = await _servicio.ObtenerMiasAsync(query);
        return tieneVendedor
            ? RespuestaOk(resultado)
            : RespuestaOk(resultado, "Tu usuario no tiene un vendedor vinculado en la temporada: no hay ventas propias para mostrar.");
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

    [Menu(151, 152, 154)]
    [HttpGet("productos")]
    public async Task<IActionResult> ObtenerProductos([FromQuery] int almacenId)
    {
        var resultado = await _servicio.ObtenerProductosAsync(almacenId);
        return RespuestaOk(resultado);
    }

    // Ruta antigua: apunta al mismo cálculo que GET api/Navidad/Reservas/cobertura
    [Menu(151, 152, 154)]
    [HttpGet("reservas/cobertura")]
    public async Task<IActionResult> ObtenerCobertura([FromQuery] ReservaCoberturaNavFiltroDto filtro)
    {
        var resultado = await _cobertura.ObtenerCoberturaAsync(filtro);
        return RespuestaOk(resultado);
    }

    [Menu(151, 154)]
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

    [Menu(151, 152, 154, 143, Flag = FlagMenu.Eliminar)]
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

    [Menu(151, 152, 154, 143, Flag = FlagMenu.Eliminar)]
    [HttpPost("cobros/{cobroId:int}/anular")]
    public async Task<IActionResult> AnularCobro(int cobroId)
    {
        var resultado = await _servicio.AnularCobroAsync(cobroId);
        return RespuestaOk(resultado, "Cobro anulado");
    }

    [Menu(151, 152, 154, 143, Flag = FlagMenu.Eliminar)]
    [HttpPost("{id:int}/anular")]
    public async Task<IActionResult> Anular(int id)
    {
        await _servicio.AnularAsync(id);
        return RespuestaOk(new { mensaje = "Venta anulada" }, "Venta anulada");
    }
}
