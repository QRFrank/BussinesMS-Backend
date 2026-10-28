using BussinesMS.Aplicacion.DTOs.Navidad;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

// Reportes del sistema navideño (módulo 9). Solo Admin (403 SIN_PERMISO para el resto), solo lectura, temporada abierta
[ApiController]
[Route("api/Navidad/Reportes")]
[Produces("application/json")]
public class ReportesNavController : BaseController
{
    private readonly IReportesNavService _servicio;

    public ReportesNavController(IReportesNavService servicio)
    {
        _servicio = servicio;
    }

    // Ganancia de la temporada (ventas netas menos costos, pérdidas, gastos y pagos) y capital
    [HttpGet("resultado")]
    public async Task<IActionResult> Resultado([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta)
        => RespuestaOk(await _servicio.ObtenerResultadoAsync(desde, hasta));

    // Totales, por día, por vendedor, por almacén y por producto (canal: 1 Tienda, 2 Ruta; tipo: 1 Directa, 2 Reserva)
    [HttpGet("ventas")]
    public async Task<IActionResult> Ventas([FromQuery] ReporteVentasNavFiltrosDto filtros)
        => RespuestaOk(await _servicio.ObtenerVentasAsync(filtros));

    // Matriz producto x almacén en unidades, con el valorizado
    [HttpGet("stock")]
    public async Task<IActionResult> Stock([FromQuery] int? almacenId, [FromQuery] int? proveedorId, [FromQuery] bool incluirSinStock = false)
        => RespuestaOk(await _servicio.ObtenerStockAsync(almacenId, proveedorId, incluirSinStock));

    // Lo pedido contra lo recibido por código de cliente y producto
    [HttpGet("faltantes")]
    public async Task<IActionResult> Faltantes([FromQuery] int? proveedorId, [FromQuery] int? codigoClienteId)
        => RespuestaOk(await _servicio.ObtenerFaltantesAsync(proveedorId, codigoClienteId));

    // Con proveedores, de clientes, de vendedores de ruta, con inversores, sueldos y comisiones
    [HttpGet("deudas")]
    public async Task<IActionResult> Deudas()
        => RespuestaOk(await _servicio.ObtenerDeudasAsync());

    [HttpGet("gastos")]
    public async Task<IActionResult> Gastos([FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? categoriaId)
        => RespuestaOk(await _servicio.ObtenerGastosAsync(desde, hasta, categoriaId));

    [HttpGet("pagos-proveedores")]
    public async Task<IActionResult> PagosProveedores(
        [FromQuery] DateTime? desde, [FromQuery] DateTime? hasta, [FromQuery] int? proveedorId, [FromQuery] int? codigoClienteId)
        => RespuestaOk(await _servicio.ObtenerPagosProveedoresAsync(desde, hasta, proveedorId, codigoClienteId));
}
