using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Ruta")]
[Produces("application/json")]
[Menu(155)]
public class RutaNavController : BaseController
{
    private readonly IRutaNavService _servicio;

    public RutaNavController(IRutaNavService servicio)
    {
        _servicio = servicio;
    }

    [SoloSesion]
    [HttpGet("Contexto")]
    public async Task<IActionResult> ObtenerContexto()
        => RespuestaOk(await _servicio.ObtenerContextoAsync());

    // ---------- Salidas ----------

    [HttpGet("Salidas")]
    public async Task<IActionResult> ObtenerSalidas([FromQuery] SalidaRutaFiltroDto query)
        => RespuestaOk(await _servicio.ObtenerSalidasAsync(query));

    [HttpGet("Salidas/{id:int}")]
    public async Task<IActionResult> ObtenerSalida(int id)
    {
        var resultado = await _servicio.ObtenerSalidaAsync(id);
        return resultado == null
            ? RespuestaError("Salida de ruta no encontrada", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost("Salidas/cargas")]
    public async Task<IActionResult> Cargar([FromBody] CargarSalidaRutaDto dto)
        => RespuestaCreado(await _servicio.CargarAsync(dto), "Carga registrada");

    [HttpPost("Salidas/{id:int}/devoluciones")]
    public async Task<IActionResult> Devolver(int id, [FromBody] DevolverSalidaRutaDto dto)
        => RespuestaCreado(await _servicio.DevolverAsync(id, dto), "Devolución registrada");

    [HttpPost("Salidas/{id:int}/cierre")]
    public async Task<IActionResult> Cerrar(int id, [FromBody] CerrarSalidaRutaDto dto)
        => RespuestaOk(await _servicio.CerrarAsync(id, dto), "Salida cerrada");

    [SoloAdmin]
    [HttpPost("Salidas/{id:int}/anular-cierre")]
    public async Task<IActionResult> AnularCierre(int id)
        => RespuestaOk(await _servicio.AnularCierreAsync(id), "Cierre anulado");

    [Menu(155, Flag = FlagMenu.Eliminar)]
    [HttpPost("Salidas/{id:int}/anular")]
    public async Task<IActionResult> AnularSalida(int id)
        => RespuestaOk(await _servicio.AnularSalidaAsync(id), "Salida anulada");

    [Menu(155, Flag = FlagMenu.Eliminar)]
    [HttpPost("Cargas/{cargaId:int}/anular")]
    public async Task<IActionResult> AnularCarga(int cargaId)
        => RespuestaOk(await _servicio.AnularCargaAsync(cargaId), "Carga anulada");

    [Menu(155, Flag = FlagMenu.Eliminar)]
    [HttpPost("Devoluciones/{devolucionId:int}/anular")]
    public async Task<IActionResult> AnularDevolucion(int devolucionId)
        => RespuestaOk(await _servicio.AnularDevolucionAsync(devolucionId), "Devolución anulada");

    // ---------- Deuda y abonos ----------

    [HttpGet("Deudas")]
    public async Task<IActionResult> ObtenerDeudas([FromQuery] int? vendedorId)
        => RespuestaOk(await _servicio.ObtenerDeudasAsync(vendedorId));

    [HttpGet("Abonos")]
    public async Task<IActionResult> ObtenerAbonos([FromQuery] AbonoVendedorFiltroDto query)
        => RespuestaOk(await _servicio.ObtenerAbonosAsync(query));

    [HttpGet("Abonos/{id:int}")]
    public async Task<IActionResult> ObtenerAbono(int id)
    {
        var resultado = await _servicio.ObtenerAbonoAsync(id);
        return resultado == null
            ? RespuestaError("Abono no encontrado", 404)
            : RespuestaOk(resultado);
    }

    [HttpPost("Abonos")]
    public async Task<IActionResult> RegistrarAbono([FromBody] CrearAbonoVendedorDto dto)
        => RespuestaCreado(await _servicio.RegistrarAbonoAsync(dto), "Abono registrado");

    [Menu(155, Flag = FlagMenu.Eliminar)]
    [HttpPost("Abonos/{id:int}/anular")]
    public async Task<IActionResult> AnularAbono(int id)
        => RespuestaOk(await _servicio.AnularAbonoAsync(id), "Abono anulado");

    // ---------- Editar venta de ruta (solo admin) ----------

    [HttpPut("Ventas/{ventaId:int}/precios")]
    public async Task<IActionResult> EditarPrecios(int ventaId, [FromBody] EditarPreciosVentaRutaDto dto)
        => RespuestaOk(await _servicio.EditarPreciosVentaAsync(ventaId, dto), "Precios actualizados");

    [SoloAdmin]
    [HttpPost("Ventas/{ventaId:int}/devolver")]
    public async Task<IActionResult> DevolverVenta(int ventaId, [FromBody] DevolverVentaRutaDto dto)
        => RespuestaOk(await _servicio.DevolverVentaAsync(ventaId, dto), "Devolución registrada");
}
