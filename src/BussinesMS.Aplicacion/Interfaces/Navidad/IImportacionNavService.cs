using BussinesMS.Aplicacion.DTOs.Navidad;

namespace BussinesMS.Aplicacion.Interfaces.Navidad;

public interface IImportacionNavService
{
    // Todo o nada: si el resultado trae Errores, no se guardó nada
    Task<ImportacionProductosResultadoNavDto> ImportarProductosAsync(Stream archivo);
}
