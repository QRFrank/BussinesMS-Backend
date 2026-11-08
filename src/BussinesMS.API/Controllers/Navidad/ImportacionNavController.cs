using BussinesMS.API.Autorizacion;
using BussinesMS.Aplicacion.Interfaces.Navidad;
using Microsoft.AspNetCore.Mvc;

namespace BussinesMS.API.Controllers.Navidad;

[ApiController]
[Route("api/Navidad/Importacion")]
[Produces("application/json")]
[SoloAdmin]
public class ImportacionNavController : BaseController
{
    private readonly IImportacionNavService _servicio;

    public ImportacionNavController(IImportacionNavService servicio)
    {
        _servicio = servicio;
    }

    /// <summary>Importa productos desde el Excel de carga (hoja "Productos"). Todo o nada.</summary>
    [HttpPost("productos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> ImportarProductos(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
            return RespuestaError("No se envió ningún archivo");
        if (!Path.GetExtension(archivo.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return RespuestaError("El archivo tiene que ser un Excel .xlsx");

        await using var flujo = archivo.OpenReadStream();
        var resultado = await _servicio.ImportarProductosAsync(flujo);

        if (resultado.Errores.Count > 0)
            return BadRequest(new
            {
                Success = false,
                Message = "El archivo tiene errores. No se guardó nada.",
                ErrorCode = "IMPORTACION_INVALIDA",
                Errores = resultado.Errores
            });

        return RespuestaOk(new
        {
            resultado.ProveedoresCreados,
            resultado.CategoriasCreadas,
            resultado.ProductosCreados,
            resultado.Omitidos
        }, "Importación terminada");
    }
}
