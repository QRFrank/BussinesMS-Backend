using BussinesMS.Aplicacion.DTOs.Sistema.Migracion;
using BussinesMS.Aplicacion.Interfaces.Sistema;
using BussinesMS.Dominio.Entidades.Sistema;
using BussinesMS.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BussinesMS.API.Autorizacion;

namespace BussinesMS.API.Controllers;

[ApiController]
[Route("api/Sistema/[controller]")]
[Produces("application/json")]
[SoloAdmin]
public class MigracionesController : BaseController
{
    private readonly IMigracionService _servicio;
    private readonly SistemaDbContext _context;

    public MigracionesController(IMigracionService servicio, SistemaDbContext context)
    {
        _servicio = servicio;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> MigrarDatos(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return RespuestaError("No se ha proporcionado ningún archivo");

        var resultado = await _servicio.MigrarDatosDesdeCsvAsync(archivo);
        return RespuestaOk(resultado);
    }

    [HttpDelete("LimpiarDatos")]
    public async Task<IActionResult> LimpiarDatos()
    {
        try
        {
            _context.ProductoVariantes.RemoveRange(_context.ProductoVariantes);
            _context.Productos.RemoveRange(_context.Productos);
            await _context.SaveChangesAsync();
            return RespuestaOk(new { success = true, message = "Datos eliminados correctamente" });
        }
        catch (Exception ex)
        {
            return RespuestaError($"Error al limpiar datos: {ex.Message}");
        }
    }
}