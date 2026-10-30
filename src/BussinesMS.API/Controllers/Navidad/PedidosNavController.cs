using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Pedidos")]
[Produces("application/json")]
[SoloAdmin]
public class PedidosNavController : BaseController
{
    private readonly IPedidoNavService _servicio;

    public PedidosNavController(IPedidoNavService servicio)
    {
        _servicio = servicio;
    }

    // El form de recepción elige el pedido: admin o "Puede hacer recepciones"
    [RequierePuedeHacerRecepciones]
    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] PedidoNavFiltroDto query)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query);
        return RespuestaOk(resultado);
    }

    [RequierePuedeHacerRecepciones]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null
            ? RespuestaError("Pedido no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearPedidoNavDto dto)
    {
        var resultado = await _servicio.CrearAsync(dto);
        return RespuestaCreado(resultado, "Pedido registrado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarPedidoNavDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Pedido actualizado");
    }
}
