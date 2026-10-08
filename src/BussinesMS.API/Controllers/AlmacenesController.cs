using Microsoft.AspNetCore.Mvc;
using BussinesMS.Aplicacion.Interfaces.Auth;
using BussinesMS.Aplicacion.DTOs.Auth;
using BussinesMS.Aplicacion.DTOs.Plantillas;
using BussinesMS.Aplicacion.Seguridad;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlmacenesController : BaseController
{
    private readonly IAlmacenService _servicio;
    private readonly JwtHelper _jwtHelper;

    public AlmacenesController(IAlmacenService servicio, JwtHelper jwtHelper)
    {
        _servicio = servicio;
        _jwtHelper = jwtHelper;
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodos([FromQuery] GenericPaginationQueryDto query, [FromQuery] int? sistemaId = null)
    {
        var resultado = await _servicio.ObtenerTodosAsync(query, ResolverSistemaId(sistemaId));
        return RespuestaOk(resultado);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObtenerPorId(int id)
    {
        var resultado = await _servicio.ObtenerPorIdAsync(id);
        return resultado == null ? RespuestaError("Almacén no encontrado", 404) : RespuestaOk(resultado);
    }

    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] CrearAlmacenDto almacen)
    {
        almacen.SistemaId = ResolverSistemaId(almacen.SistemaId);
        var resultado = await _servicio.CrearAsync(almacen);
        return RespuestaOk(resultado, "Almacén creado");
    }

    [HttpPut]
    public async Task<IActionResult> Actualizar([FromBody] ActualizarAlmacenDto dto)
    {
        var resultado = await _servicio.ActualizarAsync(dto);
        return RespuestaOk(resultado, "Almacén actualizado");
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(id);
        return RespuestaOk(new { mensaje = "Almacén eliminado" });
    }

    /// <summary>Sistema explícito → claim "sistemaId" del JWT → 1 (Regular).</summary>
    private int ResolverSistemaId(int? sistemaId) => sistemaId ?? _jwtHelper.GetSistemaId(User) ?? 1;
}
