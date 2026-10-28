using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

// Dashboard navideño por rol (solo lectura, temporada abierta)
[ApiController]
[Route("api/Navidad/Dashboard")]
[Produces("application/json")]
public class DashboardNavController : BaseController
{
    private readonly IDashboardNavService _servicio;

    public DashboardNavController(IDashboardNavService servicio)
    {
        _servicio = servicio;
    }

    [HttpGet("perfil")]
    public async Task<IActionResult> ObtenerPerfil()
        => RespuestaOk(await _servicio.ObtenerPerfilAsync());

    // Solo Admin (403 para cualquier otro rol)
    [HttpGet("admin")]
    public async Task<IActionResult> ObtenerAdmin()
        => RespuestaOk(await _servicio.ObtenerAdminAsync());

    // Vendedor vinculado al usuario actual; almacenId = tienda de sesión (avisos de conteo y caja)
    [HttpGet("vendedor")]
    public async Task<IActionResult> ObtenerVendedor([FromQuery] int? almacenId)
        => RespuestaOk(await _servicio.ObtenerVendedorAsync(almacenId));

    // Solo rol EncargadoAlmacen: sus almacenes de la temporada abierta
    [HttpGet("encargado")]
    public async Task<IActionResult> ObtenerEncargado([FromQuery] int? umbralStockBajo)
        => RespuestaOk(await _servicio.ObtenerEncargadoAsync(umbralStockBajo));
}
